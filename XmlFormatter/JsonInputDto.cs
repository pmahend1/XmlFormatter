using System.Text.Json.Serialization;

namespace XmlFormatter
{
    public class JsonInputDto
    {
        [JsonPropertyName("xmlString")]
        public string? XMLString { get; set; }

        [JsonPropertyName("actionKind")]
        public FormattingActionKind ActionKind { get; set; }

        [JsonPropertyName("formattingOptions")]
        public Options FormattingOptions { get; set; } = new Options();
    }
}
