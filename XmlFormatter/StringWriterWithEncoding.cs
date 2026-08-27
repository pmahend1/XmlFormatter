using System.Text;

namespace XmlFormatter;

internal sealed class StringWriterWithEncoding : StringWriter
{
    public StringWriterWithEncoding()
    {
        Encoding = Encoding.UTF8;
    }

    public StringWriterWithEncoding(Encoding? encoding)
    {
        Encoding = encoding ?? Encoding.UTF8;
    }

    public override Encoding Encoding { get; }
}
