namespace XmlFormatter.Tests.OptionBehavior;

public class IndentLengthTests
{
    private const string Nested = "<r><a><b>t</b></a></r>";

    [Fact]
    public void Defaults_to_four_spaces_per_level()
    {
        var formatted = TestFormatter.Format(Nested, TestOptions.NoDeclaration);

        Assert.Equal("""
            <r>
                <a>
                    <b>t</b>
                </a>
            </r>
            """, formatted);
    }

    [Fact]
    public void Two_indents_by_two_spaces_per_level()
    {
        var formatted = TestFormatter.Format(Nested, TestOptions.NoDeclaration with { IndentLength = 2 });

        Assert.Equal("""
            <r>
              <a>
                <b>t</b>
              </a>
            </r>
            """, formatted);
    }

    [Fact]
    public void Zero_keeps_the_line_breaks_but_removes_the_indent()
    {
        var formatted = TestFormatter.Format(Nested, TestOptions.NoDeclaration with { IndentLength = 0 });

        Assert.Equal("""
            <r>
            <a>
            <b>t</b>
            </a>
            </r>
            """, formatted);
    }
}
