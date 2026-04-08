using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IDocumentRepository _documentRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IPdfGenerator _pdfGenerator;
    private readonly ILogger<EmailService> _logger;
    private readonly string _environment;

    public EmailService(
        IDocumentRepository documentRepo, ITenantRepository tenantRepo,
        IPdfGenerator pdfGenerator, ILogger<EmailService> logger,
        IConfiguration configuration)
    {
        _documentRepo = documentRepo;
        _tenantRepo = tenantRepo;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
        _environment = configuration["Email:Provider"] ?? "log"; // "log", "smtp", "resend"
    }

    public async Task SendDocumentEmailAsync(Guid tenantId, Guid documentId, string recipientEmail, CancellationToken ct = default)
    {
        var document = await _documentRepo.GetByIdWithItemsAsync(documentId, ct);
        var tenant = await _tenantRepo.GetByIdAsync(tenantId, ct);
        if (document is null || tenant is null) return;

        var pdfBytes = _pdfGenerator.GenerateInvoicePdf(document, tenant);

        if (_environment == "log")
        {
            _logger.LogInformation(
                "EMAIL STUB: Would send {FullNumber} PDF ({Size} bytes) to {Email} from {Tenant}",
                document.FullNumber, pdfBytes.Length, recipientEmail, tenant.RazonSocial);
            return;
        }

        // Real email integration (Sprint 8: Resend/SES)
        _logger.LogWarning("Email provider '{Provider}' not implemented yet", _environment);
    }
}
