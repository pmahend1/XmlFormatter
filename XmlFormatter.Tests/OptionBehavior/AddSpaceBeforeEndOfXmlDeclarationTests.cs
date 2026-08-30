namespace XmlFormatter.Tests.OptionBehavior;

public class AddSpaceBeforeEndOfXmlDeclarationTests
{
    private const string WithDeclaration = """<?xml version="1.0" encoding="utf-8"?><r/>""";

    [Fact]
    public void False_by_default_keeps_the_declaration_as_written()
    {
        var formatted = TestFormatter.Format(WithDeclaration, new Options());

        Assert.Equal("""
            <?xml version="1.0" encoding="utf-8"?>
            <r />
            """, formatted);
    }

    [Fact]
    public void True_separates_the_question_mark()
    {
        var formatted = TestFormatter.Format(WithDeclaration, new Options { AddSpaceBeforeEndOfXmlDeclaration = true });

        Assert.Equal("""
            <?xml version="1.0" encoding="utf-8" ?>
            <r />
            """, formatted);
    }

    /*
     * The generated declaration used to carry the space whatever this option said, which made
     * Format(Format(x)) differ from Format(x) on any document without a declaration: the second
     * pass sees a real declaration and renders it through the option, dropping the space again.
     * The space cannot be preserved instead - XmlDeclaration.OuterXml always renders "?>" - so
     * governing both is the only version of this option that survives a round-trip.
     */
    [Fact]
    public void False_generates_the_declaration_without_the_space()
    {
        var formatted = TestFormatter.Format("<r/>", new Options { AddSpaceBeforeEndOfXmlDeclaration = false });

        Assert.Equal("""
            <?xml version="1.0" encoding="UTF-8"?>
            <r />
            """, formatted);
    }

    [Fact]
    public void True_generates_the_declaration_with_the_space()
    {
        var formatted = TestFormatter.Format("<r/>", new Options { AddSpaceBeforeEndOfXmlDeclaration = true });

        Assert.Equal("""
            <?xml version="1.0" encoding="UTF-8" ?>
            <r />
            """, formatted);
    }
}
