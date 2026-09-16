namespace TukiFact.Common.Presentation.Endpoints;

/// <summary>
/// The module that owns an endpoint, added by <see cref="EndpointExtensions.MapEndpoints"/>: its
/// OpenAPI tag and the prefix of its operationId (<c>&lt;Module&gt;_&lt;UseCase&gt;</c>).
/// </summary>
public sealed record EndpointModuleMetadata(string Name);
