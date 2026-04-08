using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Interfaces;
using TukiFact.Infrastructure.Persistence;
using TukiFact.Infrastructure.Persistence.Repositories;
using TukiFact.Infrastructure.Services;

namespace TukiFact.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL + EF Core
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(3);
                }));

        // Tenant Provider (scoped - one per request)
        services.AddScoped<ITenantProvider, TenantProvider>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<ISeriesRepository, SeriesRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        // Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IUblBuilder, UblBuilder>();
        services.AddScoped<IXmlSigningService, XmlSigningService>();
        services.AddScoped<ISunatClient, SunatClient>();
        services.AddScoped<IStorageService, MinioStorageService>();

        // Sprint 4 services
        services.AddScoped<IVoidedDocumentRepository, VoidedDocumentRepository>();
        services.AddScoped<IPdfGenerator, PdfGenerator>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Sprint 7 services
        services.AddScoped<IWebhookRepository, WebhookRepository>();
        services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IEventPublisher, NatsEventPublisher>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IRateLimiter, InMemoryRateLimiter>();
        services.AddScoped<WebhookDeliveryService>();

        // HttpClient for SUNAT SOAP calls
        services.AddHttpClient("Sunat", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "text/xml");
        });

        // HttpClient for Webhook delivery
        services.AddHttpClient("Webhook", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}
