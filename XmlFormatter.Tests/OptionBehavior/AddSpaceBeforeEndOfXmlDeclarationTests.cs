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

    [Fact]
    public void The_generated_declaration_carries_the_space_regardless()
    {
        var formatted = TestFormatter.Format("<r/>", new Options { AddSpaceBeforeEndOfXmlDeclaration = false });

        Assert.Equal("""
            <?xml version="1.0" encoding="UTF-8" ?>
            <r />
            """, formatted);
    }
}
