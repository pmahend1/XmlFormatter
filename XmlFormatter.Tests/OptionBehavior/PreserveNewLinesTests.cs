namespace XmlFormatter.Tests.OptionBehavior;

/// <summary>
/// Loads with PreserveWhitespace, but structural indentation is still regenerated (#209), so
/// the visible effect is narrow: whitespace that is an element's sole content survives, and a
/// blank line between two siblings survives as exactly one (#74).
/// </summary>
public class PreserveNewLinesTests
{
    private static Options Preserving => TestOptions.NoDeclaration with { PreserveNewLines = true };

    [Fact]
    public void False_by_default_discards_whitespace_only_content()
    {
        var formatted = TestFormatter.Format("<r>   </r>", TestOptions.NoDeclaration);

        Assert.Equal("<r />", formatted);
    }

    [Fact]
    public void True_keeps_whitespace_that_is_the_only_content()
    {
        var formatted = TestFormatter.Format("<r>   </r>", Preserving);

        Assert.Equal("<r>   </r>", formatted);
    }

    [Fact]
    public void True_keeps_a_sole_cdata_child_inline()
    {
        var formatted = TestFormatter.Format("<r><![CDATA[x]]></r>", Preserving);

        Assert.Equal("<r><![CDATA[x]]></r>", formatted);
    }

    [Fact]
    public void True_indents_a_sole_element_child_normally()
    {
        // The sole-child path is the whitespace one; an element there is still laid out.
        var formatted = TestFormatter.Format("<r><a/></r>", Preserving);

        Assert.Equal("<r>\n    <a />\n</r>", formatted);
    }

    [Fact]
    public void True_keeps_whitespace_without_a_line_break_as_nested_content()
    {
        var formatted = TestFormatter.Format("<r><a> </a></r>", Preserving);

        Assert.Equal("<r>\n    <a> </a>\n</r>", formatted);
    }

    [Theory]
    [InlineData("<r><a>\n</a></r>")]
    [InlineData("<r><a>\n\n</a></r>")]
    [InlineData("<r><a>\n </a></r>")]
    public void True_puts_the_end_tag_after_sole_whitespace_spanning_lines_at_its_own_indent(string xml)
    {
        // The source's trailing spaces used to decide the end tag's column.
        var formatted = TestFormatter.Format(xml, Preserving);

        Assert.Equal("<r>\n    <a>\n    </a>\n</r>", formatted);
    }

    [Fact]
    public void True_drops_the_indent_of_sole_whitespace_spanning_lines_at_the_root()
    {
        var formatted = TestFormatter.Format("<r>\n  </r>", Preserving);

        Assert.Equal("<r>\n</r>", formatted);
    }

    [Fact]
    public void True_collapses_a_run_of_newlines_in_whitespace_only_content()
    {
        // Sole whitespace spanning lines is layout (#69), and a blank line needs a sibling each side.
        var formatted = TestFormatter.Format("<r>\n\n\n</r>", Preserving);

        Assert.Equal("<r>\n</r>", formatted);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void True_keeps_a_blank_line_between_siblings_as_exactly_one(string newLine)
    {
        // The PrettyXML README's example input for this option.
        var input = """
            <Root>
                  <Element1>Text1</Element1>


                <Element2>Text2</Element2>

                    <Element3>Text3</Element3>
                <Element3>Text4</Element3>
            </Root>
            """.Replace("\n", newLine);

        var formatted = TestFormatter.Format(input, Preserving);

        Assert.Equal("""
            <Root>
                <Element1>Text1</Element1>

                <Element2>Text2</Element2>

                <Element3>Text3</Element3>
                <Element3>Text4</Element3>
            </Root>
            """, formatted);
    }

    [Fact]
    public void True_writes_no_indent_on_the_blank_line()
    {
        var formatted = TestFormatter.Format("<r>\n  <a/>\n  \n\t\n  <b/>\n</r>", Preserving);

        Assert.Equal("<r>\n    <a />\n\n    <b />\n</r>", formatted);
    }

    [Fact]
    public void True_drops_a_blank_line_after_the_start_tag_or_before_the_end_tag()
    {
        var formatted = TestFormatter.Format("<r>\n\n    <a/>\n\n    <b/>\n\n</r>", Preserving);

        Assert.Equal("<r>\n    <a />\n\n    <b />\n</r>", formatted);
    }

    [Fact]
    public void True_keeps_a_blank_line_before_a_comment()
    {
        var formatted = TestFormatter.Format("<r>\n    <a/>\n\n    <!-- note -->\n    <b/>\n</r>", Preserving);

        Assert.Equal("<r>\n    <a />\n\n    <!-- note -->\n    <b />\n</r>", formatted);
    }

    [Fact]
    public void True_keeps_a_blank_line_around_a_processing_instruction_and_cdata()
    {
        const string xml = "<r>\n    <a />\n\n    <?pi x?>\n\n    <![CDATA[c]]>\n\n    <b />\n</r>";

        var formatted = TestFormatter.Format(xml, Preserving);

        Assert.Equal(xml, formatted);
    }

    [Fact]
    public void False_drops_a_blank_line_between_siblings()
    {
        var formatted = TestFormatter.Format("<r>\n    <a/>\n\n    <b/>\n</r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <a />\n    <b />\n</r>", formatted);
    }

    [Fact]
    public void True_produces_the_same_output_as_false_for_a_minified_document()
    {
        const string minified = "<r><a/><b/></r>";

        Assert.Equal(TestFormatter.Format(minified, TestOptions.NoDeclaration),
                     TestFormatter.Format(minified, Preserving));
    }

    [Fact]
    public void True_does_not_emit_the_indentation_that_precedes_a_comment()
    {
        var formatted = TestFormatter.Format("<r>\n  <!--why-->\n  <a/>\n</r>", Preserving);

        Assert.Equal("""
            <r>
                <!-- why -->
                <a />
            </r>
            """, formatted);
    }

    [Fact]
    public void True_does_not_leave_a_blank_line_after_a_trailing_comment()
    {
        var options = Preserving with { PreserveCommentPlacement = true };

        var formatted = TestFormatter.Format("<r>\n  <a/> <!--why-->\n</r>", options);

        Assert.Equal("<r>\n    <a /><!-- why -->\n</r>", formatted);
    }
}
