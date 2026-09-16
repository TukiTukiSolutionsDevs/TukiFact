namespace TukiFact.TestSupport;

/// <summary>
/// Walks up from the test's base directory to the directory containing <c>TukiFact.slnx</c>,
/// so <c>TukiFact.ArchitectureTests</c> can discover every project under
/// <c>src/</c> and <c>tests/</c> without hard-coding a path.
/// </summary>
public static class SolutionRoot
{
    private static readonly Lazy<string> LazyPath = new(Find);

    public static string Path => LazyPath.Value;

    private static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (directory.EnumerateFiles("*.slnx").Any())
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"No directory with a .slnx above {AppContext.BaseDirectory}.");
    }
}
