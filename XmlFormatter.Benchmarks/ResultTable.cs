namespace XmlFormatter.Benchmarks;

/// <summary>Renders the per-sample results as a fixed-width console table.</summary>
internal sealed class ResultTable(bool showsComparison)
{
    private readonly string _header = $"{"sample",-18}{"input",9}{"output",9}{"total",9}{"startup",9}{"work",9}"
                                      + (showsComparison ? $"{"vs base",10}" : "");

    public void WriteHeader()
    {
        Console.WriteLine(_header);
        WriteRule();
    }

    public void WriteFooter() => WriteRule();

    public static void WriteRow(SampleResult result, SampleResult? baseline)
    {
        var line = $"{result.Sample,-18}{result.InputBytes / 1024.0,8:F0}K{result.OutputBytes / 1024.0,8:F0}K"
                 + $"{result.TotalMs,7:F0}ms{result.StartupMs,7:F0}ms{result.WorkMs,7:F0}ms";

        if (baseline is not null)
        {
            var ratio = baseline.IsMeasurable ? $"{result.WorkMs / baseline.WorkMs:F2}x" : "-";
            line += $"{ratio,10}";
        }

        Console.WriteLine(line);
    }

    public static void WriteFailedRow(string name, int inputBytes)
    {
        Console.WriteLine($"{name,-18}{inputBytes / 1024.0,8:F0}K{"",8}{"failed",9}");
    }

    private void WriteRule()
    {
        Console.WriteLine(new string('-', _header.Length));
    }
}
