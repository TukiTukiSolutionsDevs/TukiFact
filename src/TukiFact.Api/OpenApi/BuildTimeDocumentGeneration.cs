using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace TukiFact.Api.OpenApi;

/// <summary>
/// <c>dotnet build</c> writes <c>openapi/tukifact-api-v1.json</c> by running this host inside
/// <c>GetDocument.Insider</c> with a no-op server: the entry point, the registrations and the host
/// start all run, but no dependency or secret exists there, and the environment is Production. In
/// that mode only, settings read while registering services get unreachable placeholders, and the
/// hosted services and startup options validation that the host registers are removed, so nothing
/// connects or validates. Every other registration stays, so endpoint parameters are inferred
/// exactly as at runtime.
/// </summary>
public static class BuildTimeDocumentGeneration
{
    private const string GeneratorAssemblyName = "GetDocument.Insider";

    private static readonly Dictionary<string, string?> PlaceholderSettings = new(StringComparer.Ordinal)
    {
        // Read eagerly by AddNpgSql (Program.cs health checks) and AddJwtBearer's option binding.
        ["ConnectionStrings:DefaultConnection"] = "Host=openapi-generation.invalid;Database=none;Username=none;Password=none",
        // AddCommonPersistence's DatabaseOptions.ValidateOnStart is neutralized below (RemoveAll<IStartupValidator>),
        // but a future module resolving NpgsqlDataSource eagerly would still need this placeholder.
        ["ConnectionStrings:Database"] = "Host=openapi-generation.invalid;Database=none;Username=none;Password=none",
        ["Jwt:Secret"] = "openapi-generation-placeholder-secret-not-a-real-key-000000",
    };

    public static bool IsActive { get; } =
        string.Equals(Assembly.GetEntryAssembly()?.GetName().Name, GeneratorAssemblyName, StringComparison.Ordinal);

    /// <summary>Runs <paramref name="register"/> in document generation mode (see the type remarks).</summary>
    public static WebApplicationBuilder Register(WebApplicationBuilder builder, Action<WebApplicationBuilder> register)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(register);

        builder.Configuration.AddInMemoryCollection(PlaceholderSettings);
        var services = builder.Services;
        var frameworkHostedServices = services.Where(IsHostedService).ToHashSet();

        register(builder);

        services.RemoveAll<IStartupValidator>();
        for (var index = services.Count - 1; index >= 0; index--)
        {
            if (IsHostedService(services[index]) && !frameworkHostedServices.Contains(services[index]))
            {
                services.RemoveAt(index);
            }
        }

        return builder;
    }

    private static bool IsHostedService(ServiceDescriptor descriptor) => descriptor.ServiceType == typeof(IHostedService);
}
