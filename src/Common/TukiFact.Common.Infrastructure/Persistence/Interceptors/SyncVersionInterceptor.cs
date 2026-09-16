using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TukiFact.Common.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Maintains <c>sync_version</c> on aggregate roots: 1 on insert, +1 on update. The column is a concurrency
/// token, so an update issued from a stale version affects no row and EF raises <see cref="DbUpdateConcurrencyException"/>.
/// </summary>
internal sealed class SyncVersionInterceptor : SaveChangesInterceptor
{
    private const long InitialVersion = 1;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData);
        return ValueTask.FromResult(result);
    }

    private static void Apply(DbContextEventData eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is null)
        {
            return;
        }

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.Metadata.FindProperty(ShadowProperties.SyncVersion) is null)
            {
                continue;
            }

            var syncVersion = entry.Property(ShadowProperties.SyncVersion);
            if (entry.State == EntityState.Added)
            {
                syncVersion.CurrentValue = InitialVersion;
            }
            else if (entry.State == EntityState.Modified)
            {
                syncVersion.CurrentValue = (long)syncVersion.OriginalValue! + 1;
            }
        }
    }
}
