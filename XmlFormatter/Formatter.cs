using System;
using System.Linq;
using System.Text;
using System.Xml;

namespace XmlFormatter
{
    public class Formatter
    {
        private int firstLength = 0;

        private bool isStart = true;

        private XmlDocument ConvertToXMLDocument(string input)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(input);

            return xml;
        }

        public string Format(string inputString)
        {
            var xml = ConvertToXMLDocument(inputString);
            var formattedXML = FormatXMLDocument(xml);
            return formattedXML;
        }

        private string FormatXMLDocument(XmlDocument xml)
        {
            StringBuilder sb = new StringBuilder();
            XmlDeclaration declaration = xml.ChildNodes
                                .OfType<XmlDeclaration>()
                                .FirstOrDefault();
            if (declaration != null)
            {
                sb.Append(declaration.OuterXml + SymbolConstants.Newline);
            }
            var root = xml.DocumentElement;

            PrintNode(root, sb);
            sb.Append(Environment.NewLine + SymbolConstants.EndTagStart + root.Name + SymbolConstants.EndTagEnd);
            return sb.ToString();
        }

        private void PrintNode(XmlNode node, StringBuilder sb)
        {
            //identity if comment

            if (node.NodeType == XmlNodeType.Comment)
            {

            }

            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                    break;
                case XmlNodeType.CDATA:
                    break;
                case XmlNodeType.Comment:
                    sb.Append(new string(SymbolConstants.Space, firstLength)
                         + SymbolConstants.CommentTagStart
                         + SymbolConstants.Space
                         + node.Value
                         + SymbolConstants.Space
                         + SymbolConstants.CommentTagEnd);
                    return;
                case XmlNodeType.Document:
                    break;
                case XmlNodeType.DocumentFragment:
                    break;
                case XmlNodeType.DocumentType:
                    break;
                case XmlNodeType.Element:
                    break;
                case XmlNodeType.EndElement:
                    break;
                case XmlNodeType.EndEntity:
                    break;
                case XmlNodeType.Entity:
                    break;
                case XmlNodeType.EntityReference:
                    break;
                case XmlNodeType.None:
                    break;
                case XmlNodeType.Notation:
                    break;
                case XmlNodeType.ProcessingInstruction:
                    break;
                case XmlNodeType.SignificantWhitespace:
                    break;
                case XmlNodeType.Text:
                    break;
                case XmlNodeType.Whitespace:
                    break;
                case XmlNodeType.XmlDeclaration:
                    break;
                default:
                    break;
            }
            //print attributes
            sb.Append(new string(SymbolConstants.Space, firstLength) + SymbolConstants.StartTagStart + node.Name);

            if (node.Attributes?.Count > 0)
            {
                firstLength += new string(SymbolConstants.StartTagStart + node.Name + SymbolConstants.Space).Length;
                sb.Append(SymbolConstants.Space);

                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    var attribute = node.Attributes[i];
                    var isLast = (i == (node.Attributes.Count - 1));
                    var newline = isLast ? string.Empty : Environment.NewLine;
                    sb.Append(attribute.Name + SymbolConstants.AssignmentStart + attribute.Value + SymbolConstants.AssignmentEnd + newline);
                    if (isLast)
                        sb.Append(SymbolConstants.StartTagEnd);
                    else
                        sb.Append(new String(SymbolConstants.Space, firstLength));
                }
            }
            else
            {
                sb.Append(SymbolConstants.StartTagEnd);
            }

            //prints nodes
            if (node.HasChildNodes)
            {
                if (isStart)
                {
                    firstLength = 2;
                    isStart = false;
                }
                else
                    firstLength += 2;
                for (int j = 0; j < node.ChildNodes.Count; j++)
                {
                    sb.Append(Environment.NewLine);
                    var currentSpace = firstLength;
                    PrintNode(node.ChildNodes[j], sb);
                    //close tag
                    if (node.ChildNodes[j].NodeType != XmlNodeType.Comment)
                        sb.Append(Environment.NewLine + new string(SymbolConstants.Space, currentSpace) + SymbolConstants.EndTagStart + node.ChildNodes[j].Name + ">");
                }
            }

            return;
        }
    }
}