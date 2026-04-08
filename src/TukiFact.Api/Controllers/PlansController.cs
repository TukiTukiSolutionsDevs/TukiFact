using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/plans")]
public class PlansController : ControllerBase
{
    private readonly IPlanRepository _planRepo;

    public PlansController(IPlanRepository planRepo) => _planRepo = planRepo;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var plans = await _planRepo.GetAllActiveAsync(ct);
        var response = plans.Select(p => new
        {
            p.Id,
            p.Name,
            p.PriceMonthly,
            p.MaxDocumentsPerMonth,
            Features = System.Text.Json.JsonSerializer.Deserialize<object>(p.Features),
            p.IsActive
        });
        return Ok(response);
    }
}
