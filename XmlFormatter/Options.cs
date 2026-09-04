using System.Collections.Immutable;

namespace XmlFormatter;

public record struct Options()
{
    public int IndentLength { get; init; } = 4;
    public bool UseSelfClosingTags { get; init; } = true;
    public bool UseSingleQuotes { get; init; } = false;

    /// <summary>
    /// Write an apostrophe in an attribute value as itself rather than as &amp;apos;. Ignored
    /// under <see cref="UseSingleQuotes"/>, where the apostrophe delimits the value and a
    /// literal one would close it early - the only reading of this option that stays
    /// well-formed is the double-quoted one.
    /// </summary>
    public bool AllowSingleQuoteInAttributeValue { get; init; } = true;

    public bool AddSpaceBeforeSelfClosingTag { get; init; } = true;
    public bool WrapCommentTextWithSpaces { get; init; } = true;

    /// <summary>
    /// Write a tab, newline or carriage return inside an attribute value as a hex character
    /// reference rather than as itself. On by default because those three do not survive a
    /// round-trip otherwise: XML attribute-value normalization (XML 1.0 section 3.3.3) replaces
    /// each of them with a space when the document is read back.
    ///
    /// All three are ASCII, so this option and <see cref="EscapeInvisibleNonAsciiCharacters"/>
    /// never decide the same character - see the note there.
    /// </summary>
    public bool AllowWhiteSpaceUnicodesInAttributeValues { get; init; } = true;

    /// <summary>
    /// Write the non-ASCII characters that draw nothing - NBSP, the zero-width spaces, the bidi
    /// and other format controls - as hex character references, in text content and in attribute
    /// values. Visible non-ASCII is never touched: an umlaut, a CJK ideograph and an emoji stay
    /// literal whichever way this is set.
    ///
    /// Off by default, which is the v2.3.1 output. It exists because an invisible character is
    /// indistinguishable from a plain space on screen, so an editor can lose one to a stray
    /// keystroke with nothing to show for it.
    ///
    /// <b>This is not a superset of <see cref="AllowWhiteSpaceUnicodesInAttributeValues"/>, and
    /// deliberately does not defer to it.</b> Tab, newline and carriage return are invisible too,
    /// and they belong to that option alone; this one starts above ASCII. So the two can look
    /// like they contradict each other - turn this on with that one off and an attribute holding
    /// both comes out with a literal tab next to an escaped NBSP - and they do not. They are
    /// about different characters for different reasons: that option is about surviving
    /// attribute-value normalization, this one is about being able to see what is there.
    /// </summary>
    public bool EscapeInvisibleNonAsciiCharacters { get; init; } = false;

    public bool PositionFirstAttributeOnSameLine { get; init; } = true;
    public bool PreserveWhiteSpacesInComment { get; init; } = false;
    public bool PositionAllAttributesOnFirstLine { get; init; } = false;
    public bool AddSpaceBeforeEndOfXmlDeclaration { get; init; } = false;
    public bool AddXmlDeclarationIfMissing { get; init; } = true;
    public int AttributesInNewlineThreshold { get; init; } = 1;

    /// <summary>Element-name patterns exempt from <see cref="PositionAllAttributesOnFirstLine"/>.</summary>
    public ImmutableList<string> WildCardedExceptionsForPositionAllAttributesOnFirstLine { get; init; } = [];

    public bool AddEmptyLineBetweenElements { get; init; } = false;
    public bool PreserveNewLines { get; init; } = false;
    public bool PreserveCommentPlacement { get; init; } = false;

    // Handwritten: the synthesized version compared the patterns list by reference.
    // OptionsEqualityTests fails if a new option is missing here.
    public readonly bool Equals(Options other)
    {
        return IndentLength == other.IndentLength &&
               UseSelfClosingTags == other.UseSelfClosingTags &&
               UseSingleQuotes == other.UseSingleQuotes &&
               AllowSingleQuoteInAttributeValue == other.AllowSingleQuoteInAttributeValue &&
               AddSpaceBeforeSelfClosingTag == other.AddSpaceBeforeSelfClosingTag &&
               WrapCommentTextWithSpaces == other.WrapCommentTextWithSpaces &&
               AllowWhiteSpaceUnicodesInAttributeValues == other.AllowWhiteSpaceUnicodesInAttributeValues &&
               EscapeInvisibleNonAsciiCharacters == other.EscapeInvisibleNonAsciiCharacters &&
               PositionFirstAttributeOnSameLine == other.PositionFirstAttributeOnSameLine &&
               PreserveWhiteSpacesInComment == other.PreserveWhiteSpacesInComment &&
               PositionAllAttributesOnFirstLine == other.PositionAllAttributesOnFirstLine &&
               AddSpaceBeforeEndOfXmlDeclaration == other.AddSpaceBeforeEndOfXmlDeclaration &&
               AddXmlDeclarationIfMissing == other.AddXmlDeclarationIfMissing &&
               AttributesInNewlineThreshold == other.AttributesInNewlineThreshold &&
               AddEmptyLineBetweenElements == other.AddEmptyLineBetweenElements &&
               PreserveNewLines == other.PreserveNewLines &&
               PreserveCommentPlacement == other.PreserveCommentPlacement &&
               PatternsEqual(WildCardedExceptionsForPositionAllAttributesOnFirstLine, other.WildCardedExceptionsForPositionAllAttributesOnFirstLine);
    }

    public override readonly int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(IndentLength);
        hash.Add(UseSelfClosingTags);
        hash.Add(UseSingleQuotes);
        hash.Add(AllowSingleQuoteInAttributeValue);
        hash.Add(AddSpaceBeforeSelfClosingTag);
        hash.Add(WrapCommentTextWithSpaces);
        hash.Add(AllowWhiteSpaceUnicodesInAttributeValues);
        hash.Add(EscapeInvisibleNonAsciiCharacters);
        hash.Add(PositionFirstAttributeOnSameLine);
        hash.Add(PreserveWhiteSpacesInComment);
        hash.Add(PositionAllAttributesOnFirstLine);
        hash.Add(AddSpaceBeforeEndOfXmlDeclaration);
        hash.Add(AddXmlDeclarationIfMissing);
        hash.Add(AttributesInNewlineThreshold);
        hash.Add(AddEmptyLineBetweenElements);
        hash.Add(PreserveNewLines);
        hash.Add(PreserveCommentPlacement);

        foreach (var pattern in WildCardedExceptionsForPositionAllAttributesOnFirstLine ?? [])
        {
            hash.Add(pattern, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    /// <summary>Ordinal, order-sensitive: these are patterns, and order decides which matches first.</summary>
    private static bool PatternsEqual(ImmutableList<string>? left, ImmutableList<string>? right)
    {
        // default(Options) leaves the property null, so neither side can be assumed present.
        left ??= [];
        right ??= [];

        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (string.Equals(left[i], right[i], StringComparison.Ordinal) is false)
            {
                return false;
            }
        }

        return true;
    }
}
