namespace XmlFormatter.Tests.OptionBehavior;

public class WrapCommentTextWithSpacesTests
{
    private const string Comment = "<r><!--note--></r>";

    [Fact]
    public void True_by_default_pads_the_text()
    {
        var formatted = TestFormatter.Format(Comment, TestOptions.NoDeclaration);

        Assert.Equal("""
            <r>
                <!-- note -->
            </r>
            """, formatted);
    }

    [Fact]
    public void False_writes_the_text_tight_against_the_delimiters()
    {
        var formatted = TestFormatter.Format(Comment, TestOptions.NoDeclaration with { WrapCommentTextWithSpaces = false });

        Assert.Equal("""
            <r>
                <!--note-->
            </r>
            """, formatted);
    }

    [Fact]
    public void Existing_padding_is_trimmed_before_the_option_is_applied()
    {
        var formatted = TestFormatter.Format("<r><!--   note   --></r>",
                                             TestOptions.NoDeclaration with { WrapCommentTextWithSpaces = false });

        Assert.Equal("""
            <r>
                <!--note-->
            </r>
            """, formatted);
    }
}
