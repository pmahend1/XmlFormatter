namespace XmlFormatter.Benchmarks;

/// <summary>One scenario the guard measures at two sizes.</summary>
/// <param name="KnownFailing">
/// A case that is superlinear today for a reason already known and tracked. It is still
/// measured and still printed, but it does not fail the run - otherwise the guard is red
/// from the day it lands and stops meaning anything. If one of these starts passing the
/// run says so, so the flag gets removed rather than quietly outliving the bug.
/// </param>
internal sealed record ScalingCase(string Name,
                                   string OptionSet,
                                   bool PreFormatted,
                                   string? KnownFailing = null);
