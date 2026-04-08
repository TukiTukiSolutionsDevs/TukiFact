namespace TukiFact.Domain.Entities;

public class DocumentXmlLog
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid TenantId { get; set; }
    public string Action { get; set; } = string.Empty; // xml_generated, signed, sent_to_sunat, cdr_received, error
    public string? XmlSnippet { get; set; }      // Relevant XML portion or error details
    public string? SunatResponse { get; set; }   // CDR content or error
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Document Document { get; set; } = null!;
}
