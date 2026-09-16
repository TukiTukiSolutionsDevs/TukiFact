namespace TukiFact.Common.Presentation.Endpoints;

/// <summary>
/// Who may call an endpoint, resolved by <see cref="EndpointExtensions.MapEndpoints"/> once every
/// convention of the endpoint is applied. The OpenAPI document derives the bearer requirement and
/// its permissions from it.
/// </summary>
public sealed record EndpointAccessMetadata(bool RequiresAuthentication, IReadOnlyList<string> Permissions)
{
    public static EndpointAccessMetadata Anonymous { get; } = new(false, []);
}
