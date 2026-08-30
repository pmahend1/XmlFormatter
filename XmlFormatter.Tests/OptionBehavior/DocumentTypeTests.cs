using System.Xml;

namespace XmlFormatter.Tests.OptionBehavior;

/// <summary>
/// "Limited DTD support" is a documented limitation, not a bug to fix. These record what
/// works and hold the SYSTEM case as a known failure.
/// </summary>
public class DocumentTypeTests
{
    [Fact]
    public void A_public_identifier_keeps_its_keyword()
    {
        var formatted = TestFormatter.Format("""<!DOCTYPE root PUBLIC "-//X//DTD//EN" "my.dtd"><root/>""",
                                             TestOptions.NoDeclaration);

        Assert.Equal("""
            <!DOCTYPE root PUBLIC "-//X//DTD//EN" "my.dtd">
            <root />
            """, formatted);
    }

    [Fact]
    public void An_internal_subset_is_carried_through()
    {
        var formatted = TestFormatter.Format("""<!DOCTYPE root [<!ENTITY e "x">]><root>&e;</root>""",
                                             TestOptions.NoDeclaration);

        Assert.Equal("""
            <!DOCTYPE root [<!ENTITY e "x">]>
            <root>&e;</root>
            """, formatted);
    }

    [Fact]
    public void A_system_identifier_keeps_its_keyword()
    {
        var formatted = TestFormatter.Format("""<!DOCTYPE root SYSTEM "my.dtd"><root/>""",
                                             TestOptions.NoDeclaration);

        Assert.Equal("""
            <!DOCTYPE root SYSTEM "my.dtd">
            <root />
            """, formatted);
    }

    [Fact]
    public void The_output_of_a_system_identifier_parses()
    {
        // The point of the keyword: without it the formatter could not read its own output back.
        var formatted = TestFormatter.Format("""<!DOCTYPE root SYSTEM "my.dtd"><root/>""", TestOptions.NoDeclaration);

        new XmlDocument().LoadXml(formatted);
    }
}
