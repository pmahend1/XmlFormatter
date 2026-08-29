using System.Xml;

namespace XmlFormatter.Tests;

/// <summary>
/// The extension displays syntax errors, which needs the formatter to throw XmlException -
/// that is what carries the line and position.
/// </summary>
public class InvalidXmlTests
{
    [Theory]
    [InlineData("<r><a></r>")]
    [InlineData("<r>")]
    [InlineData("not xml at all")]
    [InlineData("")]
    [InlineData("<r a=unquoted/>")]
    public void Format_rejects_malformed_input(string malformed)
    {
        Assert.Throws<XmlException>(() => new Formatter().Format(malformed, TestOptions.NoDeclaration));
    }

    [Theory]
    [InlineData("<r><a></r>")]
    [InlineData("not xml at all")]
    [InlineData("")]
    public void Minimize_rejects_malformed_input(string malformed)
    {
        Assert.Throws<XmlException>(() => new Formatter().Minimize(malformed));
    }

    [Fact]
    public void The_error_carries_a_position_for_the_editor_to_show()
    {
        var error = Assert.Throws<XmlException>(() => new Formatter().Format("<r><a></r>", TestOptions.NoDeclaration));

        Assert.True(error.LineNumber > 0);
        Assert.True(error.LinePosition > 0);
    }

    [Fact]
    public void A_valid_document_is_not_rejected()
    {
        // Guards the premise: if everything threw, the tests above would pass for the wrong reason.
        var formatted = TestFormatter.Format("<r><a/></r>", TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <a />\n</r>", formatted);
    }
}
