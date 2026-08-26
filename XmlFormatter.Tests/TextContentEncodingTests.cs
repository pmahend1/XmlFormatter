namespace XmlFormatter.Tests;

/// <summary>
/// Non-ASCII handling in *text content*, which goes through EncodeNonAscii rather than the
/// EscapeXmlValue path that attribute values take.
///
/// The two are separate implementations of the same idea, reached by different inputs, so
/// covering one says nothing about the other - before these tests the whole non-ASCII half of
/// EncodeNonAscii was unreached while the attribute tests made unicode look covered.
///
/// Every non-ASCII character here is written as a \u escape rather than typed. A literal
/// survives until something normalises the file, and a normalised character still looks
/// identical while testing something else - which is exactly what the decomposed case below
/// would stop testing.
/// </summary>
public class TextContentEncodingTests
{
    [Fact]
    public void Non_ascii_text_is_written_as_a_hex_reference()
    {
        var formatted = TestFormatter.Format("<r>caf\u00E9</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>caf&#xE9;</r>", formatted);
    }

    [Fact]
    public void A_surrogate_pair_becomes_one_reference()
    {
        // Escaping the halves separately would emit two references for lone surrogates, which
        // no parser reads back. The pair has to be recombined into its codepoint first.
        var formatted = TestFormatter.Format("<r>hi \uD83D\uDE00</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>hi &#x1F600;</r>", formatted);
    }

    [Fact]
    public void A_combining_mark_is_encoded_separately_from_its_base_letter()
    {
        // "e" + U+0301, not the precomposed U+00E9. Two codepoints in, two references out -
        // the encoder must not compose them on the way.
        var formatted = TestFormatter.Format("<r>cafe\u0301</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>cafe&#x301;</r>", formatted);
    }

    [Fact]
    public void Ascii_text_is_returned_unchanged()
    {
        var formatted = TestFormatter.Format("<r>plain</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>plain</r>", formatted);
    }

    [Fact]
    public void Existing_entities_in_text_are_left_alone()
    {
        // EncodeNonAscii runs over already-escaped OuterXml, so it must not re-escape the
        // ampersand of an entity that is already there.
        var formatted = TestFormatter.Format("<r>a &lt; b &amp; c</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>a &lt; b &amp; c</r>", formatted);
    }

    [Fact]
    public void Non_ascii_is_encoded_at_any_depth()
    {
        var formatted = TestFormatter.Format("<r><a>caf\u00E9</a></r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <a>caf&#xE9;</a>\n</r>", formatted);
    }

    [Fact]
    public void Cdata_content_is_left_literal()
    {
        // CDATA is written straight from node.Value and never reaches the encoder. A hex
        // reference inside CDATA would be content rather than an escape, so this is correct.
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
