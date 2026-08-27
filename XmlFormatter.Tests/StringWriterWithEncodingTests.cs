using System.Text;

namespace XmlFormatter.Tests;

public class StringWriterWithEncodingTests
{
    [Fact]
    public void Reports_the_encoding_it_was_given()
    {
        var writer = new StringWriterWithEncoding(Encoding.ASCII);

        Assert.Equal(Encoding.ASCII, writer.Encoding);
    }

    [Fact]
    public void Defaults_to_utf8_when_constructed_without_one()
    {
        var writer = new StringWriterWithEncoding();

        Assert.Equal(Encoding.UTF8, writer.Encoding);
    }

    [Fact]
    public void Falls_back_to_utf8_rather_than_storing_a_null()
    {
        // Unreachable through Minimize; XmlWriter dereferences Encoding, so it must not be null.
        var writer = new StringWriterWithEncoding(null!);

        Assert.Equal(Encoding.UTF8, writer.Encoding);
    }
}
