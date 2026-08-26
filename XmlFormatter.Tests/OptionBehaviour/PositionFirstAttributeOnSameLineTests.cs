namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// PositionFirstAttributeOnSameLine decides where a wrapped attribute list starts: after the
/// element name, or on the line below it.
///
/// The two produce different continuation indents. Keeping the first attribute on the name's
/// line aligns the rest under it, so the indent depends on the element name's length; moving
/// it down uses one IndentLength from the element instead.
/// </summary>
public class PositionFirstAttributeOnSameLineTests
{
    private const string TwoAttributes = """<r a="1" b="2"/>""";

    [Fact]
    public void True_by_default_aligns_continuations_under_the_first_attribute()
    {
        var formatted = TestFormatter.Format(TwoAttributes, TestOptions.NoDeclaration);

        // Three spaces: the "<", the name, and the space before the first attribute.
        Assert.Equal("""
            <r a="1"
               b="2" />
            """, formatted);
    }

    [Fact]
    public void False_moves_every_attribute_below_the_element_name()
    {
        var formatted = TestFormatter.Format(TwoAttributes,
                                             TestOptions.NoDeclaration with { PositionFirstAttributeOnSameLine = false });

        Assert.Equal("""
            <r
                a="1"
                b="2" />
            """, formatted);
    }

    [Fact]
    public void False_moves_a_single_attribute_down_too()
    {
        // The newline threshold only applies while the first attribute stays on the name's
        // line, so a lone attribute wraps here where it would not by default.
        var formatted = TestFormatter.Format("""<r a="1"/>""",
                                             TestOptions.NoDeclaration with { PositionFirstAttributeOnSameLine = false });

        Assert.Equal("""
            <r
                a="1" />
            """, formatted);
    }

    [Fact]
    public void Continuation_indent_follows_the_element_into_a_nested_position()
    {
        var formatted = TestFormatter.Format("""<r><child a="1" b="2"/></r>""", TestOptions.NoDeclaration);

        Assert.Equal("""
            <r>
                <child a="1"
                       b="2" />
            </r>
            """, formatted);
    }
}
