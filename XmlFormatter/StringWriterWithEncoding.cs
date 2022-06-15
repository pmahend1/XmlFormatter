using System.Text;

namespace XmlFormatter
{
    public sealed class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        public StringWriterWithEncoding()
        {
            _encoding = Encoding.UTF8;
        }

        public StringWriterWithEncoding(Encoding encoding)
        {
            if (encoding == null)
                _encoding = Encoding.UTF8;
            else
                _encoding = encoding;
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }
    }
}