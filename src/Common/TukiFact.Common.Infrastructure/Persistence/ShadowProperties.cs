namespace TukiFact.Common.Infrastructure.Persistence;

/// <summary>
/// Infrastructure-owned shadow properties that <see cref="BaseDbContext"/> adds to every table
/// (snake_case columns <c>created_at</c>, <c>created_by</c>, <c>updated_at</c>, <c>updated_by</c>)
/// and to every aggregate root table (<c>sync_version</c>). Domain types must not declare these names.
/// </summary>
public static class ShadowProperties
{
    public const string CreatedAt = "CreatedAt";
    public const string CreatedBy = "CreatedBy";
    public const string UpdatedAt = "UpdatedAt";
    public const string UpdatedBy = "UpdatedBy";
    public const string SyncVersion = "SyncVersion";
}
