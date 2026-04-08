namespace TukiFact.Application.Interfaces;

public interface IEmailService
{
    Task SendDocumentEmailAsync(Guid tenantId, Guid documentId, string recipientEmail, CancellationToken ct = default);
}
