using System.Text;

namespace XmlFormatter.Tests;

/// <summary>
/// StringWriterWithEncoding exists for one reason: XmlWriter reads the encoding to declare
/// from its writer, and a plain StringWriter always reports UTF-16. Without this type every
/// document would come back declaring an encoding it does not have.
///
/// MinimizeTests covers it through the only caller. These cover the type directly, including
/// the null-encoding fallback, which nothing reaches through Minimize - Encoding.GetEncoding
/// either returns an encoding or throws.
/// </summary>
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
        // Defensive: the constructor takes a non-nullable Encoding, so this can only happen
        // from a caller that has already ignored the annotation. It still must not hand back
        // a null Encoding, because XmlWriter dereferences it.
        var writer = new StringWriterWithEncoding(null!);

        Assert.Equal(Encoding.UTF8, writer.Encoding);
    }
}
