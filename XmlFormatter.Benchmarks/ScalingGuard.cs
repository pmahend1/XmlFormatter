using System.Diagnostics;

namespace XmlFormatter.Benchmarks;

/// <summary>
/// The only check that can catch a performance regression: the O(n^2) traversal produced
/// byte-identical output, so no baseline could have flagged it. Measures one document at two
/// sizes and asserts work grows no faster than input.
///
/// Ratios, not absolute times, and in-process - so a slow runner moves both halves together.
/// </summary>
internal static class ScalingGuard
{
    /// <summary>How much steeper than linear is tolerated before this is called a regression.</summary>
    public const double Tolerance = 1.5;

    /*
     * Both sizes sit above the point where the DOM stops fitting the cache, because a two-point
     * ratio only means anything when both points are in the same memory regime. At 4,000 records
     * the working set is still cheap per character and at 16,000 it is not, so that pairing
     * charged the step between them to the formatter and read ~1.5x linear on a path measured
     * flat at five sizes. From 8,000 records on, cost per character is level.
     */
    private const int SmallRecords = 8_000;
    private const int LargeRecords = 32_000;
    private const int RunsPerMeasurement = 5;

    private static readonly ScalingCase[] Cases =
    [
        new("orders/default", "default", PreFormatted: false),

        // The editor's real case: retained whitespace roughly doubles the sibling count.
        new("orders/preserve-newlines", "preserve-newlines", PreFormatted: true),
    ];

    public static int Run(double tolerance)
    {
        Console.WriteLine($"scaling guard: {SmallRecords:N0} vs {LargeRecords:N0} records, "
                        + $"tolerance {tolerance:F2}x linear");
        Console.WriteLine();

        var header = $"{"case",-28}{"size",8}{"work",8}{"vs linear",12}{"result",10}";
        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length));

        Warmup();

        var verdicts = new List<Verdict>();

        foreach (var scenario in Cases)
        {
            var verdict = Evaluate(scenario, tolerance);
            verdicts.Add(verdict);

            Console.WriteLine($"{verdict.Scenario.Name,-28}{$"x{verdict.SizeFactor:F1}",8}"
                            + $"{$"x{verdict.WorkFactor:F1}",8}{$"{verdict.Steepness:F2}x",12}"
                            + $"{verdict.Label,10}");
        }

        Console.WriteLine(new string('-', header.Length));

        foreach (var verdict in verdicts.Where(verdict => verdict.IsKnownFailure))
        {
            Console.WriteLine($"known: {verdict.Scenario.Name} - {verdict.Scenario.KnownFailing}");
        }

        foreach (var verdict in verdicts.Where(verdict => verdict.IsUnexpectedlyFixed))
        {
            Console.WriteLine($"FIXED: {verdict.Scenario.Name} is linear again - drop its KnownFailing flag.");
        }

        var regressions = verdicts.Where(verdict => verdict.IsRegression)
                                  .Select(verdict => verdict.Scenario.Name)
                                  .ToList();

        if (regressions.Count is not 0)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"Formatting cost is growing faster than input size: {string.Join(", ", regressions)}");
            Console.Error.WriteLine("Look for per-child work that rescans from the start of a sibling list -");
            Console.Error.WriteLine("XmlNode.ChildNodes is a lazy view, so both .Count and [i] walk the list.");
            return 1;
        }

        Console.WriteLine("no new regressions.");
        return 0;
    }

    private static Verdict Evaluate(ScalingCase scenario, double tolerance)
    {
        var options = BenchOptions.Resolve(scenario.OptionSet);

        var small = Measure(SmallRecords, options, scenario.PreFormatted);
        var large = Measure(LargeRecords, options, scenario.PreFormatted);

        return new Verdict(scenario,
                           SizeFactor: (double)large.InputChars / small.InputChars,
                           WorkFactor: large.Milliseconds / small.Milliseconds,
                           tolerance);
    }

    /// <summary>JIT everything on a small document so the first real measurement is not the outlier.</summary>
    private static void Warmup()
    {
        var document = SampleGenerator.Orders(500);
        var formatter = new Formatter();

        foreach (var options in BenchOptions.All.Values)
        {
            formatter.Format(document, formattingOptions: options);
        }
    }

    private static ScalingPoint Measure(int records, Options options, bool preFormatted)
    {
        var formatter = new Formatter();
        var minified = SampleGenerator.Orders(records);
        var document = preFormatted ? formatter.Format(minified) : minified;

        // Fastest of five, not the median: noise only ever adds time, so the quickest run is
        // closest to the real work and far steadier across runs.
        var fastest = double.MaxValue;

        for (var run = 0; run < RunsPerMeasurement; run++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var started = Stopwatch.GetTimestamp();
            formatter.Format(document, formattingOptions: options);
            fastest = Math.Min(fastest, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

        return new ScalingPoint(document.Length, fastest);
    }
}
