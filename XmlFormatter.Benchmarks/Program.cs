namespace XmlFormatter.Benchmarks;

/// <summary>
/// Perf harness for the formatter.
///
///   generate                      write the sample corpus (deterministic, gitignored)
///   bench                         time the CLI over a sample directory
///   guard                         fail if formatting cost grows faster than input size
/// </summary>
internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length is 0)
        {
            Usage();
            return 1;
        }

        try
        {
            return args[0] switch
            {
                "generate" => Generate(),
                "bench" => Bench.Run(sampleDir: Argument(args, "--samples") ?? PerfPaths.SampleDir,
                                     optionSet: Argument(args, "--options") ?? "default",
                                     save: Argument(args, "--save"),
                                     compare: Argument(args, "--compare")),
                "guard" => ScalingGuard.Run(tolerance: double.TryParse(Argument(args, "--tolerance"), out var value) ? value
                        : ScalingGuard.Tolerance),
                "-h" or "--help" or "help" => Usage(),
                _ => Unknown(args[0]),
            };
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static int Generate()
    {
        SampleGenerator.GenerateAll();
        return 0;
    }

    /// <summary>Value of a `--flag value` pair, or null when the flag is absent.</summary>
    private static string? Argument(string[] args, string flag)
    {
        var index = Array.IndexOf(args, flag);

        return index >= 0 && index + 1 < args.Length ?
               args[index + 1] :
               null;
    }

    private static int Unknown(string verb)
    {
        Console.Error.WriteLine($"Unknown command '{verb}'.");
        Usage();
        return 1;
    }

    private static int Usage()
    {
        Console.WriteLine("""
            XmlFormatter perf harness.

              generate
                  Write the deterministic sample corpus under perf/samples.
                  Everything it writes is gitignored - regenerate rather than commit.

              bench [--samples DIR] [--options NAME] [--save FILE] [--compare FILE]
                  Time the CLI over every *.xml in DIR (default perf/samples), reporting
                  total round trip, fixed .NET startup, and the work left once startup is
                  subtracted. Requires: dotnet build -c Release

                  --options  default | preserve-newlines | blank-lines

              guard [--tolerance N]
                  Format the same document at two sizes and fail if work grows more than
                  N times faster than input (default 1.5). This is the check that catches
                  a performance regression - the output baselines in XmlFormatter.Tests
                  cannot, because a slow formatter still produces identical bytes.

            Examples:
              dotnet build -c Release
              dotnet run -c Release --project XmlFormatter.Benchmarks -- generate
              dotnet run -c Release --project XmlFormatter.Benchmarks -- bench --save perf/baseline.json
              dotnet run -c Release --project XmlFormatter.Benchmarks -- bench --compare perf/baseline.json
              dotnet run -c Release --project XmlFormatter.Benchmarks -- guard
            """);

        return 0;
    }
}
