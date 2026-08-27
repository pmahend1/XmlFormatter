using System.Runtime.CompilerServices;

namespace XmlFormatter.Benchmarks;

// Resolved from the source path, so it works under `dotnet run` and from a built output.
internal static class PerfPaths
{
    private static string ProjectDir { get; } = GetProjectDir();

    public static string RepoRoot { get; } = Path.GetFullPath(Path.Combine(ProjectDir, ".."));

    // The size ladder - the only set the scaling report is meaningful on.
    public static string SampleDir { get; } = Path.Combine(RepoRoot, "perf", "samples");

    public static string FormattedDir { get; } = Path.Combine(SampleDir, "formatted");

    public static string ShapeDir { get; } = Path.Combine(SampleDir, "shapes");

    // Release first: a Debug build measures the wrong thing, so it warns rather than
    // reporting numbers nobody can compare against.
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
