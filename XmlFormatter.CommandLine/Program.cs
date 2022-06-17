using Newtonsoft.Json;
using System.Diagnostics;

namespace XmlFormatter.CommandLine
{
    public class ConsoleProgram
    {
        public static async Task Main(string[] args)
        {
            string inputString = string.Empty;
            using (StreamReader reader = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
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
                    JsonInputDto? jsonInputDto = JsonConvert.DeserializeObject<JsonInputDto>(inputString);
                    if (jsonInputDto == null || string.IsNullOrWhiteSpace(jsonInputDto.XMLString))
                    {
                        throw new Exception("Unable to parse file");
                    }
                        
                    switch (jsonInputDto.ActionKind)
                    {
                        case FormattingActionKind.Format:
                            Console.Write(new Formatter().Format(jsonInputDto.XMLString, jsonInputDto.FormattingOptions));
                            break;
                        case FormattingActionKind.Minimize:
                            Console.Write(new Formatter().Minimize(jsonInputDto.XMLString));
                            break;
                        default:
                            throw new Exception("Unsupported action");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.StackTrace);
                    throw;
                }
            }
        }
    }
}
