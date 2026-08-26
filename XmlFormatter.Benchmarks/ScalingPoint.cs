namespace XmlFormatter.Benchmarks;

/// <summary>One point on the cost curve: what a document of this size took to format.</summary>
internal readonly record struct ScalingPoint(int InputChars, double Milliseconds);
