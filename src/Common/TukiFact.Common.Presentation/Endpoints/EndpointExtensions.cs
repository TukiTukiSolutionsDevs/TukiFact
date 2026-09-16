using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace TukiFact.Common.Presentation.Endpoints;

public static class EndpointExtensions
{
    private const string ModuleAssemblyPrefix = "TukiFact.Modules.";
    private const string PresentationAssemblySuffix = ".Presentation";
    private const string ProblemJson = "application/problem+json";

    /// <summary>
    /// Registers every concrete <see cref="IEndpoint"/> of the given module Presentation assemblies.
    /// The module name comes from the assembly name <c>TukiFact.Modules.&lt;Module&gt;.Presentation</c>.
    /// </summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        services.AddOptions<EndpointModuleOptions>();
        foreach (var assembly in assemblies.Distinct())
        {
            services.AddEndpoints(ModuleNameOf(assembly), assembly);
        }

        return services;
    }

    /// <summary>Registers every concrete <see cref="IEndpoint"/> of <paramref name="assembly"/> under an explicit module name.</summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, string moduleName, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        ArgumentNullException.ThrowIfNull(assembly);

        services.Configure<EndpointModuleOptions>(options => options.Modules[assembly] = moduleName);

        ServiceDescriptor[] endpoints =
        [
            .. assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IEndpoint)))
                .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type)),
        ];

        services.TryAddEnumerable(endpoints);
        return services;
    }

    /// <summary>
    /// URL segment versioning for <see cref="MapEndpoints"/>: no implicit default version (an
    /// unversioned route does not exist) and <c>api-supported-versions</c> reported on every
    /// versioned response. A URL with an unsupported version matches no endpoint, so it is a 404
    /// ProblemDetails like any unknown route.
    /// </summary>
    public static IApiVersioningBuilder AddEndpointVersioning(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = ApiVersions.V1;
            options.AssumeDefaultVersionWhenUnspecified = false;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
    }

    /// <summary>
    /// Maps every registered <see cref="IEndpoint"/> under <see cref="ApiVersions.RoutePrefix"/> (v1)
    /// in one group that requires an authenticated caller, with one sub-group per module that sets
    /// the OpenAPI tag and <see cref="EndpointModuleMetadata"/>. An endpoint opts out only with
    /// <c>AllowAnonymous()</c> and narrows with <c>RequireAuthorization("&lt;permission&gt;")</c>;
    /// protected endpoints document 401 (and 403 with a permission).
    /// </summary>
    public static RouteGroupBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var modules = app.ServiceProvider.GetRequiredService<IOptions<EndpointModuleOptions>>().Value.Modules;

        var v1 = app.NewVersionedApi()
            .MapGroup(ApiVersions.RoutePrefix)
            .HasApiVersion(ApiVersions.V1)
            .RequireAuthorization();
        ((IEndpointConventionBuilder)v1).Finally(DescribeAccess);

        var endpointsByModule = app.ServiceProvider.GetServices<IEndpoint>()
            .GroupBy(endpoint => ModuleOf(endpoint, modules), StringComparer.Ordinal)
            .OrderBy(module => module.Key, StringComparer.Ordinal);

        foreach (var module in endpointsByModule)
        {
            var moduleGroup = v1.MapGroup(string.Empty)
                .WithTags(module.Key)
                .WithMetadata(new EndpointModuleMetadata(module.Key));

            foreach (var endpoint in module.OrderBy(endpoint => endpoint.GetType().FullName, StringComparer.Ordinal))
            {
                endpoint.MapEndpoint(moduleGroup);
            }
        }

        return v1;
    }

    /// <summary>Runs after the endpoint conventions, so <c>AllowAnonymous()</c> and permission policies are final.</summary>
    private static void DescribeAccess(EndpointBuilder endpoint)
    {
        var metadata = endpoint.Metadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            metadata.Add(EndpointAccessMetadata.Anonymous);
            return;
        }

        var authorization = metadata.OfType<IAuthorizeData>().ToArray();
        string[] permissions =
        [
            .. authorization
                .Select(data => data.Policy)
                .OfType<string>()
                .Where(policy => policy.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];

        metadata.Add(new EndpointAccessMetadata(RequiresAuthentication: true, permissions));
        metadata.Add(Problem(StatusCodes.Status401Unauthorized));
        if (permissions.Length > 0 || Array.Exists(authorization, data => !string.IsNullOrEmpty(data.Roles)))
        {
            metadata.Add(Problem(StatusCodes.Status403Forbidden));
        }
    }

    private static ProducesResponseTypeMetadata Problem(int statusCode) =>
        new(statusCode, typeof(ProblemDetails), [ProblemJson]);

    private static string ModuleOf(IEndpoint endpoint, Dictionary<Assembly, string> modules) =>
        modules.TryGetValue(endpoint.GetType().Assembly, out var module)
            ? module
            : throw new InvalidOperationException(
                $"{endpoint.GetType().FullName} was not registered with AddEndpoints, so its module is unknown.");

    private static string ModuleNameOf(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? string.Empty;
        var module = name.StartsWith(ModuleAssemblyPrefix, StringComparison.Ordinal)
            && name.EndsWith(PresentationAssemblySuffix, StringComparison.Ordinal)
                ? name[ModuleAssemblyPrefix.Length..^PresentationAssemblySuffix.Length]
                : string.Empty;

        return module.Length > 0 && !module.Contains('.', StringComparison.Ordinal)
            ? module
            : throw new InvalidOperationException(
                $"{name} is not named TukiFact.Modules.<Module>.Presentation; use AddEndpoints(moduleName, assembly).");
    }
}
