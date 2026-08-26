namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// UseSingleQuotes picks the delimiter for attribute values. Whichever delimiter is in use
/// is the one escaped inside the value; the other is left literal, which is legal XML and
/// keeps the output readable.
/// </summary>
public class UseSingleQuotesTests
{
    [Fact]
    public void False_by_default_delimits_with_double_quotes()
    {
        var formatted = TestFormatter.Format("""<r a="v"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="v" />""", formatted);
    }

    [Fact]
    public void True_delimits_with_single_quotes()
    {
        var formatted = TestFormatter.Format("""<r a="v"/>""", TestOptions.NoDeclaration with { UseSingleQuotes = true });

        Assert.Equal("<r a='v' />", formatted);
    }

    [Fact]
    public void True_escapes_an_apostrophe_inside_the_value()
    {
        var formatted = TestFormatter.Format("""<r a="it's"/>""", TestOptions.NoDeclaration with { UseSingleQuotes = true });

        Assert.Equal("<r a='it&apos;s' />", formatted);
    }

    [Fact]
    public void True_leaves_a_double_quote_inside_the_value_literal()
    {
        var formatted = TestFormatter.Format("""<r a='say "hi"'/>""", TestOptions.NoDeclaration with { UseSingleQuotes = true });

        Assert.Equal("""<r a='say "hi"' />""", formatted);
    }

    [Fact]
    public void Does_not_mutate_the_options_the_caller_passed_in()
    {
        /*
         * Format turns AllowSingleQuoteInAttributeValue off when UseSingleQuotes is on. Options
         * is a mutable record struct, so that assignment lands on the formatter's copy - but
         * only because it is a struct. If it ever becomes a class the caller's instance would
         * start changing under them, and nothing else in the suite would notice.
         */
        var options = TestOptions.NoDeclaration with { UseSingleQuotes = true };

        TestFormatter.Format("""<r a="v"/>""", options);

        Assert.True(options.AllowSingleQuoteInAttributeValue);
    }
}
