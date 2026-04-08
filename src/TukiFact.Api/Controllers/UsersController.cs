using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TukiFact.Application.DTOs.Users;
using TukiFact.Application.Interfaces;
using TukiFact.Domain.Entities;
using TukiFact.Domain.Enums;
using TukiFact.Domain.Interfaces;

namespace TukiFact.Api.Controllers;

[ApiController]
[Route("v1/users")]
[Authorize(Roles = "admin")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantProvider _tenantProvider;

    public UsersController(IUserRepository userRepo, IPasswordHasher passwordHasher, ITenantProvider tenantProvider)
    {
        _userRepo = userRepo;
        _passwordHasher = passwordHasher;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var users = await _userRepo.GetByTenantAsync(tenantId, ct);
        var response = users.Select(u => new UserResponse(
            u.Id, u.Email, u.FullName, u.Role, u.IsActive, u.LastLoginAt, u.CreatedAt));
        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        if (!UserRole.IsValid(request.Role))
            return BadRequest(new { error = $"Rol inválido. Válidos: {string.Join(", ", UserRole.All)}" });

        var existing = await _userRepo.GetByEmailAsync(request.Email, tenantId, ct);
        if (existing is not null)
            return Conflict(new { error = "Email ya registrado en esta empresa" });

        var user = new User
        {
            TenantId = tenantId,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName,
            Role = request.Role
        };

        await _userRepo.CreateAsync(user, ct);
        var response = new UserResponse(
            user.Id, user.Email, user.FullName, user.Role, user.IsActive, null, user.CreatedAt);
        return Created($"/v1/users/{user.Id}", response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null) return NotFound();

        if (request.FullName is not null) user.FullName = request.FullName;
        if (request.Role is not null)
        {
            if (!UserRole.IsValid(request.Role))
                return BadRequest(new { error = $"Rol inválido. Válidos: {string.Join(", ", UserRole.All)}" });
            user.Role = request.Role;
        }
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        await _userRepo.UpdateAsync(user, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null) return NotFound();

        user.IsActive = false;
        await _userRepo.UpdateAsync(user, ct);
        return NoContent();
    }
}
