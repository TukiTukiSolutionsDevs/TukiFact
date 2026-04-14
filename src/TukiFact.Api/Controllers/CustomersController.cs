using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Interfaces;
using TukiFact.Infrastructure.Persistence;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantProvider _tenantProvider;

    public CustomersController(AppDbContext db, ITenantProvider tenantProvider)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    public record CreateCustomerRequest(
        string DocType,
        string DocNumber,
        string Name,
        string? Email = null,
        string? Phone = null,
        string? Address = null,
        string? Ubigeo = null,
        string? Departamento = null,
        string? Provincia = null,
        string? Distrito = null,
        string? Category = null,
        string? Notes = null
    );

    public record UpdateCustomerRequest(
        string? Name,
        string? Email,
        string? Phone,
        string? Address,
        string? Ubigeo,
        string? Departamento,
        string? Provincia,
        string? Distrito,
        string? Category,
        string? Notes,
        bool? IsActive
    );

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? docType,
        [FromQuery] string? category,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var query = _db.Customers.Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.DocNumber.Contains(search) || c.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(docType))
            query = query.Where(c => c.DocType == docType);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(c => c.Category == category);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(ct);

        var customers = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id, c.DocType, c.DocNumber, c.Name,
                c.Email, c.Phone, c.Address,
                c.Category, c.IsActive, c.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            data = customers,
            pagination = new { page, pageSize, totalCount, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);

        if (customer is null) return NotFound(new { error = "Cliente no encontrado" });

        return Ok(new
        {
            customer.Id, customer.DocType, customer.DocNumber, customer.Name,
            customer.Email, customer.Phone, customer.Address,
            customer.Ubigeo, customer.Departamento, customer.Provincia, customer.Distrito,
            customer.Category, customer.Notes, customer.IsActive,
            customer.CreatedAt, customer.UpdatedAt
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchByDoc([FromQuery] string docNumber, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.DocNumber == docNumber, ct);

        if (customer is null) return NotFound(new { error = "Cliente no encontrado" });

        return Ok(new
        {
            customer.Id, customer.DocType, customer.DocNumber, customer.Name,
            customer.Email, customer.Phone, customer.Address
        });
    }

    [HttpPost]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var exists = await _db.Customers
            .AnyAsync(c => c.TenantId == tenantId && c.DocNumber == request.DocNumber, ct);
        if (exists) return Conflict(new { error = $"Ya existe un cliente con documento '{request.DocNumber}'" });

        var customer = new Customer
        {
            TenantId = tenantId,
            DocType = request.DocType,
            DocNumber = request.DocNumber,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            Ubigeo = request.Ubigeo,
            Departamento = request.Departamento,
            Provincia = request.Provincia,
            Distrito = request.Distrito,
            Category = request.Category,
            Notes = request.Notes,
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        return Created($"/v1/customers/{customer.Id}", new { customer.Id, customer.DocNumber, customer.Name });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin,emisor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);

        if (customer is null) return NotFound(new { error = "Cliente no encontrado" });

        if (request.Name is not null) customer.Name = request.Name;
        if (request.Email is not null) customer.Email = request.Email;
        if (request.Phone is not null) customer.Phone = request.Phone;
        if (request.Address is not null) customer.Address = request.Address;
        if (request.Ubigeo is not null) customer.Ubigeo = request.Ubigeo;
        if (request.Departamento is not null) customer.Departamento = request.Departamento;
        if (request.Provincia is not null) customer.Provincia = request.Provincia;
        if (request.Distrito is not null) customer.Distrito = request.Distrito;
        if (request.Category is not null) customer.Category = request.Category;
        if (request.Notes is not null) customer.Notes = request.Notes;
        if (request.IsActive.HasValue) customer.IsActive = request.IsActive.Value;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Cliente actualizado" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);

        if (customer is null) return NotFound(new { error = "Cliente no encontrado" });

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync(ct);
        return Ok(new { message = "Cliente eliminado" });
    }
}
