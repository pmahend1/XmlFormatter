namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// AllowWhiteSpaceUnicodesInAttributeValues decides whether non-ASCII characters in an
/// attribute value are written as hex character references or left as themselves.
///
/// Read the name carefully: true means *escape*, false means leave literal - the opposite of
/// what "allow ... in attribute values" suggests. The name is load-bearing in the public API
/// and is not worth breaking, so these tests pin the behaviour the name does not convey.
///
/// The non-ASCII characters here are written as \u escapes rather than typed. A literal in
/// the file survives until something normalises it, and a normalised character still looks
/// identical while testing something else - which has already happened once in this repo, in
/// the benchmark sample generator.
/// </summary>
public class AllowWhiteSpaceUnicodesInAttributeValuesTests
{
    private const string Accented = "<r a=\"café\"/>";
    private const string Emoji = "<r a=\"hi 😀\"/>";

    [Fact]
    public void True_by_default_escapes_non_ascii_as_a_hex_reference()
    {
        var formatted = TestFormatter.Format(Accented, TestOptions.NoDeclaration);

        Assert.Equal("<r a=\"caf&#xE9;\" />", formatted);
    }

    [Fact]
    public void False_leaves_non_ascii_literal()
    {
        var options = TestOptions.NoDeclaration with { AllowWhiteSpaceUnicodesInAttributeValues = false };

        var formatted = TestFormatter.Format(Accented, options);

        Assert.Equal("<r a=\"café\" />", formatted);
    }

    [Fact]
    public void True_escapes_a_surrogate_pair_as_one_codepoint()
    {
        // The pair has to be recombined before escaping. Escaping each half on its own would
        // emit two references for the lone surrogates, which no parser will read back.
        var formatted = TestFormatter.Format(Emoji, TestOptions.NoDeclaration);

        Assert.Equal("<r a=\"hi &#x1F600;\" />", formatted);
    }

    [Fact]
    public void Ascii_is_untouched_either_way()
    {
        var escaped = TestFormatter.Format("""<r a="plain"/>""", TestOptions.NoDeclaration);
        var literal = TestFormatter.Format("""<r a="plain"/>""",
                                           TestOptions.NoDeclaration with { AllowWhiteSpaceUnicodesInAttributeValues = false });

        Assert.Equal("""<r a="plain" />""", escaped);
        Assert.Equal(escaped, literal);
    }
}
