namespace XmlFormatter;

public struct JsonInputDto
{
    public JsonInputDto()
    {
    }

    public string? XMLString { get; set; }
    public FormattingActionKind ActionKind { get; set; }
    public Options FormattingOptions { get; set; } = new Options();
}