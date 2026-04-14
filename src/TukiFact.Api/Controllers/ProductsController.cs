using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Interfaces;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public ProductsController(AppDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public record CreateProductRequest(
        string Code,
        string Description,
        decimal UnitPrice,
        decimal UnitPriceWithIgv,
        string? SunatCode,
        string Currency = "PEN",
        string IgvType = "10",
        string UnitMeasure = "NIU",
        string? Category = null,
        string? Brand = null
    );

    public record UpdateProductRequest(
        string? Description,
        decimal? UnitPrice,
        decimal? UnitPriceWithIgv,
        string? SunatCode,
        string? Currency,
        string? IgvType,
        string? UnitMeasure,
        string? Category,
        string? Brand,
        bool? IsActive
    );

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var query = _db.Products.Where(p => p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Code.Contains(search) || p.Description.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(ct);

        var products = await query
            .OrderBy(p => p.Description)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id, p.Code, p.SunatCode, p.Description,
                p.UnitPrice, p.UnitPriceWithIgv, p.Currency,
                p.IgvType, p.UnitMeasure, p.Category, p.Brand,
                p.IsActive, p.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            data = products,
            pagination = new { page, pageSize, totalCount, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (product is null) return NotFound(new { error = "Producto no encontrado" });

        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var exists = await _db.Products
            .AnyAsync(p => p.TenantId == tenantId && p.Code == request.Code, ct);
        if (exists) return Conflict(new { error = $"Ya existe un producto con código '{request.Code}'" });

        var product = new Product
        {
            TenantId = tenantId,
            Code = request.Code,
            Description = request.Description,
            UnitPrice = request.UnitPrice,
            UnitPriceWithIgv = request.UnitPriceWithIgv,
            SunatCode = request.SunatCode,
            Currency = request.Currency,
            IgvType = request.IgvType,
            UnitMeasure = request.UnitMeasure,
            Category = request.Category,
            Brand = request.Brand,
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        return Created($"/v1/products/{product.Id}", new { product.Id, product.Code, product.Description });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (product is null) return NotFound(new { error = "Producto no encontrado" });

        if (request.Description is not null) product.Description = request.Description;
        if (request.UnitPrice.HasValue) product.UnitPrice = request.UnitPrice.Value;
        if (request.UnitPriceWithIgv.HasValue) product.UnitPriceWithIgv = request.UnitPriceWithIgv.Value;
        if (request.SunatCode is not null) product.SunatCode = request.SunatCode;
        if (request.Currency is not null) product.Currency = request.Currency;
        if (request.IgvType is not null) product.IgvType = request.IgvType;
        if (request.UnitMeasure is not null) product.UnitMeasure = request.UnitMeasure;
        if (request.Category is not null) product.Category = request.Category;
        if (request.Brand is not null) product.Brand = request.Brand;
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Producto actualizado" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);

        if (product is null) return NotFound(new { error = "Producto no encontrado" });

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Producto eliminado" });
    }
}
