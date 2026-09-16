using Grpc.Core;
using Grpc.Core.Interceptors;
using TukiFact.Common.Application.Abstractions;
using TukiFact.Common.Presentation.Middlewares;

namespace TukiFact.Common.Presentation.Grpc.Interceptors;

/// <summary>
/// Mirrors <c>CorrelationIdMiddleware</c> for the gRPC side: a well-formed
/// incoming <c>x-correlation-id</c> is kept, otherwise the scope's <see cref="ICorrelationIdAccessor"/>
/// default is used. Trailers — not the initial response headers — are the only metadata carrier
/// present on both a successful and a failing gRPC call, so the id is echoed there, and it is set
/// before the continuation runs so it survives an exception.
/// </summary>
public sealed class CorrelationIdInterceptor(ICorrelationIdAccessor correlation) : Interceptor
{
    public const string MetadataKey = "x-correlation-id";

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ApplyCorrelation(context);
        return continuation(request, context);
    }

    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ApplyCorrelation(context);
        return continuation(request, responseStream, context);
    }

    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ApplyCorrelation(context);
        return continuation(requestStream, context);
    }

    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ApplyCorrelation(context);
        return continuation(requestStream, responseStream, context);
    }

    private void ApplyCorrelation(ServerCallContext context)
    {
        var incoming = context.RequestHeaders.GetValue(MetadataKey);
        if (incoming is not null && CorrelationIdMiddleware.IsWellFormed(incoming))
        {
            correlation.Set(incoming);
        }

        context.ResponseTrailers.Add(MetadataKey, correlation.CorrelationId);
    }
}
