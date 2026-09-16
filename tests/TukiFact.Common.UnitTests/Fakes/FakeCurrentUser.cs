using TukiFact.Common.Application.Abstractions;

namespace TukiFact.Common.UnitTests.Fakes;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public static readonly Guid User = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public Guid? UserId => User;

    public Guid? TenantId => Tenant;

    public string? Role => "Admin";
}
