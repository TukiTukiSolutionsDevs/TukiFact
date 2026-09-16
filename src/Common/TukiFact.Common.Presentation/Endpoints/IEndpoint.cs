using Microsoft.AspNetCore.Routing;

namespace TukiFact.Common.Presentation.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
