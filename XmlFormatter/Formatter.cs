using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace XmlFormatter
{
    public class Formatter
    {
        private int currentAttributeSpace = 0;

        private int previousStartSpaceLength = 0;

        private int currentStartLength = 0;

        private XmlNodeType lastNodeType;

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


        public void FormatWithReader(StringReader stringReader)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;

            using (var reader = XmlReader.Create(stringReader, settings))
            {
                Console.WriteLine(reader.Name);
                reader.MoveToContent();
                // Parse the file and display each of the nodes.  
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            Console.Write("<{0}>", reader.Name);
                            break;
                        case XmlNodeType.Text:
                            Console.Write(reader.Value);
                            break;
                        case XmlNodeType.CDATA:
                            Console.Write("<![CDATA[{0}]]>", reader.Value);
                            break;
                        case XmlNodeType.ProcessingInstruction:
                            Console.Write("<?{0} {1}?>", reader.Name, reader.Value);
                            break;
                        case XmlNodeType.Comment:
                            Console.Write("<!--{0}-->", reader.Value);
                            break;
                        case XmlNodeType.XmlDeclaration:
                            Console.Write("<?xml version='1.0'?>");
                            break;
                        case XmlNodeType.Document:
                            break;
                        case XmlNodeType.DocumentType:
                            Console.Write("<!DOCTYPE {0} [{1}]", reader.Name, reader.Value);
                            break;
                        case XmlNodeType.EntityReference:
                            Console.Write(reader.Name);
                            break;
                        case XmlNodeType.EndElement:
                            Console.Write("</{0}>{1}", reader.Name, Environment.NewLine);
                            break;
                    }
                  
                }
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
                sb.Append(declaration.OuterXml + SymbolConstants.Newline);
            }
            var root = xml.DocumentElement;
            lastNodeType = XmlNodeType.Document;

            PrintNode(root, sb);
            sb.Append(Environment.NewLine + SymbolConstants.EndTagStart + root.Name + SymbolConstants.EndTagEnd);
            return sb.ToString();
        }

        private void PrintNode(XmlNode node, StringBuilder sb)
        {
            lastNodeType = node.NodeType;
            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                    //down
                    break;

                case XmlNodeType.CDATA:
                    sb.Append(new string(SymbolConstants.Space, currentStartLength)
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
                    break;

                case XmlNodeType.DocumentType:
                    break;

                case XmlNodeType.Element:
                    break;

                case XmlNodeType.EndElement:
                    Debug.WriteLine("End");
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
                    sb.Append(node.InnerText);
                    return;

                case XmlNodeType.Whitespace:
                    break;

                case XmlNodeType.XmlDeclaration:
                    break;

                default:
                    break;
            }

            //print start tag
            
            sb.Append(new string(SymbolConstants.Space, currentStartLength)
                + SymbolConstants.StartTagStart
                + node.Name);

            
            //print attributes
            
            if (node.Attributes?.Count > 0)
            {
                sb.Append(SymbolConstants.Space);
                previousStartSpaceLength = currentAttributeSpace;
                currentAttributeSpace = currentStartLength + node.Name.Length + 2;
                //currentAttributeSpace += new string(SymbolConstants.StartTagStart + node.Name + SymbolConstants.Space).Length + currentStartLength;
                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    var attribute = node.Attributes[i];
                    var isLast = (i == (node.Attributes.Count - 1));
                    var newline = isLast ? string.Empty : Environment.NewLine;
                    sb.Append(attribute.Name + SymbolConstants.AssignmentStart + attribute.Value + SymbolConstants.AssignmentEnd + newline);
                    if (isLast && node.HasChildNodes)
                        sb.Append(SymbolConstants.StartTagEnd);
                    else if(!isLast)
                        sb.Append(new string(SymbolConstants.Space, currentAttributeSpace));
                }
            }
            else
            {
                sb.Append(SymbolConstants.StartTagEnd);
            }
            
            //prints nodes
            if (node.HasChildNodes)
            {
                previousStartSpaceLength = currentStartLength;
                
                currentStartLength += 2;
                var temp = currentStartLength;
                for (int j = 0; j < node.ChildNodes.Count; j++)
                {
                    var currentChild = node.ChildNodes[j];
                    if (currentChild.NodeType == XmlNodeType.CDATA)
                        currentStartLength -= 2;
                    if (currentChild.NodeType != XmlNodeType.Text)
                        sb.Append(SymbolConstants.Newline);
                    if (currentChild.NodeType == XmlNodeType.DocumentFragment)
                        Debug.WriteLine("Document fragment found");

                    PrintNode(currentChild, sb);
                    //close tag

                    //if (currentChild.NodeType != XmlNodeType.Comment
                    //    && currentChild.NodeType != XmlNodeType.CDATA
                    //    && currentChild.NodeType != XmlNodeType.DocumentType
                    //    && currentChild.NodeType != XmlNodeType.Text)
                    //{
                    //    var newLine = lastNodeType != XmlNodeType.Text ? Environment.NewLine : string.Empty;
                    //    var spaces = lastNodeType != XmlNodeType.Text ? new string(SymbolConstants.Space, temp) : string.Empty;
                    //      sb.Append(newLine
                    //        + spaces
                    //        + SymbolConstants.EndTagStart
                    //        + currentChild.Name
                    //        + SymbolConstants.EndTagEnd);
                        
                    //}
                }
                if (node.NodeType != XmlNodeType.Comment
                    && node.NodeType != XmlNodeType.CDATA
                    && node.NodeType != XmlNodeType.DocumentType
                    && node.NodeType != XmlNodeType.Text)
                {
                    var newLine = lastNodeType != XmlNodeType.Text ? Environment.NewLine : string.Empty;
                    var spaces = lastNodeType != XmlNodeType.Text ? new string(SymbolConstants.Space, temp) : string.Empty;
                    sb.Append(newLine
                      + spaces
                      + SymbolConstants.EndTagStart
                      + node.Name
                      + SymbolConstants.EndTagEnd);

                }
            }
            else
            {
                    sb.Append(SymbolConstants.Space+ SymbolConstants.InlineEndTag);
            }

            return;
        }
    }
}