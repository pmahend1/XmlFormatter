namespace XmlFormatter.Tests;

/// <summary>
/// Formats a document for the per-option tests and normalises the result to LF.
///
/// The formatter emits <see cref="Environment.NewLine"/>, so an expected value written as a
/// raw string literal in these files would match on macOS and Linux and fail on Windows for
/// a reason that has nothing to do with the option under test. Normalising here lets every
/// expectation be written the way it reads.
/// </summary>
internal static class TestFormatter
{
    public static string Format(string xml, Options options) => new Formatter().Format(xml, options).Replace("\r\n", "\n");
}
