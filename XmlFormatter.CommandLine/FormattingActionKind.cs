using System.Text.Json.Serialization;

namespace XmlFormatter.CommandLine;

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum FormattingActionKind
{
    Unsupported,
    Format,
    Minimize,
}
