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
                var jsInputDto = JsonConvert.DeserializeObject<JSInputDTO>(inputJsonString);
                if (jsInputDto != null)
                {
                    var options = new Options();

                    options.IndentLength = jsInputDto.IndentLength ?? options.IndentLength;
                    options.UseSelfClosingTags = jsInputDto.UseSelfClosingTags ?? options.UseSelfClosingTags;
                    options.UseSingleQuotes = jsInputDto.UseSingleQuotes ?? options.UseSingleQuotes;
                    options.AllowSingleQuoteInAttributeValue = jsInputDto.AllowSingleQuoteInAttributeValue ?? options.AllowSingleQuoteInAttributeValue;
                    options.AddSpaceBeforeSelfClosingTag = jsInputDto.AddSpaceBeforeSelfClosingTag ?? options.AddSpaceBeforeSelfClosingTag;
                    options.WrapCommentTextWithSpaces = jsInputDto.WrapCommentTextWithSpaces ?? options.WrapCommentTextWithSpaces;
                    options.AllowWhiteSpaceUnicodesInAttributeValues = jsInputDto.AllowWhiteSpaceUnicodesInAttributeValues ?? options.AllowWhiteSpaceUnicodesInAttributeValues;

                    var formattedXML = new Formatter().Format(jsInputDto.XMLString, options);

                    return Task.FromResult((object)formattedXML);
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
                throw;
            }
        }

        public Task<object> Minimize(string input)
        {
            var formattedXML = new Formatter().Minimize(input);
            return Task.FromResult<object>((object)formattedXML);
        }
    }
}