using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace XmlFormatter
{
    public class Formatter
    {
        private int currentAttributeSpace = 0;

        private int currentStartLength = 0;

        private XmlNodeType lastNodeType;

        private Options currentOptions = new Options { };

        private XmlDocument ConvertToXMLDocument(string input)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(input);

            return xml;
        }

        public string Format(string input, Options formattingOptions = null)
        {
            try
            {
                if (formattingOptions != null)
                {
                    currentOptions.IndentLength = formattingOptions.IndentLength;
                    currentOptions.UseSelfClosingTags = formattingOptions.UseSelfClosingTags;
                    currentOptions.UseSingleQuotes = formattingOptions.UseSingleQuotes;
                }

                var xmlDocument = ConvertToXMLDocument(input);
                var formattedXML = FormatXMLDocument(xmlDocument);
                return formattedXML;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
                throw ex;
            }
        }

        private string FormatXMLDocument(XmlDocument xml)
        {
            StringBuilder sb = new StringBuilder();

            XmlDeclaration declaration = xml.ChildNodes
                                .OfType<XmlDeclaration>()
                                .FirstOrDefault();
            if (declaration != null)
            {
                lastNodeType = XmlNodeType.XmlDeclaration;
                sb.Append(declaration.OuterXml + Constants.Newline);
            }
            if (xml.DocumentType != null)
            {
                sb.Append(xml.DocumentType.OuterXml + Constants.Newline);
            }
            var root = xml.DocumentElement;
            lastNodeType = XmlNodeType.Document;

            PrintNode(root, sb);
            return sb.ToString();
        }

        private void PrintNode(XmlNode node, StringBuilder sb)
        {
            var prevNode = lastNodeType;
            lastNodeType = node.NodeType;

            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                    //down
                    break;

                case XmlNodeType.CDATA:
                    var newLine = (prevNode == XmlNodeType.Text) ? string.Empty : Environment.NewLine;
                    var spaces = (prevNode == XmlNodeType.Text) ? string.Empty : new string(Constants.Space, currentStartLength);
                    sb.Append(newLine
                        + spaces
                        + Constants.CDataStart
                        + node.Value
                        + Constants.CDataEnd);
                    return;

                case XmlNodeType.Comment:
                    sb.Append(new string(Constants.Space, currentStartLength)
                         + Constants.CommentTagStart
                         + Constants.Space
                         + node.Value?.Trim()
                         + Constants.Space
                         + Constants.CommentTagEnd);
                    return;

                case XmlNodeType.Document:
                    //done
                    break;

                case XmlNodeType.DocumentFragment:
                    //done
                    break;

                case XmlNodeType.DocumentType:
                    sb.Append(Constants.DocTypeStart + Constants.Space + Constants.DocTypeEnd(node.Value));

                    return;

                case XmlNodeType.Element:
                    //Done
                    break;

                case XmlNodeType.EndElement:
                    Debug.WriteLine("End");
                    break;

                case XmlNodeType.EndEntity:
                    break;

                case XmlNodeType.Entity:
                    break;

                case XmlNodeType.EntityReference:
                    sb.Append(node.OuterXml);
                    return;

                case XmlNodeType.None:
                    break;

                case XmlNodeType.Notation:
                    break;

                case XmlNodeType.ProcessingInstruction:
                    sb.Append($"<?{node.Name} {node.Value}?>");
                    return;

                case XmlNodeType.SignificantWhitespace:
                    break;

                case XmlNodeType.Text:
                    sb.Append(node.OuterXml);
                    return;

                case XmlNodeType.Whitespace:
                    break;

                case XmlNodeType.XmlDeclaration:
                    //done
                    break;

                default:
                    break;
            }

            //print start tag
            var space = prevNode != XmlNodeType.Text ? new string(Constants.Space, currentStartLength) : string.Empty;
            sb.Append(space
                + Constants.StartTagStart
                + node.Name);

            //print attributes
            if (node.Attributes?.Count > 0)
            {
                sb.Append(Constants.Space);
                currentAttributeSpace = currentStartLength + node.Name.Length + 2;// 2 is not indent length here
                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    var attribute = node.Attributes[i];
                    var isLast = (i == (node.Attributes.Count - 1));
                    var newline = isLast ? string.Empty : Environment.NewLine;

                    sb.Append(attribute.Name + (currentOptions.UseSingleQuotes ? Constants.AssignmentStartSingleQuote : Constants.AssignmentStart) + attribute.Value +
                        (currentOptions.UseSingleQuotes ? Constants.AssignmentEndSingleQuote : Constants.AssignmentEnd) + newline);
                    if (isLast && node.HasChildNodes)
                        sb.Append(Constants.StartTagEnd);
                    else if (!isLast)
                        sb.Append(new string(Constants.Space, currentAttributeSpace));
                }
            }
            else if (!node.OuterXml.EndsWith(Constants.InlineEndTag))
            {
                sb.Append(Constants.StartTagEnd);
            }

            //prints nodes
            if (node.HasChildNodes)
            {
                if (!(node.ChildNodes.Cast<XmlNode>().First() is XmlText))
                    currentStartLength += currentOptions.IndentLength;

                for (int j = 0; j < node.ChildNodes.Count; j++)
                {
                    var currentChild = node.ChildNodes[j];
                    if (currentChild.NodeType != XmlNodeType.Text
                        && currentChild.NodeType != XmlNodeType.CDATA
                        && currentChild.NodeType != XmlNodeType.EntityReference
                        && lastNodeType != XmlNodeType.Text)
                    {
                        sb.Append(Constants.Newline);
                    }
                    //
                    PrintNode(currentChild, sb);
                }

                //close tag after all child nodes
                if (node.NodeType != XmlNodeType.Comment
                    && node.NodeType != XmlNodeType.CDATA
                    && node.NodeType != XmlNodeType.DocumentType
                    && node.NodeType != XmlNodeType.Text)
                {
                    if (currentStartLength >= currentOptions.IndentLength
                        && lastNodeType != XmlNodeType.Text
                        && lastNodeType != XmlNodeType.CDATA
                        && lastNodeType != XmlNodeType.DocumentType
                        && lastNodeType != XmlNodeType.EntityReference
                        )
                    {
                        currentStartLength -= currentOptions.IndentLength;
                    }
                    var newLine = (lastNodeType != XmlNodeType.Text &&
                        lastNodeType != XmlNodeType.CDATA
                        && lastNodeType != XmlNodeType.EntityReference) ? Constants.Newline : string.Empty;
                    var spaces = (lastNodeType != XmlNodeType.Text &&
                        lastNodeType != XmlNodeType.EntityReference) ? new string(Constants.Space, currentStartLength) : string.Empty;
                    sb.Append(newLine
                      + spaces
                      + Constants.EndTagStart
                      + node.Name
                      + Constants.EndTagEnd);
                    lastNodeType = node.NodeType;
                }

                Debug.WriteLine(node.Name + " with value " + node.Value);
            }
            else//close tag inline
            {
                if (currentOptions.UseSelfClosingTags)
                    sb.Append(Constants.Space + Constants.InlineEndTag);
                else
                    sb.AppendFormat($"></{node.Name}>");
            }

            return;
        }

        public string Beautify(string xmlString)
        {
            var xmlDoc = ConvertToXMLDocument(xmlString);
            XmlDeclaration declaration = xmlDoc.ChildNodes
                                .OfType<XmlDeclaration>()
                                .FirstOrDefault();
            StringWriter sw;

            if (declaration != null)
                sw = new StringWriterWithEncoding(Encoding.GetEncoding(declaration.Encoding));
            else
                sw = new StringWriterWithEncoding();

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = new string(Constants.Space, currentOptions.IndentLength),
                NewLineChars = Constants.Newline,
                NewLineHandling = NewLineHandling.Replace,
                NewLineOnAttributes = false,
                OmitXmlDeclaration = declaration is null,
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
            };
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                xmlDoc.Save(writer);
            }

            return sw.ToString();
        }
    }
}