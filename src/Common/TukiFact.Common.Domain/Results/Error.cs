namespace TukiFact.Common.Domain.Results;

/// <summary>
/// A failure identified by <see cref="Type"/> and <see cref="Code"/> (api-error-contract.md). <see cref="FieldErrors"/>
/// is empty except for validation errors. Equality compares the field errors by value, in order.
/// </summary>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public IReadOnlyList<FieldError> FieldErrors { get; private init; } = [];

    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);

    /// <summary>A validation error with one <see cref="FieldError"/> per broken rule; the list is copied.</summary>
    public static Error Validation(string code, string description, IEnumerable<FieldError> fieldErrors)
    {
        ArgumentNullException.ThrowIfNull(fieldErrors);

        return new Error(code, description, ErrorType.Validation) { FieldErrors = [.. fieldErrors] };
    }

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    /// <summary>For use cases that must return <see cref="ErrorType.Unauthorized"/> as a Result instead of relying on the host.</summary>
    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public bool Equals(Error? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(Code, other.Code, StringComparison.Ordinal)
            && string.Equals(Description, other.Description, StringComparison.Ordinal)
            && Type == other.Type
            && FieldErrors.SequenceEqual(other.FieldErrors));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Code, StringComparer.Ordinal);
        hash.Add(Description, StringComparer.Ordinal);
        hash.Add(Type);
        foreach (var fieldError in FieldErrors)
        {
            hash.Add(fieldError);
        }

        return hash.ToHashCode();
    }
}
