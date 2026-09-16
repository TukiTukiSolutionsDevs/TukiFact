using System.Security.Claims;
using TukiFact.Common.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace TukiFact.Common.Infrastructure.Authentication;

/// <summary>
/// Resolves the current caller from JWT claims only (<c>tenant_id</c>, <c>sub</c>, <c>role</c>). Request headers
/// are caller-controlled and are never consulted: an anonymous request has no tenant, so the RLS hook fails closed.
/// </summary>
internal sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private const string TenantClaimType = "tenant_id";
    private const string SubjectClaimType = "sub";
    private const string RoleClaimType = "role";

    public Guid? TenantId => ResolveTenantId();

    public Guid? UserId => ResolveUserId();

    public string? Role => ResolveRole();

    private Guid? ResolveTenantId()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        return Guid.TryParse(context.User.FindFirst(TenantClaimType)?.Value, out var tenantId) ? tenantId : null;
    }

    private Guid? ResolveUserId()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        // JwtBearer remaps "sub" to ClaimTypes.NameIdentifier by default; the raw claim type is checked first
        // so a token that disables claim mapping still resolves.
        var claimValue = context.User.FindFirst(SubjectClaimType)?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }

    private string? ResolveRole()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        return context.User.FindFirst(RoleClaimType)?.Value ?? context.User.FindFirst(ClaimTypes.Role)?.Value;
    }
}
