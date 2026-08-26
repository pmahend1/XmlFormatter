namespace XmlFormatter.Benchmarks;

/// <summary>
/// What one sample cost: the whole round trip, the startup that was subtracted from it,
/// and the formatting work left over. Serialised as-is into the baseline JSON.
/// </summary>
internal sealed record SampleResult(string Sample,
                                    int InputBytes,
                                    int OutputBytes,
                                    double TotalMs,
                                    double StartupMs,
                                    double WorkMs)
{
    /// <summary>
    /// Work below this is the residue of subtracting a ~135 ms startup from a ~140 ms round
    /// trip: jitter, not signal. Ratios built on it swing wildly and mean nothing, so
    /// samples under it get no ratio and no scaling line.
    /// </summary>
    public const double MeaningfulWorkMs = 20;

    public bool IsMeasurable => WorkMs >= MeaningfulWorkMs;
}
