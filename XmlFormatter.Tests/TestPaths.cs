using System.Runtime.CompilerServices;

namespace XmlFormatter.Tests;

/// <summary>
/// Locates the repo's Sample/ fixtures and this project's Baseline/ directory from the
/// source path, so the tests read the real fixture files rather than copies staged
/// into the build output.
/// </summary>
public static class TestPaths
{
    public static string ProjectDir { get; } = GetProjectDir();

    public static string RepoRoot { get; } = Path.GetFullPath(Path.Combine(ProjectDir, ".."));

    public static string FixtureDir { get; } = Path.Combine(RepoRoot, "Sample");

    public static string BaselineDir { get; } = Path.Combine(ProjectDir, "Baseline");

    /// <summary>Sample/*.xml, excluding the Formatted_* output the Sample program writes.</summary>
    public static List<string> Fixtures() =>
        Directory.GetFiles(FixtureDir, "*.xml")
                 .Where(path => !Path.GetFileName(path).StartsWith("Formatted_", StringComparison.Ordinal))
                 .OrderBy(path => path, StringComparer.Ordinal)
                 .ToList();

    private static string GetProjectDir([CallerFilePath] string thisFile = "") =>
        Path.GetDirectoryName(thisFile)!;
}
