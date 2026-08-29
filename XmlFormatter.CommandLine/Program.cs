using System.Diagnostics;
using System.Text.Json;
using System.Xml;

namespace XmlFormatter.CommandLine;

internal class ConsoleProgram
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static async Task Main(string[] _)
    {
        string inputString;

        using (StreamReader reader = new(Console.OpenStandardInput(), Console.InputEncoding))
        {
            inputString = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(inputString))
        {
            throw new Exception("Unable to read text");
        }

        try
        {
            var jsonInputDto = JsonSerializer.Deserialize<JsonInputDto?>(inputString, options: JsonSerializerOptions);
            if (jsonInputDto is null || string.IsNullOrWhiteSpace(jsonInputDto.Value.Xml))
            {
                throw new Exception("Unable to parse file");
            }

            var formatter = new Formatter();

            switch (jsonInputDto.Value.ActionKind)
            {
                case FormattingActionKind.Format:
                    Console.Write(formatter.Format(jsonInputDto.Value.Xml, jsonInputDto.Value.FormattingOptions));
                    break;
                case FormattingActionKind.Minimize:
                    Console.Write(formatter.Minimize(jsonInputDto.Value.Xml));
                    break;
                case FormattingActionKind.Unsupported:
                default:
                    throw new Exception("Unsupported action");
            }
        }
        catch (XmlException xmlException)
        {
            throw new Exception($"{xmlException.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }
    }
}
