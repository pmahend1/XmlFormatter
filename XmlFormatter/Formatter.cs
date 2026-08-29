using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace XmlFormatter;

/// <summary>
/// Formats and minimizes XML documents.
///
/// <b>Not thread-safe</b> - an instance holds the in-progress format in its fields, so sharing
/// one across threads corrupts output silently. Sequential reuse is fine, including after a
/// throw. Construction is cheap.
/// </summary>
public class Formatter
{
    private int _currentAttributeSpace;

    private int _currentStartLength;

    private XmlNodeType _lastNodeType;

    private Options _currentOptions = new();

    private static readonly XmlWriterSettings MinimizeSettings = new()
    {
        Indent = false,
        IndentChars = string.Empty,
        NewLineChars = string.Empty,
        NewLineHandling = NewLineHandling.Entitize,
        NewLineOnAttributes = false,
        NamespaceHandling = NamespaceHandling.OmitDuplicates,
    };

    // Without this, Minimize throws ArgumentException on windows-1252 and other code pages -
    // .NET Core dropped them from the default provider. No package needed on net10.0.
    static Formatter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Single-pass XML attribute value escaper. Encodes &amp; &lt; &gt; and whichever quote
    /// delimits the value; with <paramref name="escapeWhitespace"/>, also encodes tab, newline
    /// and carriage return as hex character references. Everything else is written as itself.
    /// O(n), no string scans.
    /// </summary>
    /// <param name="value">input</param>
    /// <param name="escapeWhitespace">
    /// XML attribute-value normalization (XML 1.0 section 3.3.3) replaces a literal tab, newline
    /// or carriage return in an attribute value with a space when the document is read back, so
    /// those three survive a round-trip only as character references.
    /// </param>
    /// <param name="useSingleQuotes">flag to use single quotes</param>
    private static string EscapeXmlValue(string value, bool escapeWhitespace, bool useSingleQuotes)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '&':
                    sb.Append("&amp;");
                    break;
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                case '"' when !useSingleQuotes:
                    sb.Append("&quot;");
                    break;
                case '\'' when useSingleQuotes:
                    sb.Append("&apos;");
                    break;
                default:
                    if (escapeWhitespace && c is '\t' or '\n' or '\r')
                    {
                        sb.Append("&#x");
                        sb.Append(((int)c).ToString("X"));
                        sb.Append(';');
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private static XmlDocument ConvertToXmlDocument(string input, bool preserveNewLines = false)
    {
        XmlDocument xml = new()
        {
            PreserveWhitespace = preserveNewLines
        };
        xml.LoadXml(input);
        return xml;
    }

    public string Format(string input, Options? formattingOptions = null)
    {
        try
        {
            if (formattingOptions.HasValue)
            {
                // Single quotes and a literal apostrophe cannot both hold - well-formedness wins.
                _currentOptions = formattingOptions.Value.UseSingleQuotes ?
                                  formattingOptions.Value with { AllowSingleQuoteInAttributeValue = false } :
                                  formattingOptions.Value;
            }
            var xmlDocument = ConvertToXmlDocument(input: input, preserveNewLines: _currentOptions.PreserveNewLines);
            var formattedXml = FormatXmlDocument(xmlDocument);
            return formattedXml;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.StackTrace);
            throw;
        }
    }

