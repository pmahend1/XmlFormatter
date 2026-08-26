using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace XmlFormatter.Benchmarks;

/// <summary>
/// Times the formatter end to end by shelling out to the CLI, once per sample.
///
/// A subprocess per run is deliberate: it is what the editor extension actually does, and
/// it gives every measurement a cold JIT and an empty heap instead of letting an earlier
/// sample's warm-up flatter a later one. The cost is a fixed .NET host startup on top of
/// every number, so startup is re-measured with a 1 KB document immediately before each
/// sample and subtracted - a busy machine then skews both halves together rather than
/// inflating the reported work.
/// </summary>
internal static class Bench
{
    #region State

    private const string StartupProbe = """<?xml version="1.0"?><root><a x="1"><b/></a></root>""";

    /// <summary>Above this, one run - each is seconds, and the median of one is the one.</summary>
    private const int SingleRunThreshold = 3_000_000;

    private const string RegenerateHint = "Run: dotnet run --project XmlFormatter.Benchmarks -- generate";

    private static readonly JsonSerializerOptions BaselineJson = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static readonly JsonSerializerOptions CamelCaseJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    #endregion State

    #region Methods

    public static int Run(string sampleDir, string optionSet, string? save, string? compare)
    {
        var paths = SamplesIn(sampleDir);

        if (paths.Count is 0)
        {
            Console.Error.WriteLine($"No samples in {sampleDir}. {RegenerateHint}");
            return 1;
        }

        var dll = PerfPaths.FindCommandLineDll();
        var options = BenchOptions.Resolve(optionSet);
        var baseline = LoadBaseline(compare);

        WriteRunHeader(dll, sampleDir, optionSet);

        var table = new ResultTable(showsComparison: baseline.Count is not 0);
        table.WriteHeader();

        var results = new List<SampleResult>();
        var failures = new List<string>();

        foreach (var path in paths)
        {
            var document = File.ReadAllText(path);
            var name = Path.GetFileName(path);

            Thread.Sleep(1000); // let the machine settle

            try
            {
                var result = MeasureSample(dll, name, document, options);
                results.Add(result);
                ResultTable.WriteRow(result, baseline.GetValueOrDefault(name));
            }
            catch (FormatterFailedException failure)
            {
                /*
                 * One sample the formatter cannot handle should not cost us the timings for
                 * all the others. Record it, print a placeholder row, and carry on - the
                 * failures are reported together at the end and set the exit code.
                 */
                ResultTable.WriteFailedRow(name, document.Length);
                failures.Add($"{name}: {failure.Message}");
            }
        }

        table.WriteFooter();
        ReportScaling(results);

        if (save is not null)
        {
            Save(save, results);
            Console.WriteLine($"\nsaved to {Path.GetRelativePath(PerfPaths.RepoRoot, save)}");
        }

        if (failures.Count is not 0)
        {
            return 0;
        }

        Console.Error.WriteLine();
        Console.Error.WriteLine($"{failures.Count} sample(s) the formatter could not process:");
        failures.ForEach(failure => Console.Error.WriteLine($"  {failure}"));
        return 1;
    }

    private static List<string> SamplesIn(string sampleDir)
    {
        return Directory.Exists(sampleDir) is false
            ? []
            : [.. Directory.GetFiles(sampleDir, "*.xml").OrderBy(path => path, StringComparer.Ordinal)];
    }

    private static Dictionary<string, SampleResult> LoadBaseline(string? path)
    {
        return path is null ? [] : Load(path).ToDictionary(result => result.Sample, StringComparer.Ordinal);
    }

    private static void WriteRunHeader(string dll, string sampleDir, string optionSet)
    {
        Console.WriteLine($"CLI:     {Path.GetRelativePath(PerfPaths.RepoRoot, dll)}");
        Console.WriteLine($"samples: {Path.GetRelativePath(PerfPaths.RepoRoot, sampleDir)}");
        Console.WriteLine($"options: {optionSet}");
        Console.WriteLine();
    }

