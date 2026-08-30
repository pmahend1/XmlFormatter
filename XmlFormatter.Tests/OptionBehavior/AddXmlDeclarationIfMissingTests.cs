namespace XmlFormatter.Tests.OptionBehavior;

public class AddXmlDeclarationIfMissingTests
{
    [Fact]
    public void True_by_default_prepends_the_library_declaration()
    {
        var formatted = TestFormatter.Format("<r/>", new Options());

        Assert.Equal("""
            <?xml version="1.0" encoding="UTF-8"?>
            <r />
            """, formatted);
    }

    [Fact]
    public void False_leaves_the_document_without_one()
    {
        var formatted = TestFormatter.Format("<r/>", new Options { AddXmlDeclarationIfMissing = false });

        Assert.Equal("<r />", formatted);
    }

    [Fact]
    public void An_existing_declaration_is_kept_verbatim()
    {
        // Including its encoding, which differs in case from the generated one.
        var formatted = TestFormatter.Format("""<?xml version="1.0" encoding="utf-8"?><r/>""", new Options());

        Assert.Equal("""
            <?xml version="1.0" encoding="utf-8"?>
            <r />
            """, formatted);
    }

    [Fact]
    public void An_existing_declaration_survives_the_option_being_off()
    {
        var formatted = TestFormatter.Format("""<?xml version="1.0" encoding="utf-8"?><r/>""",
                                             new Options { AddXmlDeclarationIfMissing = false });

        Assert.Equal("""
            <?xml version="1.0" encoding="utf-8"?>
            <r />
            """, formatted);
    }
}
