namespace XmlFormatter.Tests;

/// <summary>
/// Formatter.Minimize - the other half of the public API, and the CLI's second actionKind.
/// Until these tests it had no coverage at all, along with StringWriterWithEncoding, which
/// only Minimize constructs.
///
/// It is a different implementation from Format, not a mode of it: XmlWriter does the writing,
/// so its rules apply rather than any Options. Minimize takes no Options at all, and the
/// places where the two disagree are pinned below rather than left to be discovered.
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
        // Unlike Format, this is not optional - AddXmlDeclarationIfMissing cannot reach here,
        // because Minimize takes no Options.
        var minimized = Minimize("<r><a/></r>");

        Assert.Equal($"{Utf8Declaration}<r><a /></r>", minimized);
    }

    [Fact]
    public void An_existing_encoding_is_carried_into_the_output()
    {
        // The only thing StringWriterWithEncoding exists for: XmlWriter takes the encoding to
        // declare from the writer, so without it every document would come back as utf-8.
        var minimized = Minimize("""<?xml version="1.0" encoding="utf-16"?><r><a/></r>""");

        Assert.StartsWith("""<?xml version="1.0" encoding="utf-16"?>""", minimized);
    }

    [Fact]
    public void A_legacy_code_page_encoding_should_not_crash()
    {
        KnownFailure.Expect("Minimize throws ArgumentException on a document whose declaration names a "
                          + "code-page encoding. .NET Core dropped those from the default provider, so "
                          + "Encoding.GetEncoding(\"windows-1252\") needs CodePagesEncodingProvider "
                          + "registered. Format handles the same document, and the CLI catches only "
                          + "XmlException - so this escapes Main unhandled. iso-8859-1 and us-ascii are "
                          + "built in and work; windows-1252 is the common one that does not.",
                            () =>
                            {
                                var minimized = Minimize("""<?xml version="1.0" encoding="windows-1252"?><r><a/></r>""");

                                Assert.Contains("<r><a /></r>", minimized);
                            });
    }

    [Fact]
    public void Built_in_encodings_are_carried_through()
    {
        // The boundary of the failure above: these resolve without the code-pages provider.
        Assert.StartsWith("""<?xml version="1.0" encoding="iso-8859-1"?>""",
                          Minimize("""<?xml version="1.0" encoding="iso-8859-1"?><r/>"""));
        Assert.StartsWith("""<?xml version="1.0" encoding="us-ascii"?>""",
                          Minimize("""<?xml version="1.0" encoding="ascii"?><r/>"""));
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
    public void Non_ascii_text_is_left_literal_where_Format_would_escape_it()
    {
        // A real difference between the two paths, not an accident of these inputs: Format
        // runs text through EncodeNonAscii and emits &#xE9;, XmlWriter does not.
        var minimized = Minimize("<r>caf\u00E9</r>");

        Assert.Equal($"{Utf8Declaration}<r>caf\u00E9</r>", minimized);
        Assert.Equal("<r>caf&#xE9;</r>", TestFormatter.Format("<r>caf\u00E9</r>", TestOptions.NoDeclaration));
    }

    [Fact]
    public void A_system_doctype_keeps_its_keyword_where_Format_drops_it()
    {
        /*
         * The counterpart to DocumentTypeTests: XmlWriter writes SYSTEM correctly, so the two
         * entry points disagree on the same document. Worth pinning because it shows the
         * keyword is available at this level - the Format side omits it rather than lacking it.
         * The trailing "[]" is an empty internal subset that XmlWriter adds.
         */
        var minimized = Minimize("""<!DOCTYPE root SYSTEM "my.dtd"><root/>""");

        Assert.Contains("""<!DOCTYPE root SYSTEM "my.dtd"[]>""", minimized);
    }
}