    private static SampleResult MeasureSample(string dll,
                                              string name,
                                              string document,
                                              Options options)
    {
        var startup = MeasureStartup(dll, options);
        TimeOnce(dll, document, options); // warm the file cache

        var runs = document.Length < SingleRunThreshold ? 3 : 1;
        var measured = Enumerable.Range(0, runs)
                                 .Select(_ => TimeOnce(dll, document, options))
                                 .ToList();

        var total = Median(measured.Select(measurement => measurement.ElapsedMs));
        var work = Math.Max(total - startup, 0.0);

        return new SampleResult(Sample: name,
                                InputBytes: document.Length,
                                OutputBytes: measured[^1].OutputBytes,
                                TotalMs: Round(total),
                                StartupMs: Round(startup),
                                WorkMs: Round(work));
    }

    /// <summary>
    /// Work should grow no faster than size. Anything meaningfully steeper is the shape of
    /// bug this harness exists to catch, so it gets called out rather than left to be read
    /// off the table.
    /// </summary>
    private static void ReportScaling(IReadOnlyList<SampleResult> results)
    {
        /*
         * Only a size ladder has a scaling story. The shape corpus is five unrelated
         * documents in alphabetical order, and comparing neighbours there produces ratios
         * that look alarming and mean nothing.
         */
        var isAscendingLadder = results.Zip(results.Skip(1))
                                       .All(pair => pair.Second.InputBytes > pair.First.InputBytes);

        if (isAscendingLadder is false)
        {
            Console.WriteLine("samples are not an ascending size ladder - no scaling to report.");
            return;
        }

        Console.WriteLine("scaling of formatting work (fixed startup excluded):");

        foreach (var (earlier, later) in results.Zip(results.Skip(1)))
        {
            if (earlier.IsMeasurable is false)
            {
                continue;
            }

            var sizeFactor = (double)later.InputBytes / earlier.InputBytes;
            var workFactor = later.WorkMs / earlier.WorkMs;
            var flag = workFactor > sizeFactor * ScalingGuard.Tolerance ? "  <-- superlinear" : "";

            Console.WriteLine($"  {earlier.Sample} -> {later.Sample}: size x{sizeFactor:F1}, work x{workFactor:F1}{flag}");
        }
    }

    private static Measurement TimeOnce(string dll, string document, Options options)
    {
        var input = new JsonInputDto
        {
            XMLString = document,
            ActionKind = FormattingActionKind.Format,
            FormattingOptions = options,
        };
        var payload = JsonSerializer.SerializeToUtf8Bytes(value: input, options: CamelCaseJsonOptions);

        var info = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };
        info.ArgumentList.Add(dll);

        var started = Stopwatch.GetTimestamp();

        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start dotnet");

        /*
         * All three pipes have to be serviced concurrently. Writing stdin from this thread
         * deadlocks once a document outgrows the pipe buffer; draining stdout before stderr
         * deadlocks too, and less obviously - a crashing runtime dumps a stack trace far
         * larger than the stderr buffer, so the child blocks on a write nobody is reading
         * while this side waits for a stdout EOF that will never come.
         */
        var writer = Task.Run(() => WritePayload(process, payload));
        var outReader = Task.Run(process.StandardOutput.ReadToEnd);
        var errReader = Task.Run(process.StandardError.ReadToEnd);

        Task.WaitAll(writer, outReader, errReader);
        process.WaitForExit();

        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        return process.ExitCode is not 0 ?
            throw new FormatterFailedException(process.ExitCode, Truncate(errReader.Result.Trim(), 300))
            : new Measurement(elapsed, outReader.Result.Length);
    }

    private static void WritePayload(Process process, byte[] payload)
    {
        using var stdin = process.StandardInput.BaseStream;

        try
        {
            stdin.Write(payload);
        }
        catch (IOException)
        {
            // Child died before it read the whole document - its exit code is the story.
        }
    }

    /// <summary>Fixed .NET host startup, measured next to the sample it is subtracted from.</summary>
    private static double MeasureStartup(string dll, Options options, int runs = 4)
    {
        return Median(Enumerable.Range(0, runs).Select(_ => TimeOnce(dll, StartupProbe, options).ElapsedMs));
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToList();
        var middle = sorted.Count / 2;

        return sorted.Count % 2 is 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }

    private static void Save(string path, IReadOnlyList<SampleResult> results)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(results, BaselineJson));
    }

    private static IReadOnlyList<SampleResult> Load(string path) => JsonSerializer.Deserialize<List<SampleResult>>(File.ReadAllText(path), BaselineJson)
                                                                 ?? throw new InvalidOperationException($"Could not read baseline {path}");

    private static double Round(double value) => Math.Round(value, 1);

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max];

    #endregion Methods
}
