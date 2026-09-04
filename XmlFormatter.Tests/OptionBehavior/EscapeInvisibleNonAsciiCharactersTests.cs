using System.Xml;

namespace XmlFormatter.Tests.OptionBehavior;

/// <summary>
/// EscapeInvisibleNonAsciiCharacters decides whether the non-ASCII characters that draw nothing -
/// NBSP, the zero-width spaces, the bidi and other format controls - are written as hex character
/// references or as themselves. It is off by default, which is the v2.3.1 output.
///
/// It exists because #208 and #216 wanted opposite things and only one of them was about
/// visibility: #208 lost an NBSP that looked exactly like a space, and #216 got umlauts and CJK
/// spelled out as entity soup by the blanket escaping that answered #208. Escaping only what
/// draws nothing settles both, so the visible half of the corpus is covered here too - the
/// assertions that an umlaut, an ideograph and an emoji survive with the option <b>on</b> are the
/// point of the option, not incidental.
///
/// Inputs are written as character references so the source XML says exactly which codepoint it
/// means, and expectations as \u escapes so normalizing the file cannot silently change what is
/// under test. Two tests below cannot do either, and say why.
/// </summary>
public class EscapeInvisibleNonAsciiCharactersTests
{
    private static Options Escaping => TestOptions.NoDeclaration with { EscapeInvisibleNonAsciiCharacters = true };

    [Fact]
    public void Off_by_default_a_nbsp_in_text_stays_literal()
    {
        var formatted = TestFormatter.Format("<r>a&#xA0;b</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>a\u00A0b</r>", formatted);
    }

    [Fact]
    public void Off_by_default_a_nbsp_in_an_attribute_stays_literal()
    {
        var formatted = TestFormatter.Format("""<r a="x&#xA0;y"/>""", TestOptions.NoDeclaration);

        Assert.Equal("<r a=\"x\u00A0y\" />", formatted);
    }

    [Fact]
    public void On_a_nbsp_in_text_becomes_a_character_reference()
    {
        var formatted = TestFormatter.Format("<r>a&#xA0;b</r>", Escaping);

        Assert.Equal("<r>a&#xA0;b</r>", formatted);
    }

    [Fact]
    public void On_a_nbsp_in_an_attribute_becomes_a_character_reference()
    {
        var formatted = TestFormatter.Format("""<r a="x&#xA0;y"/>""", Escaping);

        Assert.Equal("""<r a="x&#xA0;y" />""", formatted);
    }

    [Fact]
    public void On_a_nbsp_is_escaped_at_any_depth()
    {
        var formatted = TestFormatter.Format("<r><a>x&#xA0;y</a></r>", Escaping);

        Assert.Equal("<r>\n    <a>x&#xA0;y</a>\n</r>", formatted);
    }

    /*
     * The #216 half. These are what separates this option from the blanket escaping of v2.3.0:
     * every one of them is non-ASCII, and every one of them draws something.
     */

    [Fact]
    public void On_an_umlaut_stays_literal()
    {
        var formatted = TestFormatter.Format("<xsl:text xmlns:xsl=\"x\">&#xFC;</xsl:text>", Escaping);

        Assert.Equal("<xsl:text xmlns:xsl=\"x\">\u00FC</xsl:text>", formatted);
    }

    [Fact]
    public void On_a_cjk_ideograph_stays_literal()
    {
        var formatted = TestFormatter.Format("<r>&#x65E5;&#x672C;</r>", Escaping);

        Assert.Equal("<r>\u65E5\u672C</r>", formatted);
    }

    [Fact]
    public void On_an_emoji_stays_literal()
    {
        // A visible surrogate pair: both halves are copied, neither is spelled out.
        var formatted = TestFormatter.Format("<r>hi &#x1F600;</r>", Escaping);

        Assert.Equal("<r>hi \uD83D\uDE00</r>", formatted);
    }

    [Fact]
    public void On_a_combining_mark_stays_literal()
    {
        // U+0301 draws the accent, so the letter under it would be wrong without it.
        var formatted = TestFormatter.Format("<r>cafe&#x301;</r>", Escaping);

        Assert.Equal("<r>cafe\u0301</r>", formatted);
    }

