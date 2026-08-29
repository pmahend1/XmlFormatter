namespace XmlFormatter.Tests;

internal static class TestFormatter
{
    // LF, so expected values can be written as raw string literals and still pass on Windows.
    public static string Format(string xml, Options options) => new Formatter().Format(xml, options).Replace("\r\n", "\n");
}
