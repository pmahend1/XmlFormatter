namespace XmlFormatter.Tests;

/// <summary>
/// Text content is written straight from OuterXml, which the DOM has already escaped
/// correctly. Under default options nothing re-encodes it afterward.
///
/// Regression cover for #216: a pass that rewrote every non-ASCII codepoint in text as a hex
/// character reference turned every umlaut in an XSLT stylesheet into &amp;#xFC;. The output
/// parsed back to the same document, so it was well-formed - and unreadable.
///
/// One option does re-encode text, and only the part of it that draws nothing:
/// EscapeInvisibleNonAsciiCharacters, off by default and covered in OptionBehavior. Nothing here
/// is invisible, so every one of these documents comes out the same at either setting - which is
/// the option's whole claim, and why that class re-asserts the umlaut and the emoji with it on.
///
/// The non-ASCII characters here are written as \u escapes rather than typed, so normalizing
/// the file cannot silently change what is under test.
/// </summary>
public class TextContentEncodingTests
{
    [Fact]
    public void Non_ascii_text_is_left_literal()
    {
        var formatted = TestFormatter.Format("<r>caf\u00E9</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>caf\u00E9</r>", formatted);
    }

    [Fact]
    public void An_umlaut_survives_in_an_xsl_text_element()
    {
        // The reported case, verbatim.
        var formatted = TestFormatter.Format("<xsl:text xmlns:xsl=\"x\">\u00FC</xsl:text>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<xsl:text xmlns:xsl=\"x\">\u00FC</xsl:text>", formatted);
    }

    [Fact]
    public void A_surrogate_pair_is_left_literal()
    {
        // Emitted as itself, so there are no surrogate halves to recombine.
        var formatted = TestFormatter.Format("<r>hi \uD83D\uDE00</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>hi \uD83D\uDE00</r>", formatted);
    }

    [Fact]
    public void A_combining_mark_stays_attached_to_its_base_letter()
    {
        // "e" + U+0301, not precomposed U+00E9: both codepoints pass through untouched.
        var formatted = TestFormatter.Format("<r>cafe\u0301</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>cafe\u0301</r>", formatted);
    }

    [Fact]
    public void Ascii_text_is_returned_unchanged()
    {
        var formatted = TestFormatter.Format("<r>plain</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>plain</r>", formatted);
    }

    [Fact]
    public void The_five_xml_characters_stay_escaped_in_text()
    {
        // What OuterXml already guarantees, and the reason text needs no escaper of its own.
        var formatted = TestFormatter.Format("<r>a &lt; b &amp; c</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>a &lt; b &amp; c</r>", formatted);
    }

    [Fact]
    public void Non_ascii_is_left_literal_at_any_depth()
    {
        var formatted = TestFormatter.Format("<r><a>caf\u00E9</a></r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <a>caf\u00E9</a>\n</r>", formatted);
    }

    [Fact]
    public void Cdata_content_is_left_literal()
    {
        var formatted = TestFormatter.Format("<r><![CDATA[caf\u00E9]]></r>", TestOptions.NoDeclaration);

        Assert.Equal("<r><![CDATA[caf\u00E9]]></r>", formatted);
    }

    [Fact]
    public void Comment_text_is_left_literal()
    {
        var formatted = TestFormatter.Format("<r><!--caf\u00E9--></r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <!-- caf\u00E9 -->\n</r>", formatted);
    }
}
