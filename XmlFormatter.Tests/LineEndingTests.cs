using XmlFormatter;

namespace XmlFormatter.Tests;

/// <summary>
/// The formatter emits Environment.NewLine, so its output is one of the few things about it
/// that genuinely differs between platforms. Everywhere else in the suite that difference is
/// normalized away - TestFormatter rewrites CRLF to LF, and FixtureFormattingTests compares
/// both sides as LF - which keeps those tests readable but leaves the line-ending behavior
/// itself asserted nowhere.
///
/// This is where it is asserted. These are the tests that make running CI on both Windows and
/// Linux worth the second runner: on Linux they confirm LF output, on Windows CRLF, from the
/// same source.
/// </summary>
public class LineEndingTests
{
    [Fact]
    public void Output_uses_the_platform_newline()
    {
        var formatted = new Formatter().Format("<r><a /></r>", TestOptions.NoDeclaration);

        Assert.Contains(Environment.NewLine, formatted);
    }

    [Fact]
    public void Output_contains_no_foreign_line_endings()
    {
        // On Windows a bare LF would mean some path concatenated "\n" instead of the platform
        // newline; on Linux a CR would mean the reverse. One assertion, both failures.
        var formatted = new Formatter().Format("<r><a /><b /></r>", TestOptions.NoDeclaration);

        var normalized = formatted.Replace(Environment.NewLine, "");

        Assert.DoesNotContain('\r', normalized);
        Assert.DoesNotContain('\n', normalized);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Input_line_endings_do_not_change_the_output(string inputNewLine)
    {
        // XML parsers normalize CRLF to LF while reading (XML 1.0 section 2.11), so a document
        // authored on Windows and the same document authored on Linux must format identically.
        // Without this, a repo without .gitattributes could format differently per contributor.
        var xml = string.Join(inputNewLine, "<root>", "    <a>text</a>", "    <b />", "</root>");

        var formatted = new Formatter().Format(xml, TestOptions.NoDeclaration);

        Assert.Equal("<root>" + Environment.NewLine +
                     "    <a>text</a>" + Environment.NewLine +
                     "    <b />" + Environment.NewLine +
                     "</root>",
                     formatted);
    }

    [Fact]
    public void Crlf_and_lf_input_produce_byte_identical_output()
    {
        var formatter = new Formatter();

        var fromLf = formatter.Format("<root>\n    <a>text</a>\n</root>", TestOptions.NoDeclaration);
        var fromCrlf = formatter.Format("<root>\r\n    <a>text</a>\r\n</root>", TestOptions.NoDeclaration);

        Assert.Equal(fromLf, fromCrlf);
    }
}
