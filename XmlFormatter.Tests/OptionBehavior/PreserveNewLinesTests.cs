namespace XmlFormatter.Tests.OptionBehavior;

/// <summary>
/// Loads with PreserveWhitespace, but structural indentation is still regenerated (#209), so
/// the visible effect is narrow: only whitespace that is an element's sole content survives.
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
    public void True_keeps_whitespace_that_spans_lines_as_the_only_content()
    {
        var formatted = TestFormatter.Format("<r>\n  </r>", Preserving);

        Assert.Equal("<r>\n  </r>", formatted);
    }

    [Fact]
    public void True_collapses_a_run_of_newlines_in_whitespace_only_content()
    {
        // Blank lines are AddEmptyLineBetweenElements' job, not this option's.
        var formatted = TestFormatter.Format("<r>\n\n\n</r>", Preserving);

        Assert.Equal("<r>\n</r>", formatted);
    }

    [Fact]
    public void True_still_regenerates_indentation_between_elements()
    {
        // The blank lines in the input are structural, and structural whitespace is rebuilt.
        // AddEmptyLineBetweenElements is the option that puts blank lines back.
        var formatted = TestFormatter.Format("<r>\n  <a/>\n\n\n  <b/>\n</r>", Preserving);

        Assert.Equal("""
            <r>
                <a />
                <b />
            </r>
            """, formatted);
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
