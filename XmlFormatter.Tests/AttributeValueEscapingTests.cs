namespace XmlFormatter.Tests;

/// <summary>
/// The five characters XML requires escaping in an attribute value, as handled by
/// EscapeXmlValue.
///
/// Three of them - &amp; &lt; &gt; - are unconditional. The two delimiters are not: only the
/// one currently in use is escaped, because the other cannot end the value and reads better
/// left alone. UseSingleQuotesTests covers the choice of delimiter; this covers the escaping
/// that follows from it.
/// </summary>
public class AttributeValueEscapingTests
{
    [Fact]
    public void An_ampersand_is_escaped()
    {
        var formatted = TestFormatter.Format("""<r a="a&amp;b"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="a&amp;b" />""", formatted);
    }

    [Fact]
    public void A_less_than_sign_is_escaped()
    {
        var formatted = TestFormatter.Format("""<r a="a&lt;b"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="a&lt;b" />""", formatted);
    }

    [Fact]
    public void A_greater_than_sign_is_escaped()
    {
        // Not strictly required inside an attribute value, but harmless and consistent.
        var formatted = TestFormatter.Format("""<r a="a&gt;b"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="a&gt;b" />""", formatted);
    }

    [Fact]
    public void A_double_quote_is_escaped_inside_a_double_quoted_value()
    {
        var formatted = TestFormatter.Format("""<r a='say "hi"'/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="say &quot;hi&quot;" />""", formatted);
    }

    [Fact]
    public void A_double_quote_is_left_literal_inside_a_single_quoted_value()
    {
        var formatted = TestFormatter.Format("""<r a='say "hi"'/>""",
                                             TestOptions.NoDeclaration with { UseSingleQuotes = true });

        Assert.Equal("""<r a='say "hi"' />""", formatted);
    }

    [Fact]
    public void All_three_unconditional_escapes_survive_together()
    {
        var formatted = TestFormatter.Format("""<r a="&lt;&amp;&gt;"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="&lt;&amp;&gt;" />""", formatted);
    }

    [Fact]
    public void Escaping_applies_to_every_attribute_on_an_element()
    {
        var options = TestOptions.NoDeclaration with { AttributesInNewlineThreshold = 2 };

        var formatted = TestFormatter.Format("""<r a="x&amp;y" b='q"r'/>""", options);

        Assert.Equal("""<r a="x&amp;y" b="q&quot;r" />""", formatted);
    }
}
