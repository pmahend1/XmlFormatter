namespace XmlFormatter.Tests.OptionBehavior;

/// <summary>
/// Placement is read from the whitespace around a comment, which PreserveNewLines is what
/// normally keeps. The option loads that whitespace for itself when it has to, so the pairing
/// is an implementation detail rather than an undocumented dependency: every case here is
/// asserted with and without PreserveNewLines, and the two have to agree.
/// </summary>
public class PreserveCommentPlacementTests
{
    private const string TrailingComment = "<r>\n  <a/> <!--why-->\n</r>";
    private const string OwnLineComment = "<r>\n  <!--why-->\n  <a/>\n</r>";
    private const string OwnLineCommentAfterElement = "<r>\n  <a/>\n  <!--why-->\n</r>";
    private const string NestedOwnLineComment = "<r>\n  <p>\n    <!--why-->\n    <a/>\n  </p>\n</r>";

    private static Options Preserving => TestOptions.NoDeclaration with { PreserveCommentPlacement = true };

    private static Options PreservingWithNewLines => Preserving with { PreserveNewLines = true };

    public static TheoryData<Options> BothWays => [Preserving, PreservingWithNewLines];

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

    [Theory]
    [MemberData(nameof(BothWays))]
    public void True_keeps_a_trailing_comment_on_the_element_line(Options options)
    {
        var formatted = TestFormatter.Format(TrailingComment, options);

        Assert.Equal("<r>\n    <a /><!-- why -->\n</r>", formatted);
    }

    [Theory]
    [MemberData(nameof(BothWays))]
    public void True_keeps_a_comment_on_its_own_line_indented(Options options)
    {
        var formatted = TestFormatter.Format(OwnLineComment, options);

        Assert.Equal("""
            <r>
                <!-- why -->
                <a />
            </r>
            """, formatted);
    }

    [Theory]
    [MemberData(nameof(BothWays))]
    public void True_keeps_a_comment_that_follows_an_element_on_its_own_line(Options options)
    {
        var formatted = TestFormatter.Format(OwnLineCommentAfterElement, options);

        Assert.Equal("""
            <r>
                <a />
                <!-- why -->
            </r>
            """, formatted);
    }

    [Theory]
    [MemberData(nameof(BothWays))]
    public void True_indents_an_own_line_comment_to_its_own_depth(Options options)
    {
        var formatted = TestFormatter.Format(NestedOwnLineComment, options);

        Assert.Equal("""
            <r>
                <p>
                    <!-- why -->
                    <a />
                </p>
            </r>
            """, formatted);
    }

    /// <summary>
    /// The first child has no preceding sibling at all, which is not the same as having one that
    /// ended a line. Reading it as own-line put the comment at the left margin.
    /// </summary>
    [Theory]
    [MemberData(nameof(BothWays))]
    public void True_keeps_a_comment_on_the_line_of_the_tag_that_opened_it(Options options)
    {
        var formatted = TestFormatter.Format("<r><!--why--><a/></r>", options);

        Assert.Equal("<r><!-- why -->\n    <a />\n</r>", formatted);
    }

    [Theory]
    [MemberData(nameof(BothWays))]
    public void True_keeps_two_comments_sharing_a_line_together(Options options)
    {
        var formatted = TestFormatter.Format("<r>\n  <!--one--><!--two-->\n</r>", options);

        Assert.Equal("<r>\n    <!-- one --><!-- two -->\n</r>", formatted);
    }

    [Theory]
    [MemberData(nameof(BothWays))]
    public void True_leaves_a_document_level_comment_unindented(Options options)
    {
        var formatted = TestFormatter.Format("<!--top-->\n<r>\n  <a/>\n</r>", options);

        Assert.Equal("<!-- top -->\n<r>\n    <a />\n</r>", formatted);
    }

    [Theory]
    [MemberData(nameof(BothWays))]
    public void True_settles_after_one_pass(Options options)
    {
        const string mixed = "<r>\n  <a/> <!--trailing-->\n  <!--own line-->\n  <b/>\n</r>";

        var once = TestFormatter.Format(mixed, options);
        var twice = TestFormatter.Format(once, options);

        Assert.Equal(once, twice);
    }

    /// <summary>
    /// The option loads whitespace it then discards; anything it fails to discard would show up
    /// as a difference against a run that never loaded any.
    /// </summary>
    [Fact]
    public void True_changes_nothing_in_a_document_without_comments()
    {
        const string xml = "<r>\n  <a x=\"1\"/>\n  <p>text</p>\n  <![CDATA[raw]]>\n</r>";

        Assert.Equal(TestFormatter.Format(xml, TestOptions.NoDeclaration),
                     TestFormatter.Format(xml, Preserving));
    }
}
