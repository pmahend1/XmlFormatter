namespace XmlFormatter.Tests.OptionBehavior;

/// <summary>
/// Ignored under UseSingleQuotes by documented contract. Under double quotes, where an
/// apostrophe is legal either way, it decides whether one is written literally or as &amp;apos;.
/// </summary>
public class AllowSingleQuoteInAttributeValueTests
{
    private const string Apostrophe = """<r a="it's"/>""";

    [Fact]
    public void True_keeps_the_apostrophe_literal_under_double_quotes()
    {
        var formatted = TestFormatter.Format(Apostrophe, TestOptions.NoDeclaration);

        Assert.Equal("""<r a="it's" />""", formatted);
    }

    [Fact]
    public void False_escapes_the_apostrophe_under_double_quotes()
    {
        var options = TestOptions.NoDeclaration with { AllowSingleQuoteInAttributeValue = false };

        var formatted = TestFormatter.Format(Apostrophe, options);

        Assert.Equal("""<r a="it&apos;s" />""", formatted);
    }

    [Fact]
    public void False_escapes_every_apostrophe_in_the_value()
    {
        // The example the extension documents for this setting, unchecked.
        var options = TestOptions.NoDeclaration with { AllowSingleQuoteInAttributeValue = false };

        var formatted = TestFormatter.Format("""<r a="Value'has'apostrophes"/>""", options);

        Assert.Equal("""<r a="Value&apos;has&apos;apostrophes" />""", formatted);
    }

    [Fact]
    public void False_leaves_the_double_quote_escape_alone()
    {
        var options = TestOptions.NoDeclaration with { AllowSingleQuoteInAttributeValue = false };

        var formatted = TestFormatter.Format("""<r a='say "hi" to it&apos;s'/>""", options);

        Assert.Equal("""<r a="say &quot;hi&quot; to it&apos;s" />""", formatted);
    }

    [Fact]
    public void Is_ignored_when_single_quotes_are_in_use()
    {
        // Documented: "Ignored if Use Single Quotes is Checked". Asking for single quotes and
        // literal apostrophes at once would produce invalid XML, so well-formedness wins.
        var options = TestOptions.NoDeclaration with
        {
            UseSingleQuotes = true,
            AllowSingleQuoteInAttributeValue = true,
        };

        var formatted = TestFormatter.Format(Apostrophe, options);

        Assert.Equal("<r a='it&apos;s' />", formatted);
    }
}
