namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// AddEmptyLineBetweenElements separates sibling elements with a blank line, and only where
/// the child count is greater than two.
///
/// That threshold is the documented contract, not an accident of the implementation - the
/// PrettyXML setting reads "Add empty line between elements if the child count is greater
/// than 2", so a parent with exactly two children is left packed on purpose.
///
/// The blank line goes *between* siblings, so none is written before the first or after the
/// last. It applies to element siblings only - text and CDATA are left alone, since a blank
/// line there would change the document's content rather than its layout.
/// </summary>
public class AddEmptyLineBetweenElementsTests
{
    private static Options BlankLines => TestOptions.NoDeclaration with { AddEmptyLineBetweenElements = true };

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
}
