namespace XmlFormatter.Tests;

/// <summary>
/// Only the delimiter in use is escaped; the other is legal literal and reads better. The
/// apostrophe is the exception - AllowSingleQuoteInAttributeValue can ask for it escaped under
/// double quotes, which AllowSingleQuoteInAttributeValueTests covers.
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
    public void An_apostrophe_is_escaped_inside_a_single_quoted_value()
    {
        var formatted = TestFormatter.Format("""<r a="it's"/>""",
                                             TestOptions.NoDeclaration with { UseSingleQuotes = true });

        Assert.Equal("<r a='it&apos;s' />", formatted);
    }

    [Fact]
    public void An_apostrophe_is_left_literal_inside_a_double_quoted_value()
    {
        // The fifth character, and the only one an option speaks for:
        // AllowSingleQuoteInAttributeValue = false escapes it here instead.
        var formatted = TestFormatter.Format("""<r a="it's"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="it's" />""", formatted);
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
