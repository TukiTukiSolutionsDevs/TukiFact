using TukiFact.Common.IntegrationTests.Persistence.Fakes;

namespace TukiFact.Common.IntegrationTests.Persistence.Interceptors;

[Collection(KernelPostgresCollection.Name)]
public sealed class AuditableInterceptorTests(KernelTestFixture fixture)
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 1, 13, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 9, 2, 8, 15, 0, TimeSpan.Zero);
    private static readonly Guid CreatorId = Guid.Parse("6f1d6d2e-0d2c-4d3a-9c6e-0a4f1b2c3d4e");
    private static readonly Guid EditorId = Guid.Parse("a2b3c4d5-e6f7-4a8b-9c0d-1e2f3a4b5c6d");

    [Fact]
    public async Task SaveChanges_NewAggregate_SetsCreatedAuditFromClockAndCurrentUser()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        fixture.Clock.UtcNow = CreatedAt;

        // Act
        await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderId, tenantId, "draft"), tenantId, CreatorId);

        // Assert
        var row = await fixture.ReadOrderAsync(orderId, tenantId);
        row.Should().NotBeNull();
        row!.CreatedAt.Should().Be(CreatedAt);
        row.CreatedBy.Should().Be(CreatorId);
        row.UpdatedAt.Should().BeNull();
        row.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task SaveChanges_ModifiedAggregate_SetsUpdatedAuditAndKeepsCreatedAudit()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        fixture.Clock.UtcNow = CreatedAt;
        await fixture.SendAsync(new RegisterKernelTestOrderCommand(orderId, tenantId, "draft"), tenantId, CreatorId);
        fixture.Clock.UtcNow = UpdatedAt;

        // Act
        await fixture.SendAsync(new RelabelKernelTestOrderCommand(orderId, "final"), tenantId, EditorId);

        // Assert
        var row = await fixture.ReadOrderAsync(orderId, tenantId);
        row.Should().NotBeNull();
        row!.CreatedAt.Should().Be(CreatedAt);
        row.CreatedBy.Should().Be(CreatorId, "an update must never rewrite the creator");
        row.UpdatedAt.Should().Be(UpdatedAt);
        row.UpdatedBy.Should().Be(EditorId);
    }
}
