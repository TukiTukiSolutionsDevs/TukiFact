using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TukiFact.Api.Hosting;
using TukiFact.Api.OpenApi;
using TukiFact.Infrastructure;
using TukiFact.Api.Middleware;
using TukiFact.Common.Application.DependencyInjection;
using TukiFact.Common.Infrastructure.Authentication;
using TukiFact.Common.Infrastructure.Persistence;
using TukiFact.Common.Presentation.DependencyInjection;
using TukiFact.Common.Presentation.Endpoints;
using TukiFact.Common.Presentation.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Sentry — no-op when Dsn is empty; honors Sentry:* config in appsettings + env (Sentry__Dsn).
builder.WebHost.UseSentry();

// Explicit Http1/Http2(h2c) ports only when Grpc:Enabled=true (default false); otherwise Kestrel
// keeps its normal ASPNETCORE_URLS-driven binding.
builder.ConfigureDualProtocol();

// === Services ===
// Under build-time OpenAPI generation (dotnet build -> openapi/tukifact-api-v1.json) the same
// registrations run with placeholder settings and no hosted services.
if (BuildTimeDocumentGeneration.IsActive)
{
    BuildTimeDocumentGeneration.Register(builder, RegisterServices);
}
else
{
    RegisterServices(builder);
}

var app = builder.Build();

// Composition root for service registration, extracted to a local
// function so BuildTimeDocumentGeneration can run it with placeholder settings and no hosted
// services when generating openapi/tukifact-api-v1.json at build time.
static void RegisterServices(WebApplicationBuilder builder)
{
    // Infrastructure (EF Core + PostgreSQL + Tenant Provider) — legacy, unchanged, still first.
    builder.Services.AddInfrastructure(builder.Configuration);

    // Kernel: every TryAdd* here is subordinate to the legacy
    // registrations above. AddCommonApplication/AddEndpoints start with an empty list — no
    // handlers, no module — until the first module ships.
    builder.Services.AddCurrentUser();
    builder.Services.AddCommonApplication();
    builder.Services.AddCommonPersistence(builder.Configuration);
    builder.Services.AddPersistenceExceptionHandling();
    builder.Services.AddCommonPresentation();
    builder.Services.AddEndpointVersioning();
    builder.Services.AddEndpoints([.. ModuleCatalog.EndpointAssemblies]);
    builder.Services.AddCommonGrpc();

    // JWT Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtSecret = builder.Configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret not configured");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TukiFact",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TukiFact",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

    builder.Services.AddAuthorization();

    // Controllers
    builder.Services.AddControllers();
    builder.Services.AddHttpClient(); // For external service proxy (lookup, AI)

    // OpenAPI: single "v1" document (openapi/tukifact-api-v1.json) covering the legacy
    // controllers and any future kernel endpoint, shaped by the ported transformers.
    builder.Services.AddApiDocumentation();

    // CORS — Cors:FrontendUrl accepts a comma-separated list to allow multiple front-end hosts
    // (e.g. marketing host + portal host calling the same API).
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            var raw = builder.Configuration.GetValue<string>("Cors:FrontendUrl") ?? "http://localhost:3000";
            var origins = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // DataProtection — persist keys across container restarts
    var keyDir = Environment.GetEnvironmentVariable("ASPNETCORE_DataProtection__KeyDirectory");
    if (!string.IsNullOrEmpty(keyDir))
    {
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keyDir));
    }

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "postgresql",
            tags: ["db", "ready"])
        .AddCheck<NatsHealthCheck>("nats", tags: ["messaging", "ready"])
        .AddCheck<MinioHealthCheck>("minio", tags: ["storage", "ready"]);
}

// Database bootstrap: migrations -> row level security -> seed data.
// Any failure is fatal on purpose: RLS is a security control, so the API must not
// start serving requests if the policies could not be applied.
// Skipped entirely under build-time OpenAPI generation: there is no real database to reach.
if (!BuildTimeDocumentGeneration.IsActive)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<TukiFact.Infrastructure.Persistence.AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<TukiFact.Application.Interfaces.IPasswordHasher>();

    var step = "Applying database migrations";
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        logger.LogInformation("{Step}", step);
        await dbContext.Database.MigrateAsync();

        step = "Applying row level security policies";
        logger.LogInformation("{Step}", step);
        await dbContext.Database.ExecuteSqlRawAsync("SELECT apply_rls_to_tenant_tables();");

        step = "Seeding data";
        logger.LogInformation("{Step}", step);
        await TukiFact.Api.Data.DataSeeder.SeedAsync(dbContext, passwordHasher);

        logger.LogInformation("Database bootstrap completed in {ElapsedMs} ms", stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database bootstrap failed at step '{Step}' after {ElapsedMs} ms", step, stopwatch.ElapsedMilliseconds);
        throw;
    }
}

// === Middleware Pipeline ===

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ResponseCacheMiddleware>();

// RFC 9457 ProblemDetails for every error (AddCommonPresentation) + correlation id propagation.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCorrelationId();

if (app.Environment.IsDevelopment())
{
    app.MapApiDocumentation();
}

// HTTPS redirect handled by nginx in prod; dev uses http directly
app.UseCors("AllowFrontend");

app.UseSentryTracing();

app.UseAuthentication();

// Tenant resolution (after auth so JWT claims are available)
app.UseMiddleware<TenantResolverMiddleware>();

// Rate limiting (after tenant resolution so we can identify the tenant)
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthorization();

// Audit logging (after authorization so we have full user context)
app.UseMiddleware<AuditMiddleware>();

// Idempotency replay (after auth/tenant so the tenant claim is set; before the controllers)
app.UseMiddleware<TukiFact.Infrastructure.Middleware.IdempotencyMiddleware>();

app.MapControllers();
app.MapEndpoints();

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Liveness = just "is the process running?"
});

// Metrics endpoint (for Prometheus scraping)
app.MapGet("/metrics", async (TukiFact.Infrastructure.Persistence.AppDbContext db) =>
{
    var tenantCount = await db.Tenants.CountAsync();
    var docCount = await db.Documents.CountAsync();
    var userCount = await db.Users.CountAsync();

    return Results.Text($"""
        # HELP tukifact_tenants_total Total number of tenants
        # TYPE tukifact_tenants_total gauge
        tukifact_tenants_total {tenantCount}

        # HELP tukifact_documents_total Total number of documents
        # TYPE tukifact_documents_total gauge
        tukifact_documents_total {docCount}

        # HELP tukifact_users_total Total number of users
        # TYPE tukifact_users_total gauge
        tukifact_users_total {userCount}
        """, "text/plain");
});

app.Run();

// Exposed for WebApplicationFactory<Program> in tests.
public partial class Program;
