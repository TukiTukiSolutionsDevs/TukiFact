using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Domain.Interfaces;
using TukiFact.Infrastructure.Services;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/homologacion")]
[Authorize(Roles = "admin")]
public class HomologacionController : ControllerBase
{
    private readonly HomologacionService _homologacion;
    private readonly ITenantProvider _tenantProvider;

    public HomologacionController(HomologacionService homologacion, ITenantProvider tenantProvider)
    {
        _homologacion = homologacion;
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Get the full homologation test set with status.
    /// </summary>
    [HttpGet]
    public IActionResult GetTestSet()
    {
        var tests = _homologacion.GetTestSet();
        return Ok(new
        {
            total = tests.Count,
            tests = tests.Select(t => new
            {
                t.Id,
                t.DocumentType,
                t.Description,
                t.Variant,
                status = "pending" // TODO: track per-tenant test status in DB
            })
        });
    }

    /// <summary>
    /// Execute a single homologation test case.
    /// </summary>
    [HttpPost("{testId}")]
    public async Task<IActionResult> ExecuteTest(string testId, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var result = await _homologacion.ExecuteTestAsync(tenantId, testId, ct);

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    /// <summary>
    /// Execute all homologation tests in sequence.
    /// </summary>
    [HttpPost("run-all")]
    public async Task<IActionResult> RunAll(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tests = _homologacion.GetTestSet();
        var results = new List<HomologacionResult>();

        foreach (var test in tests)
        {
            var result = await _homologacion.ExecuteTestAsync(tenantId, test.Id, ct);
            results.Add(result);
        }

        var passed = results.Count(r => r.Success);
        return Ok(new
        {
            total = results.Count,
            passed,
            failed = results.Count - passed,
            ready = passed == results.Count,
            results
        });
    }
}
