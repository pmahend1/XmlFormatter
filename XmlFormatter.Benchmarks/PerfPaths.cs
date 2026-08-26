using System.Runtime.CompilerServices;

namespace XmlFormatter.Benchmarks;

/// <summary>
/// Locates the repo and the generated sample directories from the source path, so the
/// harness works the same whether it is launched by `dotnet run` or from a built output.
/// </summary>
internal static class PerfPaths
{
    public static string ProjectDir { get; } = GetProjectDir();

    public static string RepoRoot { get; } = Path.GetFullPath(Path.Combine(ProjectDir, ".."));

    /// <summary>The size ladder - the set the scaling report is meant to read.</summary>
    public static string SampleDir { get; } = Path.Combine(RepoRoot, "perf", "samples");

    /// <summary>Ladder documents that have already been through the formatter once.</summary>
    public static string FormattedDir { get; } = Path.Combine(SampleDir, "formatted");

    /// <summary>Fixed-size documents that each lean on one code path.</summary>
    public static string ShapeDir { get; } = Path.Combine(SampleDir, "shapes");

    public static string DefaultBaseline { get; } = Path.Combine(RepoRoot, "perf", "baseline.json");

    /// <summary>
    /// The CLI the bench shells out to. Release is preferred - a Debug build measures the
    /// wrong thing, so an accidental Debug run says so out loud rather than reporting numbers
    /// nobody can compare against.
    /// </summary>
    public static string FindCommandLineDll()
    {
        foreach (var config in new[] { "Release", "Debug" })
        {
            var candidate = Path.Combine(RepoRoot, "XmlFormatter.CommandLine", "bin", config,
                                         "net10.0", "XmlFormatter.CommandLine.dll");
            if (File.Exists(candidate) is false)
            {
                continue;
            }

            if (config is not "Debug")
            {
                return candidate;
            }
            Console.Error.WriteLine("warning: only a Debug CLI build was found - timings will not be comparable.");
            Console.Error.WriteLine("         run: dotnet build -c Release");

            return candidate;
        }

        throw new FileNotFoundException("No built CLI found. Run: dotnet build -c Release");
    }

    private static string GetProjectDir([CallerFilePath] string thisFile = "") => Path.GetDirectoryName(thisFile)!;
}
