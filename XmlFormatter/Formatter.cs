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
public partial class Formatter
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

    /// <summary>Runs of one or more line breaks. Built at compile time, not on first use.</summary>
    [GeneratedRegex(@"(\r?\n)+")]
    private static partial Regex NewLineRuns();

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
            /*
             * Built through the same rule as a declaration that was already present. On a second
             * format the injected one *is* present, so it takes the branch above - and if the two
             * branches disagree about the space before ?>, formatting twice changes the output.
             */
            _lastNodeType = XmlNodeType.XmlDeclaration;
            var declarationEnd = _currentOptions.AddSpaceBeforeEndOfXmlDeclaration ? " ?>" : "?>";
            sb.Append($"""<?xml version="1.0" encoding="UTF-8"{declarationEnd}""")
              .Append(Environment.NewLine);
        }

        XmlNode? previousSibling = null;

        for (var node = xml.FirstChild; node is not null; previousSibling = node, node = node.NextSibling)
        {
            if (node.NodeType is XmlNodeType.XmlDeclaration)
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
                        PrintNode(documentElement, sb, previousSibling);
                        if (node.NextSibling != null)
                        {
                            sb.Append(Environment.NewLine);
                        }

                        break;
                    }
                case XmlNodeType.Comment:
                    PrintNode(node, sb, previousSibling);
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
                    PrintNode(node, sb, previousSibling);
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>Writes <paramref name="node"/> and everything below it.</summary>
    /// <param name="previousSibling">
    /// The node before <paramref name="node"/> under the same parent, or null when it is first.
    /// </param>
    private void PrintNode(XmlNode node, StringBuilder sb, XmlNode? previousSibling)
    {
        /*
         * An explicit stack, not recursion. This used to call itself once per nesting level,
         * and at roughly a kilobyte of frame per level a few hundred levels exhausted the
         * ~1 MB stack a thread-pool thread gets - which is the stack XmlFormatter.CommandLine
         * runs on, because it resumes there after awaiting stdin. That was a hard stack
         * overflow rather than a catchable exception, so the process died with no output and
         * no error for the caller to report. Depth is now bounded by the heap.
         */
        var openElements = new Stack<OpenElement>();

        var rootElement = WriteNode(node, sb, previousSibling);
        if (rootElement is null)
        {
            return;
        }
        openElements.Push(rootElement);

        while (openElements.Count > 0)
        {
            var element = openElements.Peek();

            if (element.LastWrittenChild is { } lastWrittenChild)
            {
                WriteBlankLineAfterChild(lastWrittenChild, sb, element.ChildCount);
            }

            if (element.NextChild is not { } child)
            {
                WriteClosingTag(element.Node, sb);
                openElements.Pop();
                continue;
            }

            /*
             * The child that was written last is also the one before this child, so the frame
             * supplies what #46 threaded through as a parameter - and for the same reason:
             * XmlLinkedNode has no back-pointer, so reading PreviousSibling rescans the parent
             * from FirstChild, O(k) per child and O(k^2) per parent.
             */
            var previousChild = element.LastWrittenChild;

            element.NextChild = child.NextSibling;
            element.LastWrittenChild = child;

            WriteSeparatorBeforeChild(child, sb, previousChild);

            if (WriteNode(child, sb, previousChild) is { } childElement)
            {
                openElements.Push(childElement);
            }
        }
    }

    /// <summary>
    /// Writes everything of <paramref name="node"/> that precedes its children - a leaf form in
    /// full, or a start tag with its attributes.
    /// </summary>
    /// <returns>
    /// The stack entry to descend into when <paramref name="node"/> has children left to write,
    /// or <see langword="null"/> when the node has been written in full.
    /// </returns>
    private OpenElement? WriteNode(XmlNode node, StringBuilder sb, XmlNode? previousSibling)
    {
        var prevNode = _lastNodeType;
        _lastNodeType = node.NodeType;

        if (TryWriteLeafNode(node, sb, prevNode, previousSibling))
        {
            return null;
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

        //if no children end tag
        if (node.HasChildNodes is false)
        {
            #region NoChildEndTag

            if (_currentOptions.UseSelfClosingTags)
            {
                sb.Append(_currentOptions.AddSpaceBeforeSelfClosingTag ? " />" : "/>");
            }
            else
            {
                sb.Append($"></{node.Name}>");
            }

            #endregion NoChildEndTag

            return null;
        }

        /*
         * Treat inline whitespace content like Text for indentation (see #209). Whitespace that
         * is the sole child counts whether or not it spans lines: it is written as content, so
         * the element must not also be indented around it - that double count is what grew a
         * line per format on <r>\n  </r>.
         */
        var firstChildIsInlineContent =
            node.FirstChild is { NodeType: XmlNodeType.Text or XmlNodeType.CDATA }
            || (_currentOptions.PreserveNewLines
                && node.FirstChild == node.LastChild
                && node.FirstChild is { NodeType: XmlNodeType.Whitespace });

        if (firstChildIsInlineContent is false)
        {
            _currentStartLength += _currentOptions.IndentLength;
        }

        return new OpenElement(node,
                               childCount: _currentOptions.AddEmptyLineBetweenElements ? node.ChildNodes.Count : 0);
    }

    /// <summary>
    /// Writes the node forms that have nothing to descend into - text, comments, CDATA and the
    /// rest of the leaves.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the node was written in full, <see langword="false"/> when it
    /// still needs a start tag.
    /// </returns>
    private bool TryWriteLeafNode(XmlNode node, StringBuilder sb, XmlNodeType prevNode, XmlNode? previousSibling)
    {
        switch (node.NodeType)
        {
            case XmlNodeType.CDATA:
                var newLine = prevNode is XmlNodeType.Text or XmlNodeType.Element ? string.Empty : Environment.NewLine;
                var spaces = prevNode is XmlNodeType.Text or XmlNodeType.Element ? string.Empty : new string(' ', _currentStartLength);
                Debug.WriteLine($"CDATA value: {node.Value}");

                sb.Append(newLine)
                  .Append(spaces)
                  .Append($"<![CDATA[{node.Value}]]>");
                return true;

            case XmlNodeType.Comment:
                var shouldIndent = true;
                if (_currentOptions.PreserveCommentPlacement)
                {
                    shouldIndent = previousSibling is { NodeType: XmlNodeType.Whitespace, Value: not null }
                                   && previousSibling.Value.Contains('\n');
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

                return true;
            case XmlNodeType.DocumentType:
            case XmlNodeType.SignificantWhitespace:
                return true;

            case XmlNodeType.EndElement:
                Debug.WriteLine("End");
                return false;

            case XmlNodeType.EndEntity:
            case XmlNodeType.EntityReference:
                sb.Append(node.OuterXml);
                return true;

            case XmlNodeType.ProcessingInstruction:
                sb.AppendLine($"<?{node.Name} {node.Value}?>");
                return true;


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
                        if (i is 0 && string.IsNullOrWhiteSpace(line))
                        {
                            /*
                             * Skipped, not indented. Every later line writes its own newline and
                             * indent, so an indent here is a run of spaces stranded after the start
                             * tag - and on the next format that run is the text node's leading line,
                             * which lands in this same branch with another indent written behind it.
                             */
                            continue;
                        }

                        if (i == lines.Length - 1)
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

                return true;
            case XmlNodeType.Whitespace:
                if (_currentOptions.PreserveNewLines is false || string.IsNullOrEmpty(node.Value))
                {
                    return true;
                }

                /*
                 * Only whitespace that is a node's sole content is content; anything with a
                 * sibling is structural indentation, and the formatter regenerates that. The
                 * guard used to ask whether a sibling was an *Element*, which let the indent
                 * around a comment or CDATA through as content on top of the indent generated
                 * for it - and that emitted indent came back as a whitespace node on the next
                 * format, so Format(Format(x)) grew a line per pass and never settled.
                 *
                 * previousSibling is the threaded parameter, not node.PreviousSibling: reading
                 * that rescans the parent from FirstChild (#46). See #209 for why any of this
                 * whitespace is kept at all.
                 */
                var hasSibling = previousSibling is not null || node.NextSibling is not null;

                if (hasSibling)
                {
                    return true;
                }

                /*
                 * Runs of newlines collapse to one; blank lines are AddEmptyLineBetweenElements'
                 * job, not this one. Text either way, so the closing tag follows the whitespace
                 * directly rather than adding a newline and an indent on top of content the
                 * element already carries.
                 */
                sb.Append(node.Value.Contains('\n') ?
                          NewLineRuns().Replace(node.Value, Environment.NewLine) :
                          node.Value);
                _lastNodeType = XmlNodeType.Text;

                return true;
            case XmlNodeType.Element: //Done
            case XmlNodeType.None:
            case XmlNodeType.Notation:
            case XmlNodeType.XmlDeclaration: //Done
            case XmlNodeType.Document: //Done
            case XmlNodeType.DocumentFragment: //Done
            case XmlNodeType.Entity:
            case XmlNodeType.Attribute:  //handled down
            default:
                return false;
        }
    }

    /// <summary>
    /// Writes the line break that separates <paramref name="child"/> from what precedes it,
    /// where the node types on either side call for one.
    /// </summary>
    private void WriteSeparatorBeforeChild(XmlNode child, StringBuilder sb, XmlNode? previousChild)
    {
        var commentNoNewLine = child.NodeType is XmlNodeType.Comment
                               && _currentOptions.PreserveCommentPlacement
                               && previousChild?.NodeType is XmlNodeType.Element or XmlNodeType.Whitespace;

        if (child.NodeType is not (XmlNodeType.Text or XmlNodeType.CDATA
                                 or XmlNodeType.EntityReference
                                 or XmlNodeType.SignificantWhitespace
                                 or XmlNodeType.Whitespace)
            && _lastNodeType is not XmlNodeType.Text
            && commentNoNewLine is false)
        {
            sb.Append(Environment.NewLine);
        }
    }

    /// <summary>
    /// Applies <see cref="Options.AddEmptyLineBetweenElements"/> to the child whose subtree has
    /// just been written.
    /// </summary>
    private void WriteBlankLineAfterChild(XmlNode child, StringBuilder sb, int childCount)
    {
        if (_currentOptions.AddEmptyLineBetweenElements
            && child.NodeType is XmlNodeType.Element
            && child.NextSibling?.NodeType is not (XmlNodeType.Text or XmlNodeType.SignificantWhitespace)
            && childCount > 2
            && child.NextSibling is not null)
        {
            sb.AppendLine();
        }
    }

    /// <summary>Closes a node whose children have all been written, and unwinds its indent.</summary>
    private void WriteClosingTag(XmlNode node, StringBuilder sb)
    {
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
