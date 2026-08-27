namespace XmlFormatter.Tests.OptionBehavior;

public class PreserveWhiteSpacesInCommentTests
{
    private const string Padded = "<r><!--   note   --></r>";

    [Fact]
    public void False_by_default_trims_the_comment_text()
    {
        var formatted = TestFormatter.Format(Padded, TestOptions.NoDeclaration);

        Assert.Equal("""
            <r>
                <!-- note -->
            </r>
            """, formatted);
    }

    [Fact]
    public void True_keeps_the_original_spacing()
    {
        var formatted = TestFormatter.Format(Padded, TestOptions.NoDeclaration with { PreserveWhiteSpacesInComment = true });

        Assert.Equal("""
            <r>
                <!--   note   -->
            </r>
            """, formatted);
    }

    [Fact]
    public void True_takes_precedence_over_wrapping()
    {
        var options = TestOptions.NoDeclaration with
        {
            PreserveWhiteSpacesInComment = true,
            WrapCommentTextWithSpaces = true,
        };

        var formatted = TestFormatter.Format("<r><!--note--></r>", options);

        Assert.Equal("""
            <r>
                <!--note-->
            </r>
            """, formatted);
    }

    [Fact]
    public void True_keeps_a_multi_line_comment_intact()
    {
        var formatted = TestFormatter.Format("<r><!--line1\nline2--></r>",
                                             TestOptions.NoDeclaration with { PreserveWhiteSpacesInComment = true });

        Assert.Equal("<r>\n    <!--line1\nline2-->\n</r>", formatted);
    }
}
