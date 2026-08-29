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
    public void A_system_identifier_should_keep_its_keyword()
    {
        KnownFailure.Expect("The SYSTEM keyword is dropped. FormatXMLDocument writes the public id with "
                          + "its keyword but the system id bare, which is correct only when a public id "
                          + "is present - PUBLIC takes two literals. With no public id the output is "
                          + "`<!DOCTYPE root \"my.dtd\">`, which no parser will read back. Filed under the "
                          + "documented DTD limitation rather than the roadmap, so this exemption is "
                          + "expected to stand. If it is ever picked up, emit SYSTEM when PublicId is null.",
                            () =>
                            {
                                var formatted = TestFormatter.Format("""<!DOCTYPE root SYSTEM "my.dtd"><root/>""",
                                                                     TestOptions.NoDeclaration);

                                Assert.Equal("""
                                    <!DOCTYPE root SYSTEM "my.dtd">
                                    <root />
                                    """, formatted);
                            });
    }

    [Fact]
    public void The_output_of_a_system_identifier_does_not_parse()
    {
        // Asserts the *broken* behavior deliberately: this is why the known failure matters,
        // and it fails when the fix lands, which is the reminder to delete it.
        var formatted = TestFormatter.Format("""<!DOCTYPE root SYSTEM "my.dtd"><root/>""", TestOptions.NoDeclaration);

        Assert.Throws<System.Xml.XmlException>(() => new System.Xml.XmlDocument().LoadXml(formatted));
    }
}
