namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// PreserveCommentPlacement keeps a comment on the line it was written on, rather than
/// moving every comment onto a line of its own.
///
/// It decides by looking at what precedes the comment in the tree: a comment whose previous
/// sibling is whitespace containing a newline was on its own line and stays there, and
/// anything else is treated as trailing a sibling on the same line. That test only works when
/// the whitespace is in the tree at all, which is what PreserveNewLines controls.
///
/// Several PrettyXML settings do document a dependency on another - "Ignored if Use Single
/// Quotes is Checked" and the like - but this pairing is not one of them, and the failure
/// mode is silent rather than an "ignored". See the failing test below.
/// </summary>
public class PreserveCommentPlacementTests
{
    private const string TrailingComment = "<r>\n  <a/> <!--why-->\n</r>";
    private const string OwnLineComment = "<r>\n  <!--why-->\n  <a/>\n</r>";

    private static Options Preserving => TestOptions.NoDeclaration with { PreserveCommentPlacement = true };

    [Fact]
    public void False_by_default_moves_a_trailing_comment_onto_its_own_line()
    {
        var formatted = TestFormatter.Format(TrailingComment, TestOptions.NoDeclaration);

        Assert.Equal("""
            <r>
                <a />
                <!-- why -->
            </r>
            """, formatted);
    }

    [Fact]
    public void True_keeps_a_trailing_comment_on_the_element_line()
    {
        var formatted = TestFormatter.Format(TrailingComment, Preserving);

        Assert.Equal("<r>\n    <a /><!-- why -->\n</r>", formatted);
    }

    [Fact]
    public void True_with_preserved_newlines_keeps_a_comment_on_its_own_line_indented()
    {
        var options = Preserving with { PreserveNewLines = true };

        var formatted = TestFormatter.Format(OwnLineComment, options);

        Assert.Contains("    <!-- why -->", formatted);
    }

    [Fact]
    public void True_alone_should_still_indent_a_comment_on_its_own_line()
    {
        KnownFailure.Expect("PreserveCommentPlacement without PreserveNewLines writes an own-line comment "
                          + "hard against the left margin. It decides indentation by looking for a "
                          + "preceding Whitespace sibling, and without PreserveNewLines those nodes were "
                          + "dropped at load - so every comment looks like a trailing one. If the "
                          + "dependency is intended it belongs in the setting's description the way the "
                          + "other ignored-if pairs are; either way, losing the indent is not the right "
                          + "way to express it.",
                            () =>
                            {
                                var formatted = TestFormatter.Format(OwnLineComment, Preserving);

                                Assert.Equal("""
                                    <r>
                                        <!-- why -->
                                        <a />
                                    </r>
                                    """, formatted);
                            });
    }
}