    [Fact]
    public void On_ascii_text_is_returned_unchanged()
    {
        var formatted = TestFormatter.Format("<r>plain</r>", Escaping);

        Assert.Equal("<r>plain</r>", formatted);
    }

    /*
     * The rest of what "invisible" covers. Unicode's own categories draw the line - the
     * separators, the format characters and the C1 controls - rather than a list of the
     * codepoints someone has complained about so far.
     */

    [Theory]
    [InlineData("200B", "zero width space")]
    [InlineData("200C", "zero width non-joiner")]
    [InlineData("200D", "zero width joiner")]
    [InlineData("2060", "word joiner")]
    [InlineData("FEFF", "zero width no-break space")]
    [InlineData("200F", "right-to-left mark")]
    [InlineData("202E", "right-to-left override")]
    public void On_a_format_character_becomes_a_character_reference(string hex, string name)
    {
        var formatted = TestFormatter.Format($"<r>a&#x{hex};b</r>", Escaping);

        Assert.Equal($"<r>a&#x{hex};b</r>", formatted);
        Assert.DoesNotContain((char)Convert.ToInt32(hex, 16), formatted);
        Assert.NotEmpty(name);
    }

    [Fact]
    public void On_a_soft_hyphen_becomes_a_character_reference()
    {
        // Kept out of the theory above: its reference is written without the leading zero.
        var formatted = TestFormatter.Format("<r>a&#xAD;b</r>", Escaping);

        Assert.Equal("<r>a&#xAD;b</r>", formatted);
    }

    [Theory]
    [InlineData("2003", "em space")]
    [InlineData("2009", "thin space")]
    [InlineData("202F", "narrow no-break space")]
    [InlineData("205F", "medium mathematical space")]
    [InlineData("3000", "ideographic space")]
    public void On_a_unicode_space_becomes_a_character_reference(string hex, string name)
    {
        var formatted = TestFormatter.Format($"<r>a&#x{hex};b</r>", Escaping);

        Assert.Equal($"<r>a&#x{hex};b</r>", formatted);
        Assert.NotEmpty(name);
    }

    [Fact]
    public void On_a_c1_control_becomes_a_character_reference()
    {
        // Legal in XML 1.0 content and impossible to see. U+0085 stays a character here rather
        // than folding into a line break - that is XML 1.1's rule, not 1.0's.
        var formatted = TestFormatter.Format("<r>a&#x85;b</r>", Escaping);

        Assert.Equal("<r>a&#x85;b</r>", formatted);
    }

    [Fact]
    public void On_an_invisible_character_outside_the_basic_plane_becomes_one_reference()
    {
        /*
         * U+E0020, a tag character: invisible, and two chars in UTF-16. Escaping the surrogate
         * halves separately would emit &#xDB40;&#xDC20;, which no parser reads back as anything -
         * lone surrogates are not characters. This is what the pair-aware read is for.
         */
        var formatted = TestFormatter.Format("<r>a&#xE0020;b</r>", Escaping);

        Assert.Equal("<r>a&#xE0020;b</r>", formatted);
    }

    /*
     * Where a character reference is not a character reference. Neither CDATA nor a comment
     * resolves one, so escaping inside either would replace the character with the six literal
     * characters that spell its name. These are the two tests that cannot write their input as a
     * reference, for the very reason they exist: inside CDATA, &#xA0; is not an NBSP.
     */

    [Fact]
    public void On_cdata_content_is_left_literal()
    {
        var formatted = TestFormatter.Format("<r><![CDATA[a\u00A0b]]></r>", Escaping);

        Assert.Equal("<r><![CDATA[a\u00A0b]]></r>", formatted);
    }

    [Fact]
    public void On_comment_text_is_left_literal()
    {
        var formatted = TestFormatter.Format("<r><!--a\u00A0b--></r>", Escaping);

        Assert.Equal("<r>\n    <!-- a\u00A0b -->\n</r>", formatted);
    }

    [Fact]
    public void On_a_reference_typed_inside_cdata_stays_six_characters_of_text()
    {
        // The flip side of the pair above: this input holds no NBSP at all, so there is nothing
        // for the option to act on and six characters to leave alone.
        var formatted = TestFormatter.Format("<r><![CDATA[a&#xA0;b]]></r>", Escaping);

        Assert.Equal("<r><![CDATA[a&#xA0;b]]></r>", formatted);
    }

