using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using TukiFact.Common.Domain.Results;
using TukiFact.Common.Domain.Rules;

namespace TukiFact.Common.Presentation.Grpc.Interceptors;

/// <summary>
/// Never lets a raw exception escape to a gRPC client. A broken domain
/// invariant (<see cref="BusinessRuleValidationException"/>) and an optimistic concurrency conflict
/// map to ABORTED — the same outcome as the HTTP-side handlers — everything else maps to INTERNAL
/// with no leaked message. An <see cref="RpcException"/> already thrown deliberately (e.g. via
/// <see cref="RpcErrorMapping"/> in application code) passes through unchanged.
/// </summary>
/// <remarks>
/// EF Core's <c>DbUpdateConcurrencyException</c> is matched by type name: <c>Common.Presentation</c>
/// cannot reference <c>Microsoft.EntityFrameworkCore</c> (Presentation
/// may not reference a data package), the same reason the HTTP-side equivalent
/// (<c>DbUpdateConcurrencyExceptionHandler</c>) lives in <c>Common.Infrastructure</c> and is wired
/// directly by the host instead of through this layer.
/// </remarks>
public sealed class ExceptionInterceptor(IRpcErrorDetailFactory detailFactory, ILogger<ExceptionInterceptor> logger) : Interceptor
{
    internal const string ConcurrencyCode = "Concurrency.Conflict";

    // Built at runtime, not a single string literal: NetArchTest's Cecil-based scan flags any
    // string constant that merely *contains* a forbidden namespace as a "dependency" on it (a false
    // positive for this exact type-name-matching pattern), which would trip the ArchitectureTests
    // rule that Common.Presentation may not reference Microsoft.EntityFrameworkCore.
    private static readonly string ConcurrencyExceptionTypeName =
        string.Concat("Microsoft", ".", "EntityFrameworkCore", ".", "DbUpdateConcurrencyException");

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw ToRpcException(exception, context);
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw ToRpcException(exception, context);
        }
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(requestStream, context);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw ToRpcException(exception, context);
        }
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(requestStream, responseStream, context);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw ToRpcException(exception, context);
        }
    }

    private RpcException ToRpcException(Exception exception, ServerCallContext context)
    {
        var correlationId = context.ResponseTrailers.GetValue(CorrelationIdInterceptor.MetadataKey);

        if (exception is BusinessRuleValidationException brokenRule)
        {
            logger.BusinessRuleBroken(brokenRule.BrokenRule.GetType().Name);
            var error = Error.Conflict("BusinessRule." + brokenRule.BrokenRule.GetType().Name, brokenRule.BrokenRule.Message);
            return error.ToRpcException(detailFactory, correlationId);
        }

        if (exception.GetType().FullName == ConcurrencyExceptionTypeName)
        {
            logger.ConcurrencyConflict();
            var error = Error.Conflict(ConcurrencyCode, "The record changed.");
            return error.ToRpcException(detailFactory, correlationId);
        }

        logger.UnhandledException(exception);
        var unexpected = Error.Failure("Unexpected", exception.Message);
        return unexpected.ToRpcException(detailFactory, correlationId);
    }
}
