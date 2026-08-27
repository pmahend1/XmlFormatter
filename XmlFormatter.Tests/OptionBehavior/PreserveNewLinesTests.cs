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
        const string Minified = "<r><a/><b/></r>";

        Assert.Equal(TestFormatter.Format(Minified, TestOptions.NoDeclaration),
                     TestFormatter.Format(Minified, Preserving));
    }

    [Fact]
    public void True_should_not_emit_the_indentation_that_precedes_a_comment()
    {
        KnownFailure.Expect("PreserveNewLines emits structural whitespace as content when the node next "
                          + "to it is a comment rather than an element. The `hasElementSibling` guard in "
                          + "PrintNode only looks for Element siblings, so the indent before a comment is "
                          + "written out and then indented again - producing a line of trailing spaces "
                          + "and a stray blank line.",
                            () =>
                            {
                                var formatted = TestFormatter.Format("<r>\n  <!--why-->\n  <a/>\n</r>", Preserving);

                                Assert.Equal("""
                                    <r>
                                        <!-- why -->
                                        <a />
                                    </r>
                                    """, formatted);
                            });
    }

    [Fact]
    public void True_should_not_leave_a_blank_line_after_a_trailing_comment()
    {
        KnownFailure.Expect("Same cause as the comment-indentation failure above: the whitespace after a "
                          + "trailing comment has no element sibling either, so it is emitted as content "
                          + "and the close tag adds its own newline on top, leaving a blank line before "
                          + "</r>.",
                            () =>
                            {
                                var options = Preserving with { PreserveCommentPlacement = true };

                                var formatted = TestFormatter.Format("<r>\n  <a/> <!--why-->\n</r>", options);

                                Assert.Equal("<r>\n    <a /><!-- why -->\n</r>", formatted);
                            });
    }
}
