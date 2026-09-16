namespace TukiFact.Common.Domain.Results;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,

    /// <summary>
    /// 401/403 are normally surfaced by the host; this exists for use cases that must return
    /// that outcome as a <c>Result</c>. Appended last to keep the other numeric values stable.
    /// </summary>
    Unauthorized = 4,

    /// <summary>
    /// An authenticated caller lacking the required permission, distinct from <see cref="Unauthorized"/>
    /// (needed to tell 401 UNAUTHENTICATED apart from 403 PERMISSION_DENIED on the gRPC side).
    /// Appended last to keep the other numeric values stable.
    /// </summary>
    Forbidden = 5,
}
