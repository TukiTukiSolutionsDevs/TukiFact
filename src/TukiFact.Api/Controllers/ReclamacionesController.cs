using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/public/reclamaciones")]
[AllowAnonymous]
public class ReclamacionesController : ControllerBase
{
    private readonly ILogger<ReclamacionesController> _logger;

    public ReclamacionesController(ILogger<ReclamacionesController> logger) => _logger = logger;

    [HttpPost]
    public IActionResult Submit([FromBody] ReclamacionRequest request)
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
            "email={Email} tel={Telefono} bien={Bien} descripcion={Descripcion} pedido={Pedido}",
            tracking, request.Tipo, request.Nombre, request.Documento,
            request.Email, request.Telefono, request.Bien,
            request.Descripcion[..Math.Min(200, request.Descripcion.Length)],
            request.Pedido?[..Math.Min(200, request.Pedido?.Length ?? 0)]);

        return Ok(new
        {
            trackingNumber = tracking,
            message = "Tu reclamo ha sido registrado exitosamente. Te responderemos dentro de 30 días hábiles al correo proporcionado."
        });
    }
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
