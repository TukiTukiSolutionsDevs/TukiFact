using TukiFact.Common.Application.Abstractions;

namespace TukiFact.Common.IntegrationTests.Persistence.Fakes;

/// <summary>Scoped caller; each test scope sets its own tenant/user before sending a request.</summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }

    public Guid? TenantId { get; set; }

    public string? Role { get; set; }
}
