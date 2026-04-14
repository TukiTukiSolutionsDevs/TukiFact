using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/catalogs")]
[Authorize]
public class CatalogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CatalogsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// List all SUNAT catalogs.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListCatalogs(CancellationToken ct)
    {
        var catalogs = await _db.SunatCatalogs
            .Where(c => c.IsActive)
            .OrderBy(c => c.CatalogNumber)
            .Select(c => new
            {
                c.CatalogNumber,
                c.Name,
                c.Description,
                CodesCount = c.Codes.Count(cc => cc.IsActive)
            })
            .ToListAsync(ct);

        return Ok(catalogs);
    }

    /// <summary>
    /// Get all codes for a specific SUNAT catalog.
    /// </summary>
    [HttpGet("{catalogNumber}")]
    public async Task<IActionResult> GetCatalogCodes(string catalogNumber, CancellationToken ct)
    {
        var catalog = await _db.SunatCatalogs
            .Include(c => c.Codes.Where(cc => cc.IsActive).OrderBy(cc => cc.SortOrder))
            .FirstOrDefaultAsync(c => c.CatalogNumber == catalogNumber, ct);

        if (catalog is null)
            return NotFound(new { error = $"Catálogo {catalogNumber} no encontrado" });

        return Ok(new
        {
            catalog.CatalogNumber,
            catalog.Name,
            catalog.Description,
            Codes = catalog.Codes.Select(c => new
            {
                c.Code,
                c.Description
            })
        });
    }

    /// <summary>
    /// List all detraction codes (Catálogo 54).
    /// </summary>
    [HttpGet("detractions")]
    public async Task<IActionResult> ListDetractionCodes(CancellationToken ct)
    {
        var codes = await _db.DetractionCodes
            .Where(d => d.IsActive)
            .OrderBy(d => d.Annex)
            .ThenBy(d => d.Code)
            .Select(d => new
            {
                d.Code,
                d.Description,
                d.Percentage,
                d.Annex
            })
            .ToListAsync(ct);

        return Ok(codes);
    }
}
