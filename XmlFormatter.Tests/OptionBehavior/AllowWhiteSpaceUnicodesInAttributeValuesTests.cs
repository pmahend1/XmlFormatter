namespace XmlFormatter.Tests.OptionBehavior;

/// <summary>
/// AllowWhiteSpaceUnicodesInAttributeValues decides whether tab, newline and carriage return
/// inside an attribute value are written as hex character references or left as themselves.
///
/// It exists because of XML attribute-value normalization (XML 1.0 section 3.3.3): a parser
/// replaces each literal tab, newline or CR in an attribute value with a space when reading
/// the document back. Written literally they are silently lost, so escaping them is what makes
/// the round-trip lossless - which is why the option defaults to true.
///
/// It also keeps the greater-than sign literal when true, for SAPUI5 bindings (PrettyXML #131).
///
/// Printable non-ASCII has no such problem and is never escaped by this option. It once was,
/// which is what broke umlauts in #216; TextContentEncodingTests carries the rest of that
/// regression cover.
///
/// Non-printable non-ASCII - NBSP and the zero-width characters - is not this option's either,
/// even though it is invisible in exactly the way tab and newline are. That belongs to
/// EscapeInvisibleNonAsciiCharacters, which starts above ASCII where this one stops. The two
/// look like they overlap and do not; EscapeInvisibleNonAsciiCharactersTests runs all four
/// combinations of them over one attribute holding both a tab and an NBSP.
///
/// The whitespace inputs here are written as character references so the source XML says
/// exactly which codepoint it means, and the non-ASCII ones as \u escapes so normalizing the
/// file cannot silently change what is under test.
/// </summary>
public class AllowWhiteSpaceUnicodesInAttributeValuesTests
{
    private const string Accented = "<r a=\"caf\u00E9\"/>";
    private const string Emoji = "<r a=\"hi \uD83D\uDE00\"/>";

    [Fact]
    public void True_by_default_escapes_a_newline()
    {
        var formatted = TestFormatter.Format("""<r a="line1&#xA;line2"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="line1&#xA;line2" />""", formatted);
    }

    [Fact]
    public void True_by_default_escapes_a_tab()
    {
        var formatted = TestFormatter.Format("""<r a="col1&#x9;col2"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="col1&#x9;col2" />""", formatted);
    }

    [Fact]
    public void True_by_default_escapes_a_carriage_return()
    {
        var formatted = TestFormatter.Format("""<r a="line1&#xD;line2"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="line1&#xD;line2" />""", formatted);
    }

    [Fact]
    public void False_leaves_whitespace_literal()
    {
        // Well-formed, but a parser reading this back sees a space where the tab was.
        var options = TestOptions.NoDeclaration with { AllowWhiteSpaceUnicodesInAttributeValues = false };

        var formatted = TestFormatter.Format("""<r a="col1&#x9;col2"/>""", options);

        Assert.Equal("<r a=\"col1\tcol2\" />", formatted);
    }

    [Fact]
    public void True_by_default_leaves_a_greater_than_sign_literal()
    {
        var formatted = TestFormatter.Format("""<r a="a&gt;b"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="a>b" />""", formatted);
    }

    [Fact]
    public void False_escapes_a_greater_than_sign()
    {
        var options = TestOptions.NoDeclaration with { AllowWhiteSpaceUnicodesInAttributeValues = false };

        var formatted = TestFormatter.Format("""<r a="a>b"/>""", options);

        Assert.Equal("""<r a="a&gt;b" />""", formatted);
    }

    [Fact]
    public void True_by_default_leaves_sapui5_expression_bindings_as_written()
    {
        const string view = """
                            <View>
                                <Content>
                                    <Label text="{i18n>LabelText}" />
                                    <Input id="Input1"
                                           value="{service>description}" />
                                </Content>
                            </View>
                            """;

        var formatted = TestFormatter.Format(view, TestOptions.NoDeclaration);

        Assert.Equal(view, formatted);
        Assert.Equal(formatted, TestFormatter.Format(formatted, TestOptions.NoDeclaration));
    }

    [Fact]
    public void Non_ascii_is_left_literal_either_way()
    {
        var options = TestOptions.NoDeclaration with { AllowWhiteSpaceUnicodesInAttributeValues = false };

        var escaped = TestFormatter.Format(Accented, TestOptions.NoDeclaration);
        var literal = TestFormatter.Format(Accented, options);

        Assert.Equal("<r a=\"caf\u00E9\" />", escaped);
        Assert.Equal(escaped, literal);
    }

    [Fact]
    public void A_surrogate_pair_is_left_literal_either_way()
    {
        // Nothing splits the pair, so nothing has to put it back together.
        var options = TestOptions.NoDeclaration with { AllowWhiteSpaceUnicodesInAttributeValues = false };

        var escaped = TestFormatter.Format(Emoji, TestOptions.NoDeclaration);
        var literal = TestFormatter.Format(Emoji, options);

        Assert.Equal("<r a=\"hi \uD83D\uDE00\" />", escaped);
        Assert.Equal(escaped, literal);
    }

    [Fact]
    public void Ascii_is_untouched_either_way()
    {
        var options = TestOptions.NoDeclaration with { AllowWhiteSpaceUnicodesInAttributeValues = false };

        var escaped = TestFormatter.Format("""<r a="plain"/>""", TestOptions.NoDeclaration);
        var literal = TestFormatter.Format("""<r a="plain"/>""", options);

        Assert.Equal("""<r a="plain" />""", escaped);
        Assert.Equal(escaped, literal);
    }
}
