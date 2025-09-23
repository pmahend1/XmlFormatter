namespace XmlFormatter;

public struct Constants
{
    public const string StartTagStart = "<";

    public const string StartTagEnd = ">";

    public const string EndTagStart = "</";

    public const string EndTagEnd = ">";

    public const string InlineEndTag = "/>";

    public static readonly char Space = ' ';

    public const string CommentTagStart = "<!--";

    public const string CommentTagEnd = "-->";

    public const string AssignmentStart = @"=""";

    public const string AssignmentEnd = @"""";

    public const string AssignmentStartSingleQuote = "='";

    public const string AssignmentEndSingleQuote = "'";

    public const string CDataStart = "<![CDATA[";

    public const string CDataEnd = "]]>";

    public const string DocTypeStart = "<!DOCTYPE";

    public static string DocTypeEnd(string val) => string.Format("[{0}]", val);

    public const string Apos = "&apos;";

    public const string XmlDeclaration = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>";
}