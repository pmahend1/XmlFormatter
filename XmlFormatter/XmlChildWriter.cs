using System;
using System.Xml;

namespace XmlFormatter
{
    public class XmlChildWriter : XmlWriter
    {

        int _CurrentSpace = 0;

        public XmlChildWriter()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
        }
        public override WriteState WriteState => this.WriteState;

        public override void Flush()
        {
            this.Dispose();
        }

        public override string LookupPrefix(string ns)
        {
            return LookupPrefix(ns);

        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
           
        }

        public override void WriteCData(string text)
        {
            WriteCData(new String(SymbolConstants.Space, _CurrentSpace) + text);
        }

        public override void WriteCharEntity(char ch)
        {
            WriteCharEntity(ch);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            WriteChars(buffer, index, count);
        }

        public override void WriteComment(string text)
        {
            this.WriteComment(new String(SymbolConstants.Space, _CurrentSpace)+ text);
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            this.WriteDocType(name, pubid, sysid, subset);
        }

        public override void WriteEndAttribute()
        {
            this.WriteEndAttribute();
        }

        public override void WriteEndDocument()
        {
            this.WriteEndDocument();
        }

        public override void WriteEndElement()
        {
            this.WriteEndElement();
        }

        public override void WriteEntityRef(string name)
        {
            this.WriteEntityRef(name);
        }

        public override void WriteFullEndElement()
        {
            WriteFullEndElement();
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            WriteProcessingInstruction(name, text);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            WriteRaw(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            WriteRaw(data);
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            WriteStartAttribute(prefix, localName, ns);
        }

        public override void WriteStartDocument()
        {
            WriteStartDocument();
        }

        public override void WriteStartDocument(bool standalone)
        {
            WriteStartDocument(standalone);
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            WriteStartElement(prefix, localName, ns);
        }

        public override void WriteString(string text)
        {
            WriteString(new string(SymbolConstants.Space, _CurrentSpace ) + text);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            WriteSurrogateCharEntity(lowChar, highChar);
        }

        public override void WriteWhitespace(string ws)
        {
            WriteWhitespace(new string(SymbolConstants.Space, _CurrentSpace));
        }
    }
}