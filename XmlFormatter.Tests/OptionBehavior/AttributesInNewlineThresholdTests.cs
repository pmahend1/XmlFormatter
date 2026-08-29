namespace XmlFormatter.Tests.OptionBehavior;

public class AttributesInNewlineThresholdTests
{
    private const string ThreeAttributes = """<r a="1" b="2" c="3"/>""";

    [Fact]
    public void One_attribute_stays_inline_at_the_default_threshold_of_one()
    {
        var formatted = TestFormatter.Format("""<r a="1"/>""", TestOptions.NoDeclaration);

        Assert.Equal("""<r a="1" />""", formatted);
    }

    [Fact]
    public void Exceeding_the_threshold_puts_each_attribute_on_its_own_line()
    {
        var formatted = TestFormatter.Format(ThreeAttributes, TestOptions.NoDeclaration);

        Assert.Equal("""
            <r a="1"
               b="2"
               c="3" />
            """, formatted);
    }

    [Fact]
    public void Raising_the_threshold_to_the_attribute_count_keeps_them_inline()
    {
        var formatted = TestFormatter.Format(ThreeAttributes,
                                             TestOptions.NoDeclaration with { AttributesInNewlineThreshold = 3 });

        Assert.Equal("""<r a="1" b="2" c="3" />""", formatted);
    }

    [Fact]
    public void The_threshold_is_inclusive()
    {
        // Two attributes at a threshold of two stay inline; the wrap starts at three.
        var inline = TestFormatter.Format("""<r a="1" b="2"/>""",
                                          TestOptions.NoDeclaration with { AttributesInNewlineThreshold = 2 });

        Assert.Equal("""<r a="1" b="2" />""", inline);
    }

    [Fact]
    public void Zero_cannot_wrap_a_lone_attribute()
    {
        // The line break is written *after* each attribute except the last, so an element with
        // one attribute has nowhere to wrap however low the threshold goes.
        var formatted = TestFormatter.Format("""<r a="1"/>""",
                                             TestOptions.NoDeclaration with { AttributesInNewlineThreshold = 0 });

        Assert.Equal("""<r a="1" />""", formatted);
    }
}