    private string FormatXmlDocument(XmlDocument xml)
    {
        var sb = new StringBuilder();

        var declaration = xml.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault();

        if (declaration is not null)
        {
            _lastNodeType = XmlNodeType.XmlDeclaration;
            string? xmlDeclaration;
            if (_currentOptions.AddSpaceBeforeEndOfXmlDeclaration)
            {
                xmlDeclaration = $"<?xml {declaration.InnerText.Trim()} ?>{Environment.NewLine}";
            }
            else
            {
                xmlDeclaration = declaration.OuterXml + Environment.NewLine;
            }
            sb.Append(xmlDeclaration);
        }
        else if (_currentOptions.AddXmlDeclarationIfMissing)
        {
            sb.AppendLine("""<?xml version="1.0" encoding="UTF-8" ?>""");
        }

        for (var i = 0; i < xml.ChildNodes.Count; i++)
        {
            var node = xml.ChildNodes.Item(i);
            if (node is null || node.NodeType is XmlNodeType.XmlDeclaration)
            {
                continue;
            }

            switch (node.NodeType)
            {
                case XmlNodeType.DocumentType when xml.DocumentType is not null:
                    {
                        var docTypeText = $"<!DOCTYPE {xml.DocumentType.Name}";

                        if (xml.DocumentType.Entities is { Count: > 0 })
                        {
                            var newLineOrEmpty = $"{(xml.DocumentType.Entities.Count > 1 ? Environment.NewLine : "")}";
                            var tabOrEmpty = $"{(xml.DocumentType.Entities.Count > 1 ? new string(' ', _currentOptions.IndentLength) : "")}";
                            docTypeText += $" [{newLineOrEmpty}";

                            for (var j = 0; j < xml.DocumentType.Entities.Count; j++)
                            {
                                var entity = xml.DocumentType.Entities.Item(j);
                                if (entity != null)
                                {
                                    docTypeText += $"{tabOrEmpty}<!ENTITY {entity.Name} \"{entity.InnerText}\">{newLineOrEmpty}";
                                }
                            }
                            docTypeText += $"]";
                        }

                        if (xml.DocumentType.PublicId is not null)
                        {
                            docTypeText += $" PUBLIC \"{xml.DocumentType.PublicId}\"";
                        }

                        if (xml.DocumentType.SystemId is not null)
                        {
                            docTypeText += $" \"{xml.DocumentType.SystemId}\"";
                        }

                        docTypeText += ">";

                        Debug.WriteLine($"DOCTYPE text: {docTypeText}");
                        sb.AppendLine(docTypeText);
                        continue;
                    }
                case XmlNodeType.Element:
                    {
                        if (node is not XmlElement documentElement)
                        {
                            continue;
                        }

                        _lastNodeType = XmlNodeType.Document;
                        PrintNode(documentElement, ref sb);
                        if (node.NextSibling != null)
                        {
                            sb.Append(Environment.NewLine);
                        }

                        break;
                    }
                case XmlNodeType.Comment:
                    PrintNode(node, ref sb);
                    sb.Append(Environment.NewLine);
                    break;
                case XmlNodeType.None:
                case XmlNodeType.Attribute:
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.EntityReference:
                case XmlNodeType.Entity:
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.Document:
                case XmlNodeType.DocumentFragment:
                case XmlNodeType.Notation:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.EndElement:
                case XmlNodeType.EndEntity:
                case XmlNodeType.XmlDeclaration:
                default:
                    PrintNode(node, ref sb);
                    break;
            }
        }
        return sb.ToString();
    }

    private void PrintNode(XmlNode node, ref StringBuilder sb)
    {
        var prevNode = _lastNodeType;
        _lastNodeType = node.NodeType;

        switch (node.NodeType)
        {
            case XmlNodeType.CDATA:
                var newLine = prevNode is XmlNodeType.Text or XmlNodeType.Element ? string.Empty : Environment.NewLine;
                var spaces = prevNode is XmlNodeType.Text or XmlNodeType.Element ? string.Empty : new string(' ', _currentStartLength);
                Debug.WriteLine($"CDATA value: {node.Value}");

                sb.Append(newLine)
                  .Append(spaces)
                  .Append($"<![CDATA[{node.Value}]]>");
                return;

            case XmlNodeType.Comment:
                var shouldIndent = true;
                if (_currentOptions.PreserveCommentPlacement)
                {
                    shouldIndent = node is
                    {
                        PreviousSibling: not null,
                        PreviousSibling.Value: not null,
                        PreviousSibling.NodeType: XmlNodeType.Whitespace
                    } && node.PreviousSibling.Value.Contains('\n');
                }
                if (shouldIndent && node.ParentNode?.NodeType is XmlNodeType.Document)
                {
                    shouldIndent = false;
                }
                if (shouldIndent && _currentOptions.PreserveCommentPlacement)
                {
                    sb.AppendLine();
                }
                var indent = shouldIndent ? new string(' ', _currentStartLength) : string.Empty;
                string commentText;
                if (_currentOptions.PreserveWhiteSpacesInComment)
                {
                    commentText = node.OuterXml;
                }
                else if (_currentOptions.WrapCommentTextWithSpaces)
                {
                    commentText = $"<!-- {node.Value?.Trim()} -->";
                }
                else
                {
                    commentText = $"<!--{node.Value?.Trim()}-->";
                }
                sb.Append(indent).Append(commentText);

                return;
            case XmlNodeType.DocumentType:
            case XmlNodeType.SignificantWhitespace:
                return;

            case XmlNodeType.EndElement:
                Debug.WriteLine("End");
                break;

            case XmlNodeType.EndEntity:
            case XmlNodeType.EntityReference:
                sb.Append(node.OuterXml);
                return;

            case XmlNodeType.ProcessingInstruction:
                sb.AppendLine($"<?{node.Name} {node.Value}?>");
                return;


            case XmlNodeType.Text:
                if ((node.ParentNode?.ParentNode is XmlElement element &&
                    element.HasAttribute("xml:space") &&
                    element.GetAttribute("xml:space") is "preserve") || node.OuterXml.Contains('\n') is false)
                {
                    sb.Append(node.OuterXml);
                }
                else
                {
                    // LF, not Environment.NewLine: parsing normalizes every line ending in text
                    // to LF (XML 1.0 2.11), so the DOM never holds CRLF. Searching for the
                    // platform newline skipped this branch on Windows and emitted raw text.
                    var text = node.OuterXml;
                    var lines = text.Split('\n');
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (i == 0 && string.IsNullOrEmpty(line.Trim()))
                        {
                            sb.Append($"{new string(' ', _currentOptions.IndentLength + _currentStartLength)}{line.Trim()}");
                        }
                        else if (i == lines.Length - 1)
                        {
                            if (string.IsNullOrEmpty(line.Trim()) is false)
                            {
                                sb.Append($"{Environment.NewLine}{new string(' ', _currentOptions.IndentLength + _currentStartLength)}{line.Trim()}");
                            }
                            sb.Append($"{Environment.NewLine}{new string(' ', _currentStartLength)}");
                        }
                        else
                        {
                            sb.Append($"{Environment.NewLine}{new string(' ', _currentOptions.IndentLength + _currentStartLength)}{line.Trim()}");
                        }
                    }
                }

                return;
            case XmlNodeType.Whitespace:
                if (_currentOptions.PreserveNewLines is false || string.IsNullOrEmpty(node.Value))
                {
                    return;
                }

                /*
                 * Only emit whitespace that is actual element content (no element siblings),
                 * not structural indentation between siblings (which is regenerated). See #209.
                 */
                var hasElementSibling = node.PreviousSibling is { NodeType: XmlNodeType.Element }
                                        || node.NextSibling is { NodeType: XmlNodeType.Element };

                if (hasElementSibling)
                {
                    return;
                }

                if (node.Value.Contains('\n') is false)
                {
                    // Inline whitespace — signal Text so closing tag stays inline
                    sb.Append(node.Value);
                    _lastNodeType = XmlNodeType.Text;
                }
                else
                {
                    // Collapse newline runs, then append
                    var collapsed = Regex.Replace(node.Value, @"(\r?\n)+", Environment.NewLine);
                    sb.Append(collapsed);
                }
                return;
            case XmlNodeType.Element: //Done
            case XmlNodeType.None:
            case XmlNodeType.Notation:
            case XmlNodeType.XmlDeclaration: //Done
            case XmlNodeType.Document: //Done
            case XmlNodeType.DocumentFragment: //Done
            case XmlNodeType.Entity:
            case XmlNodeType.Attribute:  //handled down
            default:
                break;
        }

