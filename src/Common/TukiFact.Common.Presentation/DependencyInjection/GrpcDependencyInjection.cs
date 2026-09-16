using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TukiFact.Common.Presentation.Grpc;
using TukiFact.Common.Presentation.Grpc.Interceptors;

namespace TukiFact.Common.Presentation.DependencyInjection;

public static class GrpcDependencyInjection
{
    /// <summary>
    /// Registers the gRPC server plus its interceptors in a fixed order:
    /// <see cref="CorrelationIdInterceptor"/> first (so the id is already set when a later
    /// interceptor needs it), then <see cref="ExceptionInterceptor"/> (so no module interceptor —
    /// or module code — can leak a raw exception past this point). A third slot is reserved for an
    /// API-key interceptor, added in the Identity phase; it is intentionally absent for now.
    /// No <c>MapGrpcService</c> call is made here — no gRPC service is registered yet.
    /// </summary>
    public static IServiceCollection AddCommonGrpc(this IServiceCollection services, Action<GrpcOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new GrpcOptions();
        configure?.Invoke(options);

        services.TryAddSingleton<IRpcErrorDetailFactory, NullRpcErrorDetailFactory>();

        services.AddGrpc(grpc =>
        {
            grpc.Interceptors.Add<CorrelationIdInterceptor>();
            grpc.Interceptors.Add<ExceptionInterceptor>();
            // reserved: grpc.Interceptors.Add<ApiKeyInterceptor>(); — lands with the Identity phase.
        });

        return services;
    }
}
