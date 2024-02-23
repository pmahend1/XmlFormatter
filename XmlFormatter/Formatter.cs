using System.Diagnostics;
using System.Security;
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

        private static XmlDocument ConvertToXMLDocument(string input)
        {
            XmlDocument xml = new();
            xml.LoadXml(input);
            return xml;
        }

        public string Format(string input, Options? formattingOptions = null)
        {
            try
            {
                if (formattingOptions != null)
                {
                    currentOptions = formattingOptions;

                    if (formattingOptions.UseSingleQuotes)
                    {
                        currentOptions.AllowSingleQuoteInAttributeValue = false;
                    }
                }
                var xmlDocument = ConvertToXMLDocument(input);
                var formattedXML = FormatXMLDocument(xmlDocument);
                return formattedXML;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private string FormatXMLDocument(XmlDocument xml)
        {
            var sb = new StringBuilder();

            XmlDeclaration? declaration = xml.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault();

            if (declaration != null)
            {
                lastNodeType = XmlNodeType.XmlDeclaration;
                string? xmlDeclaration;
                if (currentOptions.AddSpaceBeforeEndOfXmlDeclaration)
                {
                    xmlDeclaration = $@"<?xml {declaration.InnerText.Trim()} ?>{Environment.NewLine}";
                }
                else
                {
                    xmlDeclaration = declaration.OuterXml + Constants.Newline;
                }
                sb.Append(xmlDeclaration);
            }

            for (int i = 0; i < xml.ChildNodes.Count; i++)
            {
                var node = xml.ChildNodes.Item(i);
                if (node is XmlNode xmlNode)
                {
                    if (xmlNode.NodeType == XmlNodeType.XmlDeclaration)
                    {
                        continue;
                    }
                    if (xmlNode.NodeType == XmlNodeType.DocumentType && xml.DocumentType != null)
                    {
                        var docTypeText = $"<!DOCTYPE {xml.DocumentType.Name}";

                        if (xml.DocumentType.Entities != null && xml.DocumentType.Entities.Count > 0)
                        {
                            var newLineOrEmpty = $"{(xml.DocumentType.Entities.Count > 1 ? Environment.NewLine : "")}";
                            var tabOrEmpty = $"{(xml.DocumentType.Entities.Count > 1 ? new string(' ', currentOptions.IndentLength) : "")}";
                            docTypeText += $" [{newLineOrEmpty}";

                            for (int j = 0; j < xml.DocumentType.Entities.Count; j++)
                            {
                                var entity = xml.DocumentType.Entities.Item(j);
                                if (entity != null)
                                {
                                    docTypeText += $"{tabOrEmpty}<!ENTITY {entity.Name} \"{entity.InnerText}\">{newLineOrEmpty}";
                                }
                            }
                            docTypeText += $"]";
                        }

                        if (xml.DocumentType.PublicId != null)
                        {
                            docTypeText += $" PUBLIC \"{xml.DocumentType.PublicId}\"";
                        }

                        if (xml.DocumentType.SystemId != null)
                        {
                            docTypeText += $" \"{xml.DocumentType.SystemId}\"";
                        }

                        docTypeText += ">";

                        Debug.WriteLine($"DOCTYPE text: {docTypeText}");
                        sb.AppendLine(docTypeText);
                        continue;
                    }
                    if (xmlNode.NodeType == XmlNodeType.Element)
                    {
                        if (xmlNode is XmlElement documentElement)
                        {
                            lastNodeType = XmlNodeType.Document;
                            PrintNode(documentElement, ref sb);
                            if (xmlNode.NextSibling != null)
                            {
                                sb.Append(Environment.NewLine);
                            }

                        }
                    }
                    else if (xmlNode.NodeType == XmlNodeType.Comment)
                    {
                        PrintNode(xmlNode, ref sb);
                        sb.Append(Environment.NewLine);
                    }
                    else
                    {
                        PrintNode(xmlNode, ref sb);
                    }
                }
            }
            return sb.ToString();
        }

        private void PrintNode(XmlNode node, ref StringBuilder sb)
        {
            var prevNode = lastNodeType;
            lastNodeType = node.NodeType;

            switch (node.NodeType)
            {
                case XmlNodeType.Attribute:
                    //down
                    break;

                case XmlNodeType.CDATA:
                    var newLine = (prevNode == XmlNodeType.Text || prevNode == XmlNodeType.Element) ? string.Empty : Environment.NewLine;
                    var spaces = (prevNode == XmlNodeType.Text || prevNode == XmlNodeType.Element) ? string.Empty : new string(Constants.Space, currentStartLength);
                    Debug.WriteLine($"CDATA value: {node.Value}");
                    sb.Append(newLine
                              + spaces
                              + Constants.CDataStart
                              + node.Value
                              + Constants.CDataEnd);
                    return;

                case XmlNodeType.Comment:
                    if (currentOptions.PreserveWhiteSpacesInComment)
                    {
                        if (node.ParentNode?.NodeType == XmlNodeType.Document)
                        {
                            sb.Append(node.OuterXml);
                        }
                        else
                        {
                            sb.Append(new string(Constants.Space, currentStartLength) + node.OuterXml);
                        }

                    }
                    else if (currentOptions.WrapCommentTextWithSpaces)
                    {
                        if (node.ParentNode?.NodeType == XmlNodeType.Document)
                        {
                            sb.Append(Constants.CommentTagStart
                                        + Constants.Space
                                        + node.Value?.Trim()
                                        + Constants.Space
                                        + Constants.CommentTagEnd);
                        }
                        else
                        {
                            sb.Append(new string(Constants.Space, currentStartLength)
                                      + Constants.CommentTagStart
                                      + Constants.Space
                                      + node.Value?.Trim()
                                      + Constants.Space
                                      + Constants.CommentTagEnd);
                        }
                    }
                    else
                    {
                        if (node.ParentNode?.NodeType == XmlNodeType.Document)
                        {
                            sb.Append($@"<!--{node.Value?.Trim()}-->");
                        }
                        else
                        {
                            sb.Append(new string(Constants.Space, currentStartLength)
                                      + Constants.CommentTagStart
                                      + node.Value?.Trim()
                                      + Constants.CommentTagEnd);
                        }
                    }

                    return;

                case XmlNodeType.Document:
                    //done
                    break;

                case XmlNodeType.DocumentFragment:
                    //done
                    break;

                case XmlNodeType.DocumentType:
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
                    return;

                case XmlNodeType.Text:
                    if (!node.OuterXml.Contains(Environment.NewLine))
                    {
                        sb.Append(node.OuterXml);
                    }
                    else
                    {
                        var text = node.OuterXml;
                        var lines = text.Split(Environment.NewLine);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            var line = lines[i];
                            if (i == 0 && string.IsNullOrEmpty(line.Trim()))
                            {
                                sb.Append($"{new string(' ', currentOptions.IndentLength + currentStartLength)}{line.Trim()}");
                            }
                            else if (i == lines.Length - 1)
                            {
                                if (!string.IsNullOrEmpty(line.Trim()))
                                {
                                    sb.Append($"{Environment.NewLine}{new string(' ', currentOptions.IndentLength + currentStartLength)}{line.Trim()}");
                                }
                                sb.Append($"{Environment.NewLine}{new string(' ', currentStartLength)}");
                            }
                            else
                            {
                                sb.Append($"{Environment.NewLine}{new string(' ', currentOptions.IndentLength + currentStartLength)}{line.Trim()}");
                            }
                        }
                    }
                    return;

                case XmlNodeType.Whitespace:
                    return;

                case XmlNodeType.XmlDeclaration:
                    //done
                    break;

                default:
                    break;
            }

            //print start tag
            var space = prevNode != XmlNodeType.Text ? new string(Constants.Space, currentStartLength) : string.Empty;

            sb.Append(space + Constants.StartTagStart + node.Name);

            //print attributes
            if (node.Attributes?.Count > 0)
            {
                if (currentOptions.PositionAllAttributesOnFirstLine)
                {
                    sb.Append(Constants.Space);
                }
                else
                {
                    if (currentOptions.PositionFirstAttributeOnSameLine)
                    {
                        sb.Append(Constants.Space);
                        currentAttributeSpace = currentStartLength + node.Name.Length + 2;// 2 is not indent length here.It is = lengthOf(<)+ lengthOf(>)
                    }
                    else
                    {
                        sb.Append(Environment.NewLine);
                        currentAttributeSpace = currentStartLength + currentOptions.IndentLength;
                        sb.Append(new string(Constants.Space, currentAttributeSpace));
                    }
                }

                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    var attribute = node.Attributes[i];
                    var isLast = i == (node.Attributes.Count - 1);
                    var newline = isLast ? string.Empty : currentOptions.PositionAllAttributesOnFirstLine ? " " : Environment.NewLine;

                    var attributeValue = SecurityElement.Escape(attribute.Value);

                    if (currentOptions.AllowWhiteSpaceUnicodesInAttributeValues)
                    {
                        if (attributeValue.Contains("\n"))
                        {
                            attributeValue = attributeValue.Replace("\n", "&#xA;");
                        }

                        if (attributeValue.Contains("\t"))
                        {
                            attributeValue = attributeValue.Replace("\t", "&#x9;");
                        }

                        if (attributeValue.Contains("&gt;"))
                        {
                            attributeValue = attributeValue.Replace("&gt;", ">");
                        }
                    }

                    if (currentOptions.AllowSingleQuoteInAttributeValue && attributeValue.Contains(Constants.Apos))
                    {
                        attributeValue = attributeValue.Replace(Constants.Apos, "'");
                    }
                    sb.Append(attribute.Name
                              + (currentOptions.UseSingleQuotes ? Constants.AssignmentStartSingleQuote : Constants.AssignmentStart)
                              + attributeValue
                              + (currentOptions.UseSingleQuotes ? Constants.AssignmentEndSingleQuote : Constants.AssignmentEnd)
                              + newline);

                    //continue
                    if (!isLast)
                    {
                        if (!currentOptions.PositionAllAttributesOnFirstLine)
                        {
                            sb.Append(new string(Constants.Space, currentAttributeSpace));
                        }
                    }
                    //start tag end if last tag
                    else if (node.HasChildNodes)
                    {
                        sb.Append('>');
                    }
                    //else see NoChildEndTag
                }
            }
            //No attributes
            else
            {
                //start tag end if no attributes
                if (node.HasChildNodes)
                    sb.Append('>');
                //else see NoChildEndTag
            }

            //prints child nodes
            if (node.HasChildNodes)
            {
                if (node.FirstChild is XmlNode firstChild && (firstChild.NodeType is not (XmlNodeType.Text or XmlNodeType.CDATA)))
                {
                    currentStartLength += currentOptions.IndentLength;
                }

                for (int j = 0; j < node.ChildNodes.Count; j++)
                {
                    var currentChild = node.ChildNodes[j];
                    if (currentChild is not null)
                    {
                        if (currentChild.NodeType != XmlNodeType.Text
                            && currentChild.NodeType != XmlNodeType.CDATA
                            && currentChild.NodeType != XmlNodeType.EntityReference
                            && lastNodeType != XmlNodeType.Text
                            && currentChild.NodeType != XmlNodeType.SignificantWhitespace
                            && currentChild.NodeType != XmlNodeType.Whitespace)
                        {
                            sb.Append(Constants.Newline);
                        }
                        PrintNode(currentChild, ref sb);
                    }
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
                        && lastNodeType != XmlNodeType.EntityReference)
                    {
                        currentStartLength -= currentOptions.IndentLength;
                    }
                    var newLine = (lastNodeType != XmlNodeType.Text
                                   && lastNodeType != XmlNodeType.CDATA
                                   && lastNodeType != XmlNodeType.EntityReference) ? Constants.Newline : string.Empty;
                    var spaces = (lastNodeType != XmlNodeType.Text
                                  && lastNodeType != XmlNodeType.EntityReference
                                  && lastNodeType != XmlNodeType.CDATA) ? new string(Constants.Space, currentStartLength) : string.Empty;
                    sb.Append(newLine
                              + spaces
                              + Constants.EndTagStart
                              + node.Name
                              + Constants.EndTagEnd);

                    lastNodeType = node.NodeType;
                }

                Debug.WriteLine(node.Name + " with value " + node.Value);
            }
            //if no childs endtag
            #region NoChildEndTag

            else if (currentOptions.UseSelfClosingTags)
            {

                if (currentOptions.AddSpaceBeforeSelfClosingTag)
                {
                    sb.Append(Constants.Space + Constants.InlineEndTag);
                }
                else
                {
                    sb.Append(Constants.InlineEndTag);
                }
            }
            else
            {
                sb.AppendFormat($"></{node.Name}>");
            }

            #endregion NoChildEndTag

            return;
        }

        public string Minimize(string xmlString)
        {
            var xmlDoc = ConvertToXMLDocument(xmlString);

            XmlDeclaration? declaration = xmlDoc.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault();

            StringWriter sw;

            if (!string.IsNullOrEmpty(declaration?.Encoding))
            {
                sw = new StringWriterWithEncoding(Encoding.GetEncoding(declaration.Encoding));
            }
            else
            {
                sw = new StringWriterWithEncoding();
            }

            var settings = new XmlWriterSettings
            {
                Indent = false,
                IndentChars = string.Empty,
                NewLineChars = string.Empty,
                NewLineHandling = NewLineHandling.Entitize,
                NewLineOnAttributes = false,
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