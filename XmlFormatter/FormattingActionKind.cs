using System.Text.Json.Serialization;

namespace XmlFormatter
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FormattingActionKind
    {
        Unsupported,
        Format,
        Minimize,
    }
}