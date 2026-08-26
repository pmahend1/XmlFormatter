namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// DOCTYPE handling is not driven by an option, and DTDs are documented as having limited
/// support - they carry validation rules of their own that this formatter does not model, in
/// the way XSLT does not either.
///
/// These tests do not argue with that scope. They pin the cases that already work, and record
/// the one place where limited support goes past "not handled" into rewriting a document the
/// parser accepted into one it will reject.
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
                          + "`<!DOCTYPE root \"my.dtd\">`, which no parser will read back. This is a one-line "
                          + "fix in the emitter and needs none of the DTD support the formatter does not "
                          + "claim: the keyword is already in hand, it is just not written.",
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
        /*
         * Spells out the consequence of the failure above. This one asserts the *broken*
         * behaviour on purpose: it is the evidence that the known failure matters, and it will
         * start failing at the same moment the fix lands, which is the reminder to delete it.
         */
        var formatted = TestFormatter.Format("""<!DOCTYPE root SYSTEM "my.dtd"><root/>""", TestOptions.NoDeclaration);

        Assert.Throws<System.Xml.XmlException>(() => new System.Xml.XmlDocument().LoadXml(formatted));
    }
}
