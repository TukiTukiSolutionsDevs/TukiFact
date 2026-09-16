using System.Reflection;

namespace TukiFact.Common.Presentation.Endpoints;

/// <summary>Module name of each registered endpoint assembly (<see cref="EndpointExtensions"/>).</summary>
internal sealed class EndpointModuleOptions
{
    public Dictionary<Assembly, string> Modules { get; } = [];
}