    /*
     * The relationship with AllowWhiteSpaceUnicodesInAttributeValues, which is the one part of
     * this option that reads as a contradiction. Tab, newline and carriage return are invisible
     * too, and this option does not touch them: they are ASCII, they belong to that option alone,
     * and it is about surviving attribute-value normalization rather than about visibility. So
     * "escape invisible characters" on with "escape whitespace" off really does leave a literal
     * tab next to an escaped NBSP, and that is the specification rather than a gap in it.
     */

    [Theory]
    [InlineData(true, true, "<r a=\"tab&#x9;gap&#xA0;end\" />")]
    [InlineData(true, false, "<r a=\"tab&#x9;gap\u00A0end\" />")]
    [InlineData(false, true, "<r a=\"tab\tgap&#xA0;end\" />")]
    [InlineData(false, false, "<r a=\"tab\tgap\u00A0end\" />")]
    public void The_two_escaping_options_decide_different_characters(bool escapeWhitespace,
                                                                     bool escapeInvisibleNonAscii,
                                                                     string expected)
    {
        var options = TestOptions.NoDeclaration with
        {
            AllowWhiteSpaceUnicodesInAttributeValues = escapeWhitespace,
            EscapeInvisibleNonAsciiCharacters = escapeInvisibleNonAscii,
        };

        var formatted = TestFormatter.Format("""<r a="tab&#x9;gap&#xA0;end"/>""", options);

        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void On_a_tab_in_text_is_left_alone()
    {
        // The other half of the same boundary: nothing about this option reaches ASCII, so text
        // line endings and indentation are not its business at any setting.
        var formatted = TestFormatter.Format("<r>a&#x9;b</r>", Escaping);

        Assert.Equal("<r>a\tb</r>", formatted);
    }

    /*
     * The sharpest version of what the option is for. Multi-line text is reflowed a line at a
     * time and each line is trimmed - and Trim() counts NBSP as whitespace, so with the option
     * off one at the edge of a line is not merely indistinguishable from a space, it is deleted.
     * Escaped, it starts with an ampersand and there is nothing there to trim.
     */

    [Fact]
    public void Off_a_nbsp_at_the_edge_of_a_reflowed_line_is_lost()
    {
        var formatted = TestFormatter.Format("<r>\n  &#xA0;first\n  second&#xA0;\n</r>",
                                             TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    first\n    second\n</r>", formatted);
    }

    [Fact]
    public void On_a_nbsp_at_the_edge_of_a_reflowed_line_survives()
    {
        var formatted = TestFormatter.Format("<r>\n  &#xA0;first\n  second&#xA0;\n</r>", Escaping);

        Assert.Equal("<r>\n    &#xA0;first\n    second&#xA0;\n</r>", formatted);
        Assert.Equal(formatted, TestFormatter.Format(formatted, Escaping));
    }

    [Fact]
    public void On_the_output_reads_back_as_the_same_characters()
    {
        // What makes the escaping lossless rather than merely different.
        const string input = "<r a=\"x&#xA0;y\">caf&#xE9;&#x200B;&#x3000;&#x1F600;</r>";

        var formatted = TestFormatter.Format(input, Escaping);

        var original = new XmlDocument();
        original.LoadXml(input);
        var reparsed = new XmlDocument();
        reparsed.LoadXml(formatted);

        Assert.Equal(original.DocumentElement!.InnerText, reparsed.DocumentElement!.InnerText);
        Assert.Equal(original.DocumentElement.GetAttribute("a"), reparsed.DocumentElement.GetAttribute("a"));
    }

    [Fact]
    public void On_formatting_twice_changes_nothing()
    {
        // A reference resolves back to its character on load, so pass two escapes the same
        // character again rather than escaping the ampersand of pass one's output.
        var once = TestFormatter.Format("<r a=\"x&#xA0;y\">a&#xA0;b&#x200B;c</r>", Escaping);

        Assert.Equal(once, TestFormatter.Format(once, Escaping));
    }
}
