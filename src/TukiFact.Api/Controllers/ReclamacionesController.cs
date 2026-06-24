using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TukiFact.Application.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/public/reclamaciones")]
[AllowAnonymous]
public class ReclamacionesController : ControllerBase
{
    private readonly IEmailService _email;
    private readonly string _soporteEmail;
    private readonly ILogger<ReclamacionesController> _logger;

    public ReclamacionesController(
        IEmailService email,
        IConfiguration configuration,
        ILogger<ReclamacionesController> logger)
    {
        _email = email;
        // INDECOPI requires the merchant to actually receive the reclamo — not just log it.
        // Configurable so we can route to a ticketing system later (e.g. legales@).
        _soporteEmail = configuration["Reclamaciones:NotifyEmail"] ?? "soporte@tukifact.com.pe";
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] ReclamacionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Descripcion) ||
            string.IsNullOrWhiteSpace(request.Tipo))
        {
            return BadRequest(new { error = "Faltan campos obligatorios: tipo, nombre, email, descripcion." });
        }

        var tracking = $"TF-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        _logger.LogWarning(
            "LIBRO_RECLAMACIONES [{Tracking}] tipo={Tipo} nombre={Nombre} doc={Documento} " +
            "email={Email} tel={Telefono} bien={Bien}",
            tracking, request.Tipo, request.Nombre, request.Documento,
            request.Email, request.Telefono, request.Bien);

        // Notify soporte (INDECOPI compliance: the merchant must receive the complaint).
        // Notify the consumer with their tracking number (so they have proof of submission).
        // Both calls are best-effort: if email is down we still return success with the
        // tracking number — the reclamo is captured in logs and the WARN above is queryable.
        try
        {
            await _email.SendAsync(BuildSoporteEmail(tracking, request), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reclamacion notification email to {To} for tracking {Tracking}", _soporteEmail, tracking);
        }
        try
        {
            await _email.SendAsync(BuildConsumerEmail(tracking, request), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reclamacion confirmation email to consumer {Email} for tracking {Tracking}", request.Email, tracking);
        }

        return Ok(new
        {
            trackingNumber = tracking,
            message = "Tu reclamo ha sido registrado exitosamente. Recibirás una confirmación por correo y te responderemos dentro de 30 días hábiles."
        });
    }

    private EmailMessage BuildSoporteEmail(string tracking, ReclamacionRequest r) => new()
    {
        To = _soporteEmail,
        Subject = $"[Libro de Reclamaciones] {tracking} — {r.Tipo} de {WebUtility.HtmlEncode(r.Nombre)}",
        Template = "reclamacion_internal",
        HtmlBody = $"""
            <h2>Nuevo registro en el Libro de Reclamaciones</h2>
            <p><strong>Código de hoja virtual:</strong> {tracking}</p>
            <p><strong>Fecha:</strong> {DateTimeOffset.UtcNow.AddHours(-5):yyyy-MM-dd HH:mm} (hora Lima)</p>
            <hr/>
            <h3>Datos del consumidor</h3>
            <ul>
              <li><strong>Nombre:</strong> {WebUtility.HtmlEncode(r.Nombre)}</li>
              <li><strong>Documento:</strong> {WebUtility.HtmlEncode(r.Documento)}</li>
              <li><strong>Email:</strong> {WebUtility.HtmlEncode(r.Email)}</li>
              <li><strong>Teléfono:</strong> {WebUtility.HtmlEncode(r.Telefono ?? "—")}</li>
            </ul>
            <h3>Detalle del registro</h3>
            <ul>
              <li><strong>Tipo:</strong> {WebUtility.HtmlEncode(r.Tipo)}</li>
              <li><strong>Bien contratado:</strong> {WebUtility.HtmlEncode(r.Bien)}</li>
            </ul>
            <h3>Descripción del hecho</h3>
            <p style="white-space:pre-wrap">{WebUtility.HtmlEncode(r.Descripcion)}</p>
            {(string.IsNullOrWhiteSpace(r.Pedido) ? "" : $"<h3>Pedido del consumidor</h3><p style='white-space:pre-wrap'>{WebUtility.HtmlEncode(r.Pedido)}</p>")}
            <hr/>
            <p>Plazo máximo de respuesta: <strong>30 días hábiles</strong> (DS 011-2011-PCM).</p>
            """,
    };

    private EmailMessage BuildConsumerEmail(string tracking, ReclamacionRequest r) => new()
    {
        To = r.Email,
        Subject = $"Hemos recibido tu {r.Tipo.ToLowerInvariant()} — {tracking}",
        Template = "reclamacion_confirmation",
        HtmlBody = $"""
            <p>Hola {WebUtility.HtmlEncode(r.Nombre)},</p>
            <p>Hemos registrado tu {WebUtility.HtmlEncode(r.Tipo.ToLowerInvariant())} en nuestro Libro de Reclamaciones.</p>
            <p>Tu código de seguimiento es:</p>
            <p style="font-family:monospace;font-size:18px;background:#f4f4f4;padding:12px;border-radius:8px;text-align:center"><strong>{tracking}</strong></p>
            <p>Nos comunicaremos contigo a este correo dentro de los próximos <strong>30 días hábiles</strong> con la respuesta a tu reclamo.</p>
            <p>Si necesitas escalar el caso, puedes contactar también a INDECOPI al <strong>0800-4-4040</strong> (llamada gratuita) o en <a href="https://www.indecopi.gob.pe">www.indecopi.gob.pe</a>.</p>
            <hr/>
            <p style="color:#666;font-size:13px">Tukituki Solution S.A.C. — RUC 20613614509<br/>Plataforma SaaS de facturación electrónica TukiFact</p>
            """,
    };
}

public record ReclamacionRequest(
    string Tipo,
    string Nombre,
    string Documento,
    string Email,
    string Telefono,
    string Bien,
    string Descripcion,
    string? Pedido
);
