using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using TukiFact.Infrastructure;
using TukiFact.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// === Services ===

// Infrastructure (EF Core + PostgreSQL + Tenant Provider)
builder.Services.AddInfrastructure(builder.Configuration);

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

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetValue<string>("Cors:FrontendUrl") ?? "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: ["db", "ready"])
    .AddCheck<NatsHealthCheck>("nats", tags: ["messaging", "ready"])
    .AddCheck<MinioHealthCheck>("minio", tags: ["storage", "ready"]);

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TukiFact.Infrastructure.Persistence.AppDbContext>();
    await TukiFact.Api.Data.DataSeeder.SeedAsync(dbContext);
}

// === Middleware Pipeline ===

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ResponseCacheMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "TukiFact API";
        options.Theme = ScalarTheme.BluePlanet;
        options.DefaultHttpClient = new(ScalarTarget.Shell, ScalarClient.Curl);
        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecurityScheme = "Bearer"
        };
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();

// Tenant resolution (after auth so JWT claims are available)
app.UseMiddleware<TenantResolverMiddleware>();

// Rate limiting (after tenant resolution so we can identify the tenant)
app.UseMiddleware<RateLimitingMiddleware>();

app.UseAuthorization();

// Audit logging (after authorization so we have full user context)
app.UseMiddleware<AuditMiddleware>();

app.MapControllers();

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
