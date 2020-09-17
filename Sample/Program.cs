using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using XmlFormatter.VSCode;

namespace XmlFormatter
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var file = @"XMLFile3.xml";
            var xmlString = File.ReadAllText(file);
            var formatter = new PrettyXML();

            var jsonString = JsonConvert.SerializeObject(new JSInputDTO
            {
                XMLString = @xmlString,
            });

            var formattedText = formatter.Format(jsonString).Result;
            Console.WriteLine(formatter.Format(jsonString).Result);
            File.WriteAllText("Formatted_" + file, formattedText.ToString());

            //old way
            Console.WriteLine("--------Old Way------");
            Console.WriteLine(new Formatter().Beautify(xmlString));
        }

        private static string FooBar(string xmlStr)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlStr);
            StringWriter sw = new StringWriter();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "\t",
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.Replace,
                NewLineOnAttributes = false,
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
            };
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                xmlDoc.Save(writer);
            }

            return sw.ToString();
        }
    }
}