namespace TukiFact.Common.Domain.Results;

/// <summary>
/// One broken validation rule (api-error-contract.md §4): <paramref name="Field"/> is the camelCase JSON path in the
/// request body (<c>items[0].quantity</c>), <paramref name="Code"/> the stable rule code.
/// </summary>
public sealed record FieldError(string Field, string Code, string Description);
