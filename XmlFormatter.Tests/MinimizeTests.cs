namespace XmlFormatter.Tests;

/// <summary>
/// Minimize is a separate implementation from Format, not a mode of it: XmlWriter does the
/// writing and it takes no Options. Where the two disagree is pinned below.
/// </summary>
public class MinimizeTests
{
    private const string Utf8Declaration = """<?xml version="1.0" encoding="utf-8"?>""";

    private static string Minimize(string xml) => new Formatter().Minimize(xml);

    [Fact]
    public void Indentation_and_line_breaks_are_removed()
    {
        var minimized = Minimize("<r>\n  <a x=\"1\" />\n  <b>text</b>\n</r>");

        Assert.Equal($"{Utf8Declaration}<r><a x=\"1\" /><b>text</b></r>", minimized);
    }

    [Fact]
    public void A_declaration_is_added_even_when_the_input_had_none()
    {
        // Not optional: AddXmlDeclarationIfMissing cannot reach here.
        var minimized = Minimize("<r><a/></r>");

        Assert.Equal($"{Utf8Declaration}<r><a /></r>", minimized);
    }

    [Fact]
    public void An_existing_encoding_is_carried_into_the_output()
    {
        // What StringWriterWithEncoding exists for: XmlWriter reads the encoding off the writer.
        var minimized = Minimize("""<?xml version="1.0" encoding="utf-16"?><r><a/></r>""");

        Assert.StartsWith("""<?xml version="1.0" encoding="utf-16"?>""", minimized);
    }

    [Fact]
    public void A_legacy_code_page_encoding_is_handled()
    {
        // Regression test for the CodePagesEncodingProvider registration in Formatter.
        var minimized = Minimize("""<?xml version="1.0" encoding="windows-1252"?><r><a/></r>""");

        // The declared name is echoed as written, not normalized to the provider's casing.
        Assert.Equal("""<?xml version="1.0" encoding="windows-1252"?><r><a /></r>""", minimized);
    }

    [Fact]
    public void Built_in_encodings_are_carried_through()
    {
        // Resolve without the code-pages provider, which is why the crash was easy to miss.
        Assert.StartsWith("""<?xml version="1.0" encoding="iso-8859-1"?>""",
                          Minimize("""<?xml version="1.0" encoding="iso-8859-1"?><r/>"""));
        Assert.StartsWith("""<?xml version="1.0" encoding="us-ascii"?>""",
                          Minimize("""<?xml version="1.0" encoding="ascii"?><r/>"""));
    }

    [Fact]
    public void An_unknown_encoding_name_still_throws_the_wrong_exception_type()
    {
        KnownFailure.Expect("Minimize throws ArgumentException, not XmlException, when the declaration "
                          + "names an encoding that does not exist. XmlDocument parses such a document "
                          + "happily, so this is Minimize's own failure - and the CLI catches only "
                          + "XmlException, so it reaches the user as an unhandled crash rather than the "
                          + "syntax error the extension promises to display.",
                            () =>
                            {
                                var bogus = """<?xml version="1.0" encoding="not-an-encoding"?><r/>""";

                                Assert.Throws<System.Xml.XmlException>(() => Minimize(bogus));
                            });
    }

    [Fact]
    public void Comments_are_kept()
    {
        var minimized = Minimize("<r>\n  <!-- note -->\n  <a/>\n</r>");

        Assert.Equal($"{Utf8Declaration}<r><!-- note --><a /></r>", minimized);
    }

    [Fact]
    public void Cdata_is_kept_intact()
    {
        var minimized = Minimize("<r><![CDATA[a < b]]></r>");

        Assert.Equal($"{Utf8Declaration}<r><![CDATA[a < b]]></r>", minimized);
    }

    [Fact]
    public void Text_content_is_preserved_exactly()
    {
        var minimized = Minimize("<r> spaced </r>");

        Assert.Equal($"{Utf8Declaration}<r> spaced </r>", minimized);
    }

    [Fact]
    public void Redundant_namespace_declarations_are_dropped()
    {
        // NamespaceHandling.OmitDuplicates. The child redeclares the same prefix and uri.
        var minimized = Minimize("""<r xmlns:p="urn:x"><p:a xmlns:p="urn:x"/></r>""");

        Assert.Equal($"{Utf8Declaration}<r xmlns:p=\"urn:x\"><p:a /></r>", minimized);
    }

    [Fact]
    public void An_empty_element_keeps_the_form_it_was_written_in()
    {
        // Minimize does not convert between the two the way UseSelfClosingTags does in Format.
        Assert.Equal($"{Utf8Declaration}<r><a></a></r>", Minimize("<r><a></a></r>"));
        Assert.Equal($"{Utf8Declaration}<r><a /></r>", Minimize("<r><a/></r>"));
    }

    [Fact]
    public void Non_ascii_text_is_left_literal_as_it_is_in_Format()
    {
        // The two paths agreed again once Format stopped re-encoding text (#216). They reach it
        // independently - XmlWriter here, OuterXml there - so it is worth pinning on both.
        var minimized = Minimize("<r>caf\u00E9</r>");

        Assert.Equal($"{Utf8Declaration}<r>caf\u00E9</r>", minimized);
        Assert.Equal("<r>caf\u00E9</r>", TestFormatter.Format("<r>caf\u00E9</r>", TestOptions.NoDeclaration));
    }

    [Fact]
    public void A_system_doctype_keeps_its_keyword_where_Format_drops_it()
    {
        // Format drops SYSTEM; XmlWriter keeps it, so the keyword is available - Format just
        // omits it. The trailing "[]" is an empty internal subset XmlWriter adds.
        var minimized = Minimize("""<!DOCTYPE root SYSTEM "my.dtd"><root/>""");

        Assert.Contains("""<!DOCTYPE root SYSTEM "my.dtd"[]>""", minimized);
    }
}
