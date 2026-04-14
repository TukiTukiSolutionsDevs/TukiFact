# 08 — Email Transaccional + Notificaciones

> **Prioridad**: 🔴 ALTA — El cliente DEBE recibir su factura por email.
> **Recomendación**: Resend (simple, barato, buen SDK .NET)

---

## Proveedores Evaluados

| Proveedor | Free tier | Precio | SDK .NET | Ventaja |
|-----------|-----------|--------|----------|---------|
| **Resend** | 100 emails/día | $20/mes = 50K | ✅ Oficial | Más simple, API moderna |
| SendGrid | 100/día | $19.95/mes = 50K | ✅ | Más maduro, más features |
| AWS SES | 62K gratis (EC2) | $0.10/1000 | ✅ | Más barato a escala |
| SMTP propio | — | — | ✅ Built-in | Control total, más trabajo |

**Recomendación**: **Resend** para MVP (simplicidad), migrar a SES si escala.

## Flujo de Envío

```
1. Emisión de documento (factura/boleta/NC/ND)
2. DocumentService publica evento en NATS: "document.emitted"
3. EmailConsumer escucha el evento
4. Si tenant tiene "auto_send_email: true":
   a. Genera PDF (si no existe)
   b. Construye email HTML con template
   c. Adjunta PDF
   d. Envía via IEmailService
   e. Guarda log en email_logs
5. Si falla, reintenta 3 veces con backoff
```

## Implementación

### Interface

```csharp
public interface IEmailService
{
    Task<EmailResult> SendAsync(EmailMessage message);
}

public class EmailMessage
{
    public string To { get; set; }
    public string? Cc { get; set; }
    public string Subject { get; set; }
    public string HtmlBody { get; set; }
    public List<EmailAttachment> Attachments { get; set; } = new();
    public string? ReplyTo { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; }
    public byte[] Content { get; set; }
    public string ContentType { get; set; } = "application/pdf";
}
```

### Templates de Email

1. **Comprobante emitido** — "Se ha emitido la Factura F001-00000001" + PDF adjunto
2. **Reset password** — "Restablecer tu contraseña de TukiFact"
3. **Factura recurrente** — "Tu factura mensual ha sido generada"
4. **Bienvenida** — "Bienvenido a TukiFact"
5. **Alerta de uso** — "Has alcanzado el 80% de tu plan"

### Entity EmailLog

```csharp
public class EmailLog
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string To { get; set; }
    public string Subject { get; set; }
    public string Template { get; set; } // "document_emitted"
    public string Status { get; set; } // "sent", "failed", "bounced"
    public string? ErrorMessage { get; set; }
    public string? ExternalId { get; set; } // ID del proveedor
    public Guid? DocumentId { get; set; } // documento relacionado
    public DateTime SentAt { get; set; }
    public int RetryCount { get; set; }
}
```

### Configuración por Tenant

```csharp
// En TenantServiceConfig
public bool AutoSendEmail { get; set; } = false;
public string? EmailProvider { get; set; } // "resend", "smtp", "ses"
public string? SmtpHost { get; set; }
public int? SmtpPort { get; set; }
public string? SmtpUser { get; set; }
public string? SmtpPassword { get; set; } // encriptado
public string? EmailFromName { get; set; } // "Mi Empresa SAC"
public string? EmailFromAddress { get; set; } // "facturacion@miempresa.com"
```

### NATS Consumer

```csharp
// Escucha eventos de documentos emitidos
[NatsConsumer("document.emitted")]
public class EmailDocumentConsumer
{
    public async Task Handle(DocumentEmittedEvent evt)
    {
        var tenant = await GetTenantConfig(evt.TenantId);
        if (!tenant.AutoSendEmail) return;
        
        var pdf = await GenerateOrGetPdf(evt.DocumentId);
        var email = BuildDocumentEmail(evt, pdf);
        await _emailService.SendAsync(email);
        await LogEmail(evt, "sent");
    }
}
```

### Archivos a crear

| Archivo | Descripción |
|---------|-------------|
| `Domain/Entities/EmailLog.cs` | Entity |
| `Infrastructure/Services/EmailService.cs` | Implementación Resend |
| `Infrastructure/Services/SmtpEmailService.cs` | Fallback SMTP |
| `Infrastructure/Consumers/EmailDocumentConsumer.cs` | NATS consumer |
| `Infrastructure/Templates/` | Templates HTML emails |
| `API/Controllers/EmailController.cs` | Reenviar email, ver logs |
| Frontend settings | Toggle auto-envío + config SMTP |

### NuGet packages

```xml
<PackageReference Include="Resend" Version="*" />
<!-- o para SMTP nativo: MailKit ya viene con .NET -->
```
