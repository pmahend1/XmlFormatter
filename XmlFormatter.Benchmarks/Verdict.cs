namespace XmlFormatter.Benchmarks;

/// <summary>What the two measurements said about one case, and how to read it.</summary>
internal readonly record struct Verdict(ScalingCase Scenario,
                                        double SizeFactor,
                                        double WorkFactor,
                                        double Tolerance)
{
    /// <summary>Work growth as a multiple of size growth. 1.0 is exactly linear.</summary>
    public double Steepness => WorkFactor / SizeFactor;

    private bool IsLinear => Steepness <= Tolerance;

    public bool IsRegression => IsLinear is false && Scenario.KnownFailing is null;

    public bool IsKnownFailure => IsLinear is false && Scenario.KnownFailing is not null;

    public bool IsUnexpectedlyFixed => IsLinear && Scenario.KnownFailing is not null;

    public string Label => (IsLinear, Scenario.KnownFailing) switch
    {
        (true, null) => "ok",
        (true, _) => "FIXED",
        (false, null) => "FAIL",
        (false, _) => "known",
    };
}
