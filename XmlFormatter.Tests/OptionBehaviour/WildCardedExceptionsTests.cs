namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// WildCardedExceptionsForPositionAllAttributesOnFirstLine lists regular expressions matched
/// against element names. A match exempts that element from PositionAllAttributesOnFirstLine,
/// so its attributes wrap by the usual threshold instead.
///
/// Despite "WildCarded" in the name the patterns are regular expressions, matched unanchored
/// with Regex.IsMatch - a bare "item" matches "lineitem" as well. The list is only consulted
/// when PositionAllAttributesOnFirstLine is on; on its own it does nothing.
/// </summary>
public class WildCardedExceptionsTests
{
    private const string ThreeAttributes = """<r a="1" b="2" c="3"/>""";

    private static Options AllOnFirstLineExcept(params string[] patterns) =>
        TestOptions.NoDeclaration with
        {
            PositionAllAttributesOnFirstLine = true,
            WildCardedExceptionsForPositionAllAttributesOnFirstLine = [.. patterns],
        };

    [Fact]
    public void A_matching_element_falls_back_to_wrapping()
    {
        var formatted = TestFormatter.Format(ThreeAttributes, AllOnFirstLineExcept("^r$"));

        Assert.Equal("""
            <r a="1"
               b="2"
               c="3" />
            """, formatted);
    }

    [Fact]
    public void A_non_matching_element_keeps_its_attributes_on_one_line()
    {
        var formatted = TestFormatter.Format(ThreeAttributes, AllOnFirstLineExcept("^other$"));

        Assert.Equal("""<r a="1" b="2" c="3" />""", formatted);
    }

    [Fact]
    public void Patterns_are_regular_expressions_matched_unanchored()
    {
        var formatted = TestFormatter.Format("""<lineitem a="1" b="2" c="3"/>""", AllOnFirstLineExcept("item"));

        Assert.Equal("""
            <lineitem a="1"
                      b="2"
                      c="3" />
            """, formatted);
    }

    [Fact]
    public void The_list_does_nothing_while_all_attributes_on_first_line_is_off()
    {
        var options = TestOptions.NoDeclaration with
        {
            WildCardedExceptionsForPositionAllAttributesOnFirstLine = ["^r$"],
        };

        var formatted = TestFormatter.Format(ThreeAttributes, options);

        Assert.Equal("""
            <r a="1"
               b="2"
               c="3" />
            """, formatted);
    }

    [Fact]
    public void Exemption_applies_per_element_not_per_document()
    {
        var options = AllOnFirstLineExcept("^wrapped$");

        var formatted = TestFormatter.Format("""<r><wrapped a="1" b="2"/><inline a="1" b="2"/></r>""", options);

        Assert.Equal("""
            <r>
                <wrapped a="1"
                         b="2" />
                <inline a="1" b="2" />
            </r>
            """, formatted);
    }
}
