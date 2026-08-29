using System.Runtime.CompilerServices;

namespace XmlFormatter.Tests;

/// <summary>Resolves paths from the source file, so tests read the real fixtures.</summary>
internal static class TestPaths
{
    private static string ProjectDir { get; } = GetProjectDir();

    private static string RepoRoot { get; } = Path.GetFullPath(Path.Combine(ProjectDir, ".."));

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
