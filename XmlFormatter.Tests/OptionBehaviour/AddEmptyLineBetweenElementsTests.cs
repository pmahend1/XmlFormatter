namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// AddEmptyLineBetweenElements separates sibling elements with a blank line.
///
/// The blank line goes *between* siblings, so none is written after the last one. It applies
/// to element siblings only - text and CDATA are left alone, since a blank line there would
/// change the document's content rather than its layout.
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
    public void True_should_separate_two_siblings_as_well()
    {
        KnownFailure.Expect("AddEmptyLineBetweenElements does nothing when the parent has exactly two "
                          + "children. The blank line is guarded by `childCount > 2` in PrintNode, which "
                          + "looks like an off-by-one: with two children there is one gap to fill, and "
                          + "the option fills every other gap in the document.",
                            () =>
                            {
                                var formatted = TestFormatter.Format("<r><a/><b/></r>", BlankLines);

                                Assert.Equal("<r>\n    <a />\n\n    <b />\n</r>", formatted);
                            });
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