        //print start tag
        var space = prevNode is not XmlNodeType.Text ? new string(' ', _currentStartLength) : string.Empty;

        sb.Append(space).Append($"<{node.Name}");

        // Default is null
        var wildCardPatterns = _currentOptions.WildCardedExceptionsForPositionAllAttributesOnFirstLine ?? [];

        var wildCardExceptionForAllAttributesOnFirstLineExist = wildCardPatterns.Any(pattern => Regex.IsMatch(node.Name, pattern));
        var shouldAttributesSeparatedBySpace = _currentOptions.PositionAllAttributesOnFirstLine
                                               && (wildCardPatterns.Count is 0 || wildCardExceptionForAllAttributesOnFirstLineExist is false);
        //print attributes
        if (node.Attributes?.Count > 0)
        {
            if (shouldAttributesSeparatedBySpace)
            {
                sb.Append(' ');
            }
            else
            {
                if (_currentOptions.PositionFirstAttributeOnSameLine)
                {
                    sb.Append(' ');
                    if (node.Attributes.Count > _currentOptions.AttributesInNewlineThreshold)
                    {
                        _currentAttributeSpace = _currentStartLength + node.Name.Length + 2;// 2 is not indent length here.It is = lengthOf(<)+ lengthOf(>)
                    }
                }
                else
                {
                    sb.AppendLine();
                    _currentAttributeSpace = _currentStartLength + _currentOptions.IndentLength;
                    sb.Append(new string(' ', _currentAttributeSpace));
                }
            }

            var isThresholdApplicable = _currentOptions.PositionFirstAttributeOnSameLine && node.Attributes.Count <= _currentOptions.AttributesInNewlineThreshold;
            for (var i = 0; i < node.Attributes.Count; i++)
            {
                var attribute = node.Attributes[i];
                var isLast = i == node.Attributes.Count - 1;

                var newLineOrSpace = isLast ?
                                     string.Empty :
                                     shouldAttributesSeparatedBySpace || isThresholdApplicable ? " " : Environment.NewLine;

                var attributeValue = EscapeXmlValue(attribute.Value,
                                                    escapeWhitespace: _currentOptions.AllowWhiteSpaceUnicodesInAttributeValues,
                                                    useSingleQuotes: _currentOptions.UseSingleQuotes);

                if (_currentOptions.AllowSingleQuoteInAttributeValue && attributeValue.Contains("&apos;"))
                {
                    attributeValue = attributeValue.Replace("&apos;", "'");
                }
                sb.Append($"{attribute.Name}{(_currentOptions.UseSingleQuotes ? "='" : "=\"")}{attributeValue}{(_currentOptions.UseSingleQuotes ? '\'' : "\"")}{newLineOrSpace}");

                //continue
                if (isLast is false)
                {
                    if (shouldAttributesSeparatedBySpace is false && isThresholdApplicable is false)
                    {
                        sb.Append(new string(' ', _currentAttributeSpace));
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
            {
                sb.Append('>');
            }
            //else see NoChildEndTag
        }

        //prints child nodes
        if (node.HasChildNodes)
        {
            // Treat inline whitespace content like Text for indentation (see #209)
            var firstChildIsInlineContent =
                node.FirstChild is { NodeType: XmlNodeType.Text or XmlNodeType.CDATA }
                || (_currentOptions.PreserveNewLines
                    && node.FirstChild == node.LastChild
                    && node.FirstChild is { NodeType: XmlNodeType.Whitespace, Value: not null } firstWs
                    && !firstWs.Value.Contains('\n'));

            if (!firstChildIsInlineContent)
            {
                _currentStartLength += _currentOptions.IndentLength;
            }

            var childCount = _currentOptions.AddEmptyLineBetweenElements ? node.ChildNodes.Count : 0;

            for (var currentChild = node.FirstChild; currentChild is not null; currentChild = currentChild.NextSibling)
            {
                var commentNoNewLine = currentChild.NodeType is XmlNodeType.Comment
                                       && _currentOptions.PreserveCommentPlacement
                                       && currentChild.PreviousSibling?.NodeType is XmlNodeType.Element or XmlNodeType.Whitespace;
                if (currentChild.NodeType is not (XmlNodeType.Text or XmlNodeType.CDATA
                                         or XmlNodeType.EntityReference
                                         or XmlNodeType.SignificantWhitespace
                                         or XmlNodeType.Whitespace)
                    && _lastNodeType is not XmlNodeType.Text
                    && commentNoNewLine is false)
                {
                    sb.Append(Environment.NewLine);
                }
                PrintNode(currentChild, ref sb);

                if (_currentOptions.AddEmptyLineBetweenElements
                    && currentChild.NodeType is XmlNodeType.Element
                    && currentChild.NextSibling?.NodeType is not (XmlNodeType.Text or XmlNodeType.SignificantWhitespace)
                    && childCount > 2
                    && currentChild.NextSibling is not null)
                {
                    sb.AppendLine();
                }
            }

            //close tag after all child nodes
            if (node.NodeType is XmlNodeType.Comment or
                                 XmlNodeType.CDATA or
                                 XmlNodeType.DocumentType or
                                 XmlNodeType.Text)
            {
                return;
            }

            if (_currentStartLength >= _currentOptions.IndentLength &&
                _lastNodeType is not (XmlNodeType.Text or
                                      XmlNodeType.CDATA or
                                      XmlNodeType.DocumentType or
                                      XmlNodeType.EntityReference))
            {
                _currentStartLength -= _currentOptions.IndentLength;
            }
            var newLine = _lastNodeType is not (XmlNodeType.Text or
                                                XmlNodeType.CDATA or
                                                XmlNodeType.EntityReference) ? Environment.NewLine : string.Empty;

            var spaces = _lastNodeType is not (XmlNodeType.Text or
                                               XmlNodeType.EntityReference or
                                               XmlNodeType.CDATA) ? new string(' ', _currentStartLength) : string.Empty;
            sb.Append(newLine)
                .Append(spaces)
                .Append($"</{node.Name}>");

            _lastNodeType = node.NodeType;
        }
        //if no children end tag
        #region NoChildEndTag

        else if (_currentOptions.UseSelfClosingTags)
        {
            sb.Append(_currentOptions.AddSpaceBeforeSelfClosingTag ? " />" : "/>");
        }
        else
        {
            sb.Append($"></{node.Name}>");
        }

        #endregion NoChildEndTag
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
    public string Minimize(string xmlString)
    {
        var xmlDoc = ConvertToXmlDocument(xmlString);

        // ReSharper disable once SuggestVarOrType_SimpleTypes
        var declaration = xmlDoc.ChildNodes.OfType<XmlDeclaration>().FirstOrDefault();

        var stringWriter = string.IsNullOrEmpty(declaration?.Encoding) is false ?
            new StringWriterWithEncoding(Encoding.GetEncoding(declaration.Encoding)) :
            new StringWriterWithEncoding();

        using var writer = XmlWriter.Create(stringWriter, MinimizeSettings);
        xmlDoc.Save(writer);

        return stringWriter.ToString();
    }
}
