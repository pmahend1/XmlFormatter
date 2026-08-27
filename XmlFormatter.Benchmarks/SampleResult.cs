namespace XmlFormatter.Benchmarks;

internal sealed record SampleResult(string Sample,
                                    int InputBytes,
                                    int OutputBytes,
                                    double TotalMs,
                                    double StartupMs,
                                    double WorkMs)
{
    // Below this, work is the residue of subtracting a ~135 ms startup from a ~140 ms round
    // trip - jitter, not signal.
    private const double MeaningfulWorkMs = 20;

    public bool IsMeasurable => WorkMs >= MeaningfulWorkMs;
}
