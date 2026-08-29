using System.Collections.Immutable;

namespace XmlFormatter;

public record struct Options()
{
    public int IndentLength { get; init; } = 4;
    public bool UseSelfClosingTags { get; init; } = true;
    public bool UseSingleQuotes { get; init; } = false;
    public bool AllowSingleQuoteInAttributeValue { get; init; } = true;
    public bool AddSpaceBeforeSelfClosingTag { get; init; } = true;
    public bool WrapCommentTextWithSpaces { get; init; } = true;
    public bool AllowWhiteSpaceUnicodesInAttributeValues { get; init; } = true;
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
