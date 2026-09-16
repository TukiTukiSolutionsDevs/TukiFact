using System.Security.Claims;
using TukiFact.Common.Infrastructure.Authentication;
using Microsoft.AspNetCore.Http;

namespace TukiFact.Common.UnitTests.Authentication;

public sealed class HttpContextCurrentUserTests
{
    private static readonly Guid TenantId = Guid.Parse("0b6d7c2a-5e1f-4c3b-8a9d-2e4f6a8c0b1d");
    private static readonly Guid UserId = Guid.Parse("9c8b7a6d-5e4f-4a3b-2c1d-0e9f8a7b6c5d");

    [Fact]
    public void Properties_AuthenticatedPrincipalWithClaims_ResolveTenantUserAndRole()
    {
        // Arrange
        var sut = BuildSut(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", TenantId.ToString()), new Claim("sub", UserId.ToString()), new Claim("role", "admin")],
                authenticationType: "Bearer")),
        });

        // Act
        var (tenantId, userId, role) = (sut.TenantId, sut.UserId, sut.Role);

        // Assert
        tenantId.Should().Be(TenantId);
        userId.Should().Be(UserId);
        role.Should().Be("admin");
    }

    [Fact]
    public void TenantId_OnlyTenantHeaderOnAnonymousRequest_IsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = TenantId.ToString();
        var sut = BuildSut(context);

        // Act
        var tenantId = sut.TenantId;

        // Assert
        tenantId.Should().BeNull("a request header is caller-controlled and never a tenant source");
    }

    [Fact]
    public void TenantId_TenantClaimIsNotAGuid_IsNull()
    {
        // Arrange
        var sut = BuildSut(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", "not-a-guid")], authenticationType: "Bearer")),
        });

        // Act
        var tenantId = sut.TenantId;

        // Assert
        tenantId.Should().BeNull();
    }

    [Fact]
    public void Properties_NoHttpContext_AreAllNull()
    {
        // Arrange
        var sut = BuildSut(httpContext: null);

        // Act
        var (tenantId, userId, role) = (sut.TenantId, sut.UserId, sut.Role);

        // Assert
        tenantId.Should().BeNull();
        userId.Should().BeNull();
        role.Should().BeNull();
    }

    private static HttpContextCurrentUser BuildSut(HttpContext? httpContext) =>
        new(new HttpContextAccessor { HttpContext = httpContext });
}
