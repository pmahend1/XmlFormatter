namespace XmlFormatter.Tests.OptionBehavior;

/// <summary>
/// Ignored under UseSingleQuotes by documented contract. Under double quotes the escaper
/// never emits &amp;apos;, so the option has no reachable effect at all.
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
    public void False_should_escape_the_apostrophe_under_double_quotes()
    {
        KnownFailure.Expect("AllowSingleQuoteInAttributeValue = false is inert under double quotes, which "
                          + "is the only configuration where it is not documented as ignored. EscapeXmlValue "
                          + "escapes just the delimiter in use, so it never produces the &apos; that this "
                          + "option's replacement looks for, and the setting has no reachable effect at all.",
                            () =>
                            {
                                var options = TestOptions.NoDeclaration with { AllowSingleQuoteInAttributeValue = false };

                                var formatted = TestFormatter.Format(Apostrophe, options);

                                Assert.Equal("""<r a="it&apos;s" />""", formatted);
                            });
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
