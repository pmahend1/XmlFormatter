namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// AddSpaceBeforeSelfClosingTag controls the space in "&lt;a /&gt;". It only applies when
/// UseSelfClosingTags is on - with an explicit end tag there is nothing to separate.
/// </summary>
public class AddSpaceBeforeSelfClosingTagTests
{
    private const string Empty = "<r><a/></r>";

    [Fact]
    public void True_by_default_separates_the_slash_from_the_name()
    {
        var formatted = TestFormatter.Format(Empty, TestOptions.NoDeclaration);

        Assert.Equal("""
            <r>
                <a />
            </r>
            """, formatted);
    }

    [Fact]
    public void False_closes_the_tag_tightly()
    {
        var formatted = TestFormatter.Format(Empty, TestOptions.NoDeclaration with { AddSpaceBeforeSelfClosingTag = false });

        Assert.Equal("""
            <r>
                <a/>
            </r>
            """, formatted);
    }

    [Fact]
    public void Has_no_effect_when_self_closing_tags_are_off()
    {
        var options = TestOptions.NoDeclaration with
        {
            UseSelfClosingTags = false,
            AddSpaceBeforeSelfClosingTag = false,
        };

        var formatted = TestFormatter.Format(Empty, options);

        Assert.Equal("""
            <r>
                <a></a>
            </r>
            """, formatted);
    }
}
