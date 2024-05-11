namespace XmlFormatter;

public struct Options
{
    public Options() { }

    public int IndentLength { get; set; } = 4;
    public bool UseSelfClosingTags { get; set; } = true;
    public bool UseSingleQuotes { get; set; } = false;
    public bool AllowSingleQuoteInAttributeValue { get; set; } = true;
    public bool AddSpaceBeforeSelfClosingTag { get; set; } = true;
    public bool WrapCommentTextWithSpaces { get; set; } = true;
    public bool AllowWhiteSpaceUnicodesInAttributeValues { get; set; } = true;
    public bool PositionFirstAttributeOnSameLine { get; set; } = true;
    public bool PreserveWhiteSpacesInComment { get; set; } = false;
    public bool PositionAllAttributesOnFirstLine { get; set; } = false;
    public bool AddSpaceBeforeEndOfXmlDeclaration { get; set; } = false;
    public int AttributesInNewlineThreshold { get; set; } = 1;
}