using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
            options
                .UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure(3);
                    })
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

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

        // GRE (Guía de Remisión Electrónica) services
        services.AddScoped<IDespatchAdviceRepository, DespatchAdviceRepository>();
        services.AddScoped<IDespatchAdviceService, DespatchAdviceService>();
        services.AddScoped<IGreXmlBuilder, GreXmlBuilder>();
        services.AddScoped<IGreSunatClient, GreSunatClient>();

        // RUC/DNI validation
        services.AddScoped<IRucValidationService, RucValidationService>();

        // Batch B services
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        services.AddScoped<ICpeValidationService, CpeValidationService>();

        // Batch C services — Retentions & Perceptions
        services.AddScoped<IRetentionRepository, RetentionRepository>();
        services.AddScoped<IPerceptionRepository, PerceptionRepository>();
        services.AddScoped<IRetentionXmlBuilder, RetentionXmlBuilder>();
        services.AddScoped<IPerceptionXmlBuilder, PerceptionXmlBuilder>();

        // Batch C — SIRE
        services.AddScoped<ISireClient, SireClient>();

        // Batch C — Recurring Invoices & Quotations
        services.AddScoped<IRecurringInvoiceRepository, RecurringInvoiceRepository>();
        services.AddScoped<IQuotationRepository, QuotationRepository>();
        services.AddHostedService<RecurringInvoiceScheduler>();

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

        // HttpClient for GRE REST API (OAuth2 + send)
        services.AddHttpClient("SunatGre", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // HttpClient for Resend email API
        services.AddHttpClient("Resend", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // HttpClient for RUC/DNI validation (apis.net.pe)
        services.AddHttpClient("ApisNetPe", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // HttpClient for Webhook delivery
        services.AddHttpClient("Webhook", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // HttpClient for SIRE REST API (OAuth2 + RVIE management)
        services.AddHttpClient("SunatSire", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
