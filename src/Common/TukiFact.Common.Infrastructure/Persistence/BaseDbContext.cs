using TukiFact.Common.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// Base of every <c>&lt;Module&gt;DbContext</c>: default schema owned by the module, configurations from the
/// module assembly, audit columns on every table and <c>sync_version</c> on aggregate roots.
/// Register it with <c>AddModuleDbContext</c>, which wires the shared connection, snake_case naming and the
/// domain events, auditable and sync_version interceptors. No domain logic lives here.
/// </summary>
public abstract class BaseDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>Postgres schema owned by the module (lowercase, singular: <c>sales</c>, <c>identity</c>).</summary>
    protected abstract string Schema { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        AddInfrastructureColumns(modelBuilder);
    }

    private static void AddInfrastructureColumns(ModelBuilder modelBuilder)
    {
        var tableRootTypes = modelBuilder.Model
            .GetEntityTypes()
            .Where(entityType => entityType is { BaseType: null, HasSharedClrType: false }
                && !entityType.IsOwned()
                && entityType.FindPrimaryKey() is not null)
            .Select(entityType => entityType.ClrType)
            .ToList();

        foreach (var clrType in tableRootTypes)
        {
            var entity = modelBuilder.Entity(clrType);
            entity.Property<DateTimeOffset>(ShadowProperties.CreatedAt);
            entity.Property<Guid?>(ShadowProperties.CreatedBy);
            entity.Property<DateTimeOffset?>(ShadowProperties.UpdatedAt);
            entity.Property<Guid?>(ShadowProperties.UpdatedBy);

            if (typeof(IAggregateRoot).IsAssignableFrom(clrType))
            {
                entity.Property<long>(ShadowProperties.SyncVersion).IsConcurrencyToken();
            }
        }
    }
}
