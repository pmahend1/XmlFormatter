namespace XmlFormatter.VSCode
{
    public class JSInputDTO
    {
        public string? XMLString { get; set; }
        public int? IndentLength { get; set; }
        public bool? UseSelfClosingTags { get; set; }
        public bool? UseSingleQuotes { get; set; }
        public bool? AllowSingleQuoteInAttributeValue { get; set; }
        public bool? AddSpaceBeforeSelfClosingTag { get; set; }
        public bool? WrapCommentTextWithSpaces { get; set; }
        public bool? AllowWhiteSpaceUnicodesInAttributeValues { get; set; }
        public bool? PositionFirstAttributeOnSameLine { get; set; }
    }
}