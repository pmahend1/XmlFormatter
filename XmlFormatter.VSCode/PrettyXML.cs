using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace XmlFormatter.VSCode
{
    public class PrettyXML
    {
        public Task<object> Format(string inputJsonString)
        {
            try
            {
                var _JSInputDTO = JsonConvert.DeserializeObject<JSInputDTO>(inputJsonString);
                if (_JSInputDTO != null)
                {
                    var options = new Options();
                    options.IndentLength = _JSInputDTO?.IndentLength ?? options.IndentLength;
                    options.UseSelfClosingTags = _JSInputDTO?.UseSelfClosingTags ?? options.UseSelfClosingTags;
                    options.UseSingleQuotes = _JSInputDTO?.UseSingleQuotes ?? options.UseSingleQuotes;

                    var formattedXML = new Formatter().Format(_JSInputDTO.XMLString, options);
                    return Task.FromResult<object>((object)formattedXML);
                }
                else
                {
                    throw new Exception("Invalid input , cant cast");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                throw ex;
            }
        }

        public Task<object> Minimize(string input)
        {
                var formattedXML = new Formatter().Minimize(input);
                return Task.FromResult<object>((object)formattedXML);
        }
    }
}