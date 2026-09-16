using TukiFact.Common.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace TukiFact.Common.Infrastructure.Persistence.Interceptors;

/// <summary>Fills <c>created_by/at</c> on insert and <c>updated_by/at</c> on update from <see cref="IClock"/> and <see cref="ICurrentUser"/>.</summary>
internal sealed class AuditableInterceptor(IClock clock, ICurrentUser currentUser) : SaveChangesInterceptor
{
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

    private void Apply(DbContextEventData eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is null)
        {
            return;
        }

        var now = clock.UtcNow;
        var userId = currentUser.UserId;

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.Metadata.FindProperty(ShadowProperties.CreatedAt) is null)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Property(ShadowProperties.CreatedAt).CurrentValue = now;
                entry.Property(ShadowProperties.CreatedBy).CurrentValue = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(ShadowProperties.UpdatedAt).CurrentValue = now;
                entry.Property(ShadowProperties.UpdatedBy).CurrentValue = userId;
                entry.Property(ShadowProperties.CreatedAt).IsModified = false;
                entry.Property(ShadowProperties.CreatedBy).IsModified = false;
            }
        }
    }
}
