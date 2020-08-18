﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace XmlFormatter
{
    public class Formatter
    {
        private int currentAttributeSpace = 0;

        private int currentStartLength = 0;

        private XmlNodeType lastNodeType;

        private XmlDocument ConvertToXMLDocument(string input)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(input);

            return xml;
        }

        public Task<object> Format(string inputString)
        {
            var xmlDocument = ConvertToXMLDocument(inputString);
            var formattedXML = FormatXMLDocument(xmlDocument);
            return Task.FromResult((object)formattedXML);
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
                sb.Append(declaration.OuterXml + SymbolConstants.Newline);
            }
            if (xml.DocumentType != null)
            {
                sb.Append(xml.DocumentType.OuterXml + SymbolConstants.Newline);
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
                    var spaces = (prevNode == XmlNodeType.Text) ? string.Empty : new string(SymbolConstants.Space, currentStartLength);
                    sb.Append(newLine
                        + spaces
                        + SymbolConstants.CDataStart
                        + node.Value
                        + SymbolConstants.CDataEnd);
                    return;

                case XmlNodeType.Comment:
                    sb.Append(new string(SymbolConstants.Space, currentStartLength)
                         + SymbolConstants.CommentTagStart
                         + SymbolConstants.Space
                         + node.Value
                         + SymbolConstants.Space
                         + SymbolConstants.CommentTagEnd);
                    return;

                case XmlNodeType.Document:
                    //done
                    break;

                case XmlNodeType.DocumentFragment:
                    //done
                    break;

                case XmlNodeType.DocumentType:
                    sb.Append(SymbolConstants.DocTypeStart + SymbolConstants.Space + SymbolConstants.DocTypeEnd(node.Value));

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
            var space = prevNode != XmlNodeType.Text ? new string(SymbolConstants.Space, currentStartLength) : string.Empty;
            sb.Append(space
                + SymbolConstants.StartTagStart
                + node.Name);

            //print attributes
            if (node.Attributes?.Count > 0)
            {
                sb.Append(SymbolConstants.Space);
                currentAttributeSpace = currentStartLength + node.Name.Length + 2;
                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    var attribute = node.Attributes[i];
                    var isLast = (i == (node.Attributes.Count - 1));
                    var newline = isLast ? string.Empty : Environment.NewLine;
                    sb.Append(attribute.Name + SymbolConstants.AssignmentStart + attribute.Value + SymbolConstants.AssignmentEnd + newline);
                    if (isLast && node.HasChildNodes)
                        sb.Append(SymbolConstants.StartTagEnd);
                    else if (!isLast)
                        sb.Append(new string(SymbolConstants.Space, currentAttributeSpace));
                }
            }
            else if (!node.OuterXml.EndsWith("/>"))
            {
                sb.Append(SymbolConstants.StartTagEnd);
            }

            //prints nodes
            if (node.HasChildNodes)
            {
                int prevStartLength = currentStartLength;
                if (!(node.ChildNodes.Cast<XmlNode>().First() is XmlText))
                {
                    prevStartLength = currentStartLength;
                    currentStartLength += 2;
                }

                for (int j = 0; j < node.ChildNodes.Count; j++)
                {
                    var currentChild = node.ChildNodes[j];
                    if (currentChild.NodeType != XmlNodeType.Text
                        && currentChild.NodeType != XmlNodeType.CDATA
                        && currentChild.NodeType != XmlNodeType.EntityReference
                        && lastNodeType != XmlNodeType.Text)
                    {
                        sb.Append(SymbolConstants.Newline);
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
                    if (currentStartLength >= 2
                        && lastNodeType != XmlNodeType.Text
                        && lastNodeType != XmlNodeType.CDATA
                        && lastNodeType != XmlNodeType.DocumentType
                        && lastNodeType != XmlNodeType.EntityReference
                        )
                    {
                        currentStartLength -= 2;
                    }
                    var newLine = (lastNodeType != XmlNodeType.Text &&
                        lastNodeType != XmlNodeType.CDATA
                        && lastNodeType != XmlNodeType.EntityReference) ? Environment.NewLine : string.Empty;
                    var spaces = (lastNodeType != XmlNodeType.Text &&
                        lastNodeType != XmlNodeType.EntityReference) ? new string(SymbolConstants.Space, currentStartLength) : string.Empty;
                    sb.Append(newLine
                      + spaces
                      + SymbolConstants.EndTagStart
                      + node.Name
                      + SymbolConstants.EndTagEnd);
                    lastNodeType = node.NodeType;
                }

                Debug.WriteLine(node.Name + " with value " + node.Value);
            }
            else//close tag inline
            {
                sb.Append(SymbolConstants.Space + SymbolConstants.InlineEndTag);
            }

            return;
        }

        public string Beautify(string xmlString)
        {
            var xmlDoc = ConvertToXMLDocument(xmlString);
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = new string(SymbolConstants.Space, 1),
                NewLineChars = SymbolConstants.Newline,
                NewLineHandling = NewLineHandling.Replace,
                NewLineOnAttributes = true,
                OmitXmlDeclaration = false,
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                xmlDoc.Save(writer);
            }

            return sb.ToString();
        }
    }
}