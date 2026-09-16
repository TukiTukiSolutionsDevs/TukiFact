using System.Diagnostics;

namespace TukiFact.Common.Application.Behaviors;

/// <summary>
/// Naming for use-case telemetry: activity <c>tukifact.usecase.&lt;UseCase&gt;</c>.
/// The host must register <see cref="ActivitySourceName"/> with OpenTelemetry.
/// </summary>
public static class UseCaseDiagnostics
{
    public const string ActivitySourceName = "TukiFact.Common.Application";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    private static readonly string[] RequestSuffixes = [UseCaseNaming.CommandSuffix, UseCaseNaming.QuerySuffix];

    internal static string UseCaseOf(Type requestType)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        var name = requestType.Name;
        var suffix = Array.Find(
            RequestSuffixes,
            candidate => name.Length > candidate.Length && name.EndsWith(candidate, StringComparison.Ordinal));

        return suffix is null ? name : name[..^suffix.Length];
    }

    internal static string ActivityNameOf(Type requestType) => $"tukifact.usecase.{UseCaseOf(requestType)}";
}
