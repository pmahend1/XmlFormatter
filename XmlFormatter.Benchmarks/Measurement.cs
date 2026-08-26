namespace XmlFormatter.Benchmarks;

/// <summary>One CLI round trip: how long it took, and how much XML came back.</summary>
internal readonly record struct Measurement(double ElapsedMs, int OutputBytes);
