namespace XmlFormatter.Tests.OptionBehavior;

/// <summary>
/// The "greater than two children" threshold is the documented contract, not an off-by-one:
/// "Add empty line between elements if the child count is greater than 2".
/// </summary>
/// <remarks>
/// The tests at the end pair this option with <see cref="Options.PreserveNewLines"/>, the only
/// thing that puts the indentation between elements into the DOM.
/// </remarks>
public class AddEmptyLineBetweenElementsTests
{
    private static Options BlankLines => TestOptions.NoDeclaration with { AddEmptyLineBetweenElements = true };

    private static Options BlankLinesKeepingNewLines => BlankLines with { PreserveNewLines = true };

    [Fact]
    public void False_by_default_packs_the_siblings_together()
    {
        var formatted = TestFormatter.Format("<r><a/><b/><c/></r>", TestOptions.NoDeclaration);

        Assert.Equal("""
            <r>
                <a />
                <b />
                <c />
            </r>
            """, formatted);
    }

    [Fact]
    public void True_separates_siblings_and_stops_before_the_close_tag()
    {
        var formatted = TestFormatter.Format("<r><a/><b/><c/></r>", BlankLines);

        Assert.Equal("<r>\n    <a />\n\n    <b />\n\n    <c />\n</r>", formatted);
    }

    [Fact]
    public void True_leaves_exactly_two_siblings_packed()
    {
        // The documented threshold. Two children is the boundary case, so it is pinned here
        // rather than left to be inferred from the three-child test above.
        var formatted = TestFormatter.Format("<r><a/><b/></r>", BlankLines);

        Assert.Equal("""
            <r>
                <a />
                <b />
            </r>
            """, formatted);
    }

    [Fact]
    public void True_leaves_a_single_child_alone()
    {
        var formatted = TestFormatter.Format("<r><a/></r>", BlankLines);

        Assert.Equal("""
            <r>
                <a />
            </r>
            """, formatted);
    }

    [Fact]
    public void True_does_not_break_up_text_content()
    {
        var formatted = TestFormatter.Format("<r>hello</r>", BlankLines);

        Assert.Equal("<r>hello</r>", formatted);
    }

    /*
     * The same three contracts again, on input that carries indentation. Those Whitespace nodes
     * are regenerated rather than written, so counting them read n children as 2n + 1 and the
     * second pass disagreed with the first (#53, #54).
     */

    [Theory]
    [InlineData("<r><a/></r>")]
    [InlineData("<r><a/><b/></r>")]
    [InlineData("<r><a/><b/><c/></r>")]
    public void True_with_preserved_new_lines_formats_its_own_output_unchanged(string minified)
    {
        // The fixture corpus is all indented already, so its second pass matches its first even
        // with the bug present; only a document that gains indentation shows the drift.
        var once = TestFormatter.Format(minified, BlankLinesKeepingNewLines);

        Assert.Equal(once, TestFormatter.Format(once, BlankLinesKeepingNewLines));
    }

    [Fact]
    public void True_with_preserved_new_lines_leaves_exactly_two_siblings_packed()
    {
        var formatted = TestFormatter.Format("<r>\n    <a />\n    <b />\n</r>", BlankLinesKeepingNewLines);

        Assert.Equal("<r>\n    <a />\n    <b />\n</r>", formatted);
    }

    [Fact]
    public void True_with_preserved_new_lines_stops_before_the_close_tag()
    {
        // The indentation before </r> is not a sibling, so the last child earns nothing after it.
        var formatted = TestFormatter.Format("<r>\n    <a />\n\n    <b />\n\n    <c />\n</r>", BlankLinesKeepingNewLines);

        Assert.Equal("<r>\n    <a />\n\n    <b />\n\n    <c />\n</r>", formatted);
    }

    [Fact]
    public void True_with_preserved_new_lines_leaves_a_single_child_alone()
    {
        var formatted = TestFormatter.Format("<r>\n    <a />\n</r>", BlankLinesKeepingNewLines);

        Assert.Equal("<r>\n    <a />\n</r>", formatted);
    }
}
