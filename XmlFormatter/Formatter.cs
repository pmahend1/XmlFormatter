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

    /// <summary>
    /// Comments that began a line in the source. Populated only when the whitespace saying so is
    /// about to be stepped over, and so cannot be read off the node itself.
    /// </summary>
    private HashSet<XmlNode> _ownLineComments = [];

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

    private static XmlDocument ConvertToXmlDocument(string input, bool preserveWhitespace = false)
    {
        XmlDocument xml = new()
        {
            PreserveWhitespace = preserveWhitespace
        };
        xml.LoadXml(input);
        return xml;
    }

    /// <summary>
    /// Records every comment that began a line of its own, reading the whitespace nodes that say
    /// so before <see cref="SkipCommentEvidence"/> makes the rest of the run blind to them.
    /// </summary>
    private static HashSet<XmlNode> IndexOwnLineComments(XmlDocument xml)
    {
        // XmlNode has no value equality and identity is what is meant here, so say so.
        var ownLineComments = new HashSet<XmlNode>(ReferenceEqualityComparer.Instance);

        // An explicit stack for the same reason PrintNode has one: recursion overflows on depth.
        var parents = new Stack<XmlNode>();
        parents.Push(xml);

        while (parents.Count > 0)
        {
            var parent = parents.Pop();
            XmlNode? previous = null;

            for (var child = parent.FirstChild; child is not null; previous = child, child = child.NextSibling)
            {
                if (child.NodeType is XmlNodeType.Comment
                    && previous is { NodeType: XmlNodeType.Whitespace, Value: not null }
                    && previous.Value.Contains('\n'))
                {
                    ownLineComments.Add(child);
                }

                if (child.HasChildNodes)
                {
                    parents.Push(child);
                }
            }
        }

        return ownLineComments;
    }

    /// <summary>
    /// Whether the DOM holds whitespace nodes that nothing but
    /// <see cref="Options.PreserveCommentPlacement"/> asked for.
    /// </summary>
    private bool WhitespaceIsCommentEvidenceOnly => _currentOptions.PreserveCommentPlacement
                                                    && _currentOptions.PreserveNewLines is false;

    /// <summary>
    /// The first child of <paramref name="node"/> the rest of this run can see.
    /// </summary>
    private XmlNode? FirstVisibleChild(XmlNode node)
    {
        return SkipCommentEvidence(node.FirstChild);
    }

    /// <summary>
    /// The sibling after <paramref name="node"/> the rest of this run can see.
    /// </summary>
    private XmlNode? NextVisibleSibling(XmlNode node)
    {
        return SkipCommentEvidence(node.NextSibling);
    }

    /// <summary>
    /// Steps over whitespace loaded only to place comments, so that every other rule sees the
    /// DOM it would have seen had that whitespace never been read.
    /// </summary>
    /// <remarks>
    /// Deleting the nodes instead would be simpler to reason about and quadratic to do:
    /// <see cref="XmlNode.RemoveChild"/> relinks through <c>PreviousSibling</c>, which rescans
    /// the parent from its first child (see #46). Adjacent whitespace is one node, so a step
    /// here passes over at most one, and no node is passed over twice.
    /// </remarks>
    private XmlNode? SkipCommentEvidence(XmlNode? node)
    {
        while (WhitespaceIsCommentEvidenceOnly && node is { NodeType: XmlNodeType.Whitespace })
        {
            node = node.NextSibling;
        }

        return node;
    }

    /// <summary>Number of children of <paramref name="node"/> that this run will write.</summary>
    private int VisibleChildCount(XmlNode node)
    {
        var count = 0;

        for (var child = FirstVisibleChild(node); child is not null; child = NextVisibleSibling(child))
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Whether <paramref name="comment"/> began a line of its own in the source, and so should
    /// begin one here. Only meaningful under <see cref="Options.PreserveCommentPlacement"/>.
    /// </summary>
    /// <param name="previousSibling">
    /// The node before <paramref name="comment"/> under the same parent, or null when it is first.
    /// </param>
    private bool StartsItsOwnLine(XmlNode comment, XmlNode? previousSibling)
    {
        /*
         * One answer, two places to read it from. Under PreserveNewLines the whitespace is part
         * of the walk, so the sibling threaded down it answers this for free; otherwise the walk
         * steps over that whitespace and the answer was taken at load instead.
         */
        return _currentOptions.PreserveNewLines ?
               previousSibling is { NodeType: XmlNodeType.Whitespace, Value: not null }
               && previousSibling.Value.Contains('\n') :
               _ownLineComments.Contains(comment);
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
            /*
             * Where a comment sat is only readable from the whitespace around it, and
             * PreserveNewLines is what normally keeps that whitespace. Load it for either option
             * and read the placements out of it up front; the walk then steps over what it was
             * not asked to preserve. Without this the option had no evidence to work from, took
             * every comment for a trailing one, and failed silently rather than being ignored.
             */
            var xmlDocument = ConvertToXmlDocument(input: input,
                                                   preserveWhitespace: _currentOptions.PreserveNewLines
                                                                       || _currentOptions.PreserveCommentPlacement);

            _ownLineComments = WhitespaceIsCommentEvidenceOnly ? IndexOwnLineComments(xmlDocument) : [];
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

        for (var node = FirstVisibleChild(xml); node is not null; previousSibling = node, node = NextVisibleSibling(node))
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

                        /*
                         * PUBLIC takes two literals - the public id then the system id - so the
                         * system id is written bare after one. On its own it needs its own
                         * keyword, and without it `<!DOCTYPE root "my.dtd">` is not something any
                         * parser will read back, which also meant the formatter's own output for
                         * such a document could not be formatted a second time.
                         */
                        if (xml.DocumentType.PublicId is not null)
                        {
                            docTypeText += $" PUBLIC \"{xml.DocumentType.PublicId}\"";
                        }
                        else if (xml.DocumentType.SystemId is not null)
                        {
                            docTypeText += " SYSTEM";
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
                        if (NextVisibleSibling(node) is not null)
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

            element.NextChild = NextVisibleSibling(child);
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

        var firstChild = FirstVisibleChild(node);
        var hasChildren = firstChild is not null;

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
                else if (hasChildren)
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
            if (hasChildren)
            {
                sb.Append('>');
            }
            //else see NoChildEndTag
        }

        //if no children end tag
        if (hasChildren is false)
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
            firstChild is { NodeType: XmlNodeType.Text or XmlNodeType.CDATA }
            || (_currentOptions.PreserveNewLines
                && firstChild == node.LastChild
                && firstChild is { NodeType: XmlNodeType.Whitespace });

        if (firstChildIsInlineContent is false)
        {
            _currentStartLength += _currentOptions.IndentLength;
        }

        return new OpenElement(node,
                               firstChild: firstChild,
                               childCount: _currentOptions.AddEmptyLineBetweenElements ? VisibleChildCount(node) : 0);
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
                /*
                 * Off, every comment is repositioned onto its own line; on, only the ones that
                 * already had one. Either way the line break itself comes from the separator, so
                 * this decides the indent alone - see WriteSeparatorBeforeChild.
                 */
                var shouldIndent = _currentOptions.PreserveCommentPlacement is false
                                   || StartsItsOwnLine(node, previousSibling);

                // Nothing at document level is nested, so nothing there is indented.
                if (node.ParentNode?.NodeType is XmlNodeType.Document)
                {
                    shouldIndent = false;
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
        /*
         * A comment that shared a line with what came before it keeps doing so - that is the
         * whole of PreserveCommentPlacement, and the previous node type is no way to tell: a
         * comment first under its parent, or after text or another comment, shared the line too
         * and used to be broken onto a new one with no indent, against the left margin.
         */
        var commentKeepsItsLine = child.NodeType is XmlNodeType.Comment
                                  && _currentOptions.PreserveCommentPlacement
                                  && StartsItsOwnLine(child, previousChild) is false;

        if (child.NodeType is not (XmlNodeType.Text or XmlNodeType.CDATA
                                 or XmlNodeType.EntityReference
                                 or XmlNodeType.SignificantWhitespace
                                 or XmlNodeType.Whitespace)
            && _lastNodeType is not XmlNodeType.Text
            && commentKeepsItsLine is false)
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
        var nextSibling = NextVisibleSibling(child);

        if (_currentOptions.AddEmptyLineBetweenElements
            && child.NodeType is XmlNodeType.Element
            && nextSibling?.NodeType is not (XmlNodeType.Text or XmlNodeType.SignificantWhitespace)
            && childCount > 2
            && nextSibling is not null)
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
