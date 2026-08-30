using System.Xml;

namespace XmlFormatter;

/// <summary>
/// One entry on <see cref="Formatter"/>'s traversal stack: a node whose start tag has been
/// written and whose children are still being walked. This is what used to be a recursive
/// call frame, so nesting depth now costs heap instead of stack.
/// </summary>
internal sealed class OpenElement(XmlNode node, int childCount)
{
    public XmlNode Node { get; } = node;

    /// <summary>
    /// Number of children, or 0 when <see cref="Options.AddEmptyLineBetweenElements"/> is off
    /// and it is never read. Counting walks the sibling list, so it is done once per element.
    /// </summary>
    public int ChildCount { get; } = childCount;

    /// <summary>The child to write next, or null once the children are exhausted.</summary>
    public XmlNode? NextChild { get; set; } = node.FirstChild;

    /// <summary>
    /// The child visited most recently, or null before the first one. It answers two questions
    /// at two moments: on re-entry it is the child whose subtree has just landed, which is when
    /// a following blank line can be decided; and when the walk moves on it is the next child's
    /// previous sibling, which <see cref="System.Xml.XmlLinkedNode"/> cannot supply cheaply
    /// (see #46).
    /// </summary>
    public XmlNode? LastWrittenChild { get; set; }
}
