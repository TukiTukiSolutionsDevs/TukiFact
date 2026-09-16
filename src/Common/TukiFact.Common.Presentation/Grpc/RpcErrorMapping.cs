using Google.Protobuf.WellKnownTypes;
using Google.Rpc;
using Grpc.Core;
using TukiFact.Common.Domain.Results;

namespace TukiFact.Common.Presentation.Grpc;

/// <summary>
/// The only translation from <see cref="Error"/> to a gRPC <see cref="RpcException"/>:
/// a <c>google.rpc.Status</c> carries the mapped code and message, plus
/// an optional detail message from <see cref="IRpcErrorDetailFactory"/>. Trailers are not the error
/// channel here — <c>x-correlation-id</c> is echoed separately by <c>CorrelationIdInterceptor</c>.
/// </summary>
public static class RpcErrorMapping
{
    /// <summary>A Failure is a server-side fault: its description may name internals, so only a fixed message leaves the API.</summary>
    private const string FailureMessage = "An unexpected error occurred.";

    public static RpcException ToRpcException(this Error error, IRpcErrorDetailFactory detailFactory, string? correlationId)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(detailFactory);

        var status = new Google.Rpc.Status
        {
            Code = (int)CodeOf(error.Type),
            Message = error.Type == ErrorType.Failure ? FailureMessage : error.Description,
        };

        var detail = detailFactory.Create(error, correlationId);
        if (detail is not null)
        {
            status.Details.Add(Any.Pack(detail));
        }

        return status.ToRpcException();
    }

    private static Code CodeOf(ErrorType type) =>
        type switch
        {
            ErrorType.Validation => Code.InvalidArgument,
            ErrorType.Unauthorized => Code.Unauthenticated,
            ErrorType.Forbidden => Code.PermissionDenied,
            ErrorType.NotFound => Code.NotFound,
            ErrorType.Conflict => Code.Aborted,
            _ => Code.Internal,
        };
}
