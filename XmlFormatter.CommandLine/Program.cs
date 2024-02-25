using System.Diagnostics;
using System.Text.Json;
using System.Xml;

namespace XmlFormatter.CommandLine;

public class ConsoleProgram
{
    public static async Task Main(string[] args)
    {
        string inputString = string.Empty;
        using (StreamReader reader = new(Console.OpenStandardInput(), Console.InputEncoding))
        {
            inputString = await reader.ReadToEndAsync().ConfigureAwait(false);
        }
            
        if (string.IsNullOrEmpty(inputString))
        {
            throw new Exception("Unable to read text");
        }
        else
        {
            try
            {
                JsonInputDto? jsonInputDto = JsonSerializer.Deserialize<JsonInputDto>(inputString, options: new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                if (jsonInputDto == null || string.IsNullOrWhiteSpace(jsonInputDto.XMLString))
                {
                    throw new Exception("Unable to parse file");
                }
                var formatter = new Formatter(); 
                switch (jsonInputDto.ActionKind)
                {
                    case FormattingActionKind.Format:
                        Console.Write(formatter.Format(jsonInputDto.XMLString, jsonInputDto.FormattingOptions));
                        break;
                    case FormattingActionKind.Minimize:
                        Console.Write(formatter.Minimize(jsonInputDto.XMLString));
                        break;
                    default:
                        throw new Exception("Unsupported action");
                }
            }
            catch(XmlException xmlException)
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
}
