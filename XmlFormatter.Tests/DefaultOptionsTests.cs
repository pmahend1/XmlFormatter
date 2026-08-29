namespace XmlFormatter.Tests;

/// <summary>
/// default(Options) skips the property initializers, so every reference type on the struct is
/// null - including the wildcard pattern list, which the formatter reads on every element.
///
/// A caller reaches this without doing anything exotic: default(Options), a field left
/// unassigned, or an Options deserialized from JSON that omits the property. Options.Equals
/// and GetHashCode already guard it; PrintNode did not, and threw ArgumentNullException out of
/// Any() before emitting anything.
/// </summary>
public class DefaultOptionsTests
{
    [Fact]
    public void Formatting_with_default_options_does_not_throw()
    {
        var formatted = new Formatter().Format("<r><a /></r>", default(Options));

        Assert.NotEmpty(formatted);
    }

    [Fact]
    public void Formatting_with_default_options_still_formats()
    {
        // default(Options) means IndentLength 0 and no self-closing tags, not "library defaults".
        // Pinned so the null guard cannot quietly become a substitution of new Options().
        var formatted = new Formatter().Format("<r><a /></r>", default(Options)).Replace("\r\n", "\n");

        Assert.Equal("<r>\n<a></a>\n</r>", formatted);
    }

    [Fact]
    public void A_null_pattern_list_is_treated_as_no_exceptions()
    {
        var options = new Options
        {
            PositionAllAttributesOnFirstLine = true,
            WildCardedExceptionsForPositionAllAttributesOnFirstLine = null!,
            AddXmlDeclarationIfMissing = false
        };

        var formatted = TestFormatter.Format("""<r a="1" b="2" />""", options);

        Assert.Equal("""<r a="1" b="2" />""", formatted);
    }
}
