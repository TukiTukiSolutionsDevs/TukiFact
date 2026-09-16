using TukiFact.Common.Application.Abstractions;

namespace TukiFact.Common.UnitTests.Abstractions;

/// <summary>
/// Guards the <see cref="ICurrentUser"/> contract shape: only tenant-agnostic invoicing properties are exposed.
/// Domain-specific members (CompanyIds/WarehouseIds/CashRegisterId) must never come back.
/// </summary>
public sealed class CurrentUserShapeTests
{
    [Fact]
    public void ICurrentUser_Always_ExposesExactlyUserIdTenantIdAndRole()
    {
        // Act
        var properties = typeof(ICurrentUser).GetProperties();

        // Assert
        properties.Select(property => property.Name).Should().BeEquivalentTo(
            nameof(ICurrentUser.UserId),
            nameof(ICurrentUser.TenantId),
            nameof(ICurrentUser.Role));
    }

    [Fact]
    public void UserId_Always_IsNullableGuid()
    {
        // Act & Assert
        typeof(ICurrentUser).GetProperty(nameof(ICurrentUser.UserId))!.PropertyType.Should().Be(typeof(Guid?));
    }

    [Fact]
    public void TenantId_Always_IsNullableGuid()
    {
        // Act & Assert
        typeof(ICurrentUser).GetProperty(nameof(ICurrentUser.TenantId))!.PropertyType.Should().Be(typeof(Guid?));
    }

    [Fact]
    public void Role_Always_IsNullableString()
    {
        // Act & Assert
        typeof(ICurrentUser).GetProperty(nameof(ICurrentUser.Role))!.PropertyType.Should().Be(typeof(string));
    }
}
