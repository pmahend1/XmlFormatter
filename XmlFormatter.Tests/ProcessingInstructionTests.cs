namespace XmlFormatter.Tests;

/// <summary>
/// A processing instruction is a leaf like a comment, and is written the same way: the indent
/// for its depth and nothing else, with the line break before it left to the separator.
/// Regression cover for #58. Nothing here is option-driven - an instruction has no setting of
/// its own, and the shapes below come out the same whatever is set.
/// </summary>
public class ProcessingInstructionTests
{
    [Fact]
    public void An_instruction_inside_an_element_is_indented_to_its_depth()
    {
        var formatted = TestFormatter.Format("<r><?pi d?><a/></r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <?pi d?>
                         <a />
                     </r>
                     """,
                     formatted);
    }

    [Fact]
    public void An_instruction_is_not_followed_by_a_blank_line()
    {
        // Nothing follows it, so the only line break after it is the closing tag's.
        var formatted = TestFormatter.Format("<r><?pi d?></r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <?pi d?>
                     </r>
                     """,
                     formatted);
    }

    [Fact]
    public void Consecutive_instructions_each_get_their_own_line()
    {
        var formatted = TestFormatter.Format("<r><?p1 a?><?p2 b?></r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <?p1 a?>
                         <?p2 b?>
                     </r>
                     """,
                     formatted);
    }

    [Fact]
    public void An_instruction_is_indented_to_the_depth_it_sits_at()
    {
        var formatted = TestFormatter.Format("<r><b><?pi d?><a/></b></r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <b>
                             <?pi d?>
                             <a />
                         </b>
                     </r>
                     """,
                     formatted);
    }

    [Fact]
    public void An_instruction_with_no_data_keeps_none()
    {
        // <?pi?> is legal, and the space belongs to the data - hardcoding it emitted <?pi ?>.
        var formatted = TestFormatter.Format("<r><?pi?></r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <?pi?>
                     </r>
                     """,
                     formatted);
    }

    [Fact]
    public void Instruction_data_is_written_through_verbatim()
    {
        // Whatever sits between the target and ?> is opaque to the formatter, trailing space
        // included - Sample/XmlFile15.xml's <?xaml-comp compile="true" ?> carries one.
        var formatted = TestFormatter.Format("""<r><?xaml-comp compile="true" ?></r>""",
                                             TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <?xaml-comp compile="true" ?>
                     </r>
                     """,
                     formatted);
    }

    [Fact]
    public void A_document_level_instruction_is_not_indented()
    {
        // The stylesheet link, which is the common real-world case: it sits outside the root
        // element, so there is no depth to indent it to.
        var formatted = TestFormatter.Format("""<?xml-stylesheet type="text/xsl" href="a.xsl"?><r><a/></r>""",
                                             TestOptions.NoDeclaration);

        Assert.Equal("""
                     <?xml-stylesheet type="text/xsl" href="a.xsl"?>
                     <r>
                         <a />
                     </r>
                     """,
                     formatted);
    }

    [Fact]
    public void A_document_level_instruction_follows_the_xml_declaration()
    {
        // The declaration is an XmlDeclaration node, not a ProcessingInstruction, and is written
        // by FormatXmlDocument before the loop ever sees one - it must be unaffected.
        var formatted = new Formatter().Format("""<?xml version="1.0"?><?pi d?><r/>""",
                                               new Options())
                                       .Replace("\r\n", "\n");

        Assert.Equal("""
                     <?xml version="1.0"?>
                     <?pi d?>
                     <r />
                     """,
                     formatted);
    }

    [Fact]
    public void An_instruction_beside_text_does_not_strand_an_indent()
    {
        // An indent beside text lands mid-line, returns as part of that text node, and is
        // indented again on the next format.
        var formatted = TestFormatter.Format("<r><?pi d?>text</r>", TestOptions.NoDeclaration);

        Assert.Equal("""
                     <r>
                         <?pi d?>text
                     </r>
                     """,
                     formatted);
        Assert.Equal(formatted, TestFormatter.Format(formatted, TestOptions.NoDeclaration));
    }

    [Theory]
    [InlineData("<r><?pi d?><a/></r>")]
    [InlineData("<r><?pi d?></r>")]
    [InlineData("<r><?pi?></r>")]
    [InlineData("<r><b><?pi d?><a/></b></r>")]
    [InlineData("<r><?p1 a?><?p2 b?></r>")]
    [InlineData("<r>text<?pi d?></r>")]
    // This one gained an indent on every pass and never settled.
    [InlineData("<r><?pi d?>text</r>")]
    [InlineData("<r><a/>text<?pi d?><b/></r>")]
    [InlineData("""<?xml-stylesheet type="text/xsl" href="a.xsl"?><r><a/></r>""")]
    [InlineData("<r><a/></r><?pi d?>")]
    public void An_instruction_settles_after_one_pass(string xml)
    {
        var once = TestFormatter.Format(xml, TestOptions.NoDeclaration);
        var twice = TestFormatter.Format(once, TestOptions.NoDeclaration);

        Assert.Equal(once, twice);
        Assert.Equal(twice, TestFormatter.Format(twice, TestOptions.NoDeclaration));
    }
}
