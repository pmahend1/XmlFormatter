using System.Diagnostics;

namespace XmlFormatter.Benchmarks;

/// <summary>
/// The only check here that can actually catch a performance regression.
///
/// The baseline suite in XmlFormatter.Tests compares output, and the O(n^2) traversal that
/// prompted all of this produced byte-identical output - no output comparison could ever
/// have flagged it. What gives it away is the *shape* of the cost curve, so this measures
/// the same document at two sizes and asserts that work grows no faster than input.
///
/// Ratios, not absolute times: a slow CI runner moves both measurements together and the
/// ratio survives. That is also why this runs in-process rather than through the CLI -
/// both halves then share one JIT and one heap, and only the size differs.
/// </summary>
internal static class ScalingGuard
{
    /// <summary>How much steeper than linear is tolerated before this is called a regression.</summary>
    public const double Tolerance = 1.5;

    private const int SmallRecords = 4_000;
    private const int LargeRecords = 16_000;
    private const int RunsPerMeasurement = 5;

    private static readonly ScalingCase[] Cases =
    [
        new("orders/default", "default", PreFormatted: false),

        /*
         * Already-formatted input with PreserveNewLines on: the editor's real case, and the
         * more demanding one - retained whitespace nodes roughly double the sibling count.
         */
        new("orders/preserve-newlines",
            "preserve-newlines",
            PreFormatted: true,
            KnownFailing: "PreserveNewLines on already-formatted input is still superlinear - "
                        + "the ChildNodes fix in #37 did not reach this path."),
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

        /*
         * Fastest of five, not the median. Noise on this machine only ever adds time - a GC
         * pause, a descheduled thread - so the quickest run is the closest to the work the
         * formatter actually does, and it is far steadier across runs than the median.
         */
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
