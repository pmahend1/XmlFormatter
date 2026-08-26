namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// AllowSingleQuoteInAttributeValue is meant to decide whether an apostrophe inside a value
/// stays literal or is written as &amp;apos;.
///
/// It currently cannot do either. With double quotes the escaper never emits &amp;apos; in
/// the first place, so the replacement that honours this option has nothing to act on; with
/// single quotes Format forces the option off, because a literal apostrophe would close the
/// value. The option therefore has no observable effect in any configuration.
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
        KnownFailure.Expect("AllowSingleQuoteInAttributeValue = false is ignored. EscapeXmlValue only "
                          + "escapes the delimiter in use, so under double quotes it never produces the "
                          + "&apos; that the option's replacement looks for.",
                            () =>
                            {
                                var options = TestOptions.NoDeclaration with { AllowSingleQuoteInAttributeValue = false };

                                var formatted = TestFormatter.Format(Apostrophe, options);

                                Assert.Equal("""<r a="it&apos;s" />""", formatted);
                            });
    }

    [Fact]
    public void Is_overridden_when_single_quotes_are_in_use()
    {
        // Asking for single quotes and literal apostrophes at once would produce invalid XML,
        // so Format resolves the conflict in favour of well-formedness rather than the option.
        var options = TestOptions.NoDeclaration with
        {
            UseSingleQuotes = true,
            AllowSingleQuoteInAttributeValue = true,
        };

        var formatted = TestFormatter.Format(Apostrophe, options);

        Assert.Equal("<r a='it&apos;s' />", formatted);
    }
}
