using Microsoft.AspNetCore.Mvc;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Simple ping endpoint to verify the API is running
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            service = "TukiFact API",
            version = "0.1.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
