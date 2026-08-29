namespace XmlFormatter.CommandLine;

internal struct JsonInputDto()
{
    public string? Xml { get; init; }
    public FormattingActionKind ActionKind { get; init; }
    public Options FormattingOptions { get; init; } = new();
}
