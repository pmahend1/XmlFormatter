namespace XmlFormatter.Tests.OptionBehavior;

public class PositionAllAttributesOnFirstLineTests
{
    private const string ThreeAttributes = """<r a="1" b="2" c="3"/>""";

    [Fact]
    public void False_by_default_leaves_the_threshold_in_charge()
    {
        var formatted = TestFormatter.Format(ThreeAttributes, TestOptions.NoDeclaration);

        Assert.Equal("""
            <r a="1"
               b="2"
               c="3" />
            """, formatted);
    }

    [Fact]
    public void True_keeps_every_attribute_on_one_line()
    {
        var formatted = TestFormatter.Format(ThreeAttributes,
                                             TestOptions.NoDeclaration with { PositionAllAttributesOnFirstLine = true });

        Assert.Equal("""<r a="1" b="2" c="3" />""", formatted);
    }

    [Fact]
    public void True_overrides_a_threshold_that_would_have_wrapped()
    {
        var options = TestOptions.NoDeclaration with
        {
            PositionAllAttributesOnFirstLine = true,
            AttributesInNewlineThreshold = 1,
        };

        var formatted = TestFormatter.Format(ThreeAttributes, options);

        Assert.Equal("""<r a="1" b="2" c="3" />""", formatted);
    }

    [Fact]
    public void True_overrides_moving_the_first_attribute_down()
    {
        var options = TestOptions.NoDeclaration with
        {
            PositionAllAttributesOnFirstLine = true,
            PositionFirstAttributeOnSameLine = false,
        };

        var formatted = TestFormatter.Format(ThreeAttributes, options);

        Assert.Equal("""<r a="1" b="2" c="3" />""", formatted);
    }
}
