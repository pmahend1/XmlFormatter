namespace XmlFormatter.Tests.OptionBehavior;

public class UseSelfClosingTagsTests
{
    private const string Empty = "<r><a/></r>";

    [Fact]
    public void True_by_default_writes_a_self_closing_tag()
    {
        var formatted = TestFormatter.Format(Empty, TestOptions.NoDeclaration);

        Assert.Equal("""
            <r>
                <a />
            </r>
            """, formatted);
    }

    [Fact]
    public void False_writes_a_separate_end_tag()
    {
        var formatted = TestFormatter.Format(Empty, TestOptions.NoDeclaration with { UseSelfClosingTags = false });

        Assert.Equal("""
            <r>
                <a></a>
            </r>
            """, formatted);
    }

    [Fact]
    public void False_still_writes_an_end_tag_when_the_element_has_attributes()
    {
        var formatted = TestFormatter.Format("""<r><a x="1"/></r>""",
                                             TestOptions.NoDeclaration with { UseSelfClosingTags = false });

        Assert.Equal("""
            <r>
                <a x="1"></a>
            </r>
            """, formatted);
    }
}
