namespace TukiFact.Common.Application.Abstractions;

/// <summary>Authenticated caller resolved from token claims. Values are null when there is no authenticated user.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    Guid? TenantId { get; }

    string? Role { get; }
}
