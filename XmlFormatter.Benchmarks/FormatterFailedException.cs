namespace XmlFormatter.Benchmarks;

/// <summary>The CLI returned non-zero for a sample - bad input, or a formatter crash.</summary>
internal sealed class FormatterFailedException(int exitCode, string stderr) :
                      Exception($"CLI exited {exitCode}{(stderr.Length is not 0 ? $": {stderr}" : string.Empty)}");
