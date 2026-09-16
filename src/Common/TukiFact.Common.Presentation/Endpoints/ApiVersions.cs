using Asp.Versioning;

namespace TukiFact.Common.Presentation.Endpoints;

/// <summary>
/// URL segment versioning: every module endpoint is served under <see cref="RoutePrefix"/>, one
/// OpenAPI document per major version. A breaking change ships as a new major version; additive
/// changes stay in the current one.
/// </summary>
public static class ApiVersions
{
    public const string RoutePrefix = "/api/v{version:apiVersion}";

    public static ApiVersion V1 { get; } = new(1, 0);
}
