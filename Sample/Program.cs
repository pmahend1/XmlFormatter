using System;
using System.IO;
using System.Xml;

namespace XmlFormatter
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var file = @"ResxSample.xml";
            var xmlString = File.ReadAllText(file);
            var formatter = new Formatter();

            //var jsonString = JsonConvert.SerializeObject(new JSInputDTO
            //{
            //    XMLString = @xmlString,
            //});

            var formattedText = formatter.Format(xmlString);//.Result;
            Console.WriteLine(formatter.Format(xmlString));//.Result);
            File.WriteAllText("Formatted_" + file, formattedText.ToString());
            //await Task.Delay(1000);
            //minimize
            //var minxml = new Formatter().Minimize(xmlString);
            //Console.WriteLine("--------Old Way------");
            //Console.WriteLine(minxml);
            //File.WriteAllText("Minimized_" + file, minxml);
        }

        private static string FooBar(string xmlStr)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlStr);
            StringWriter sw = new StringWriter();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = false,
                IndentChars = "",
                NewLineChars = "",
                NewLineHandling = NewLineHandling.Replace,
                NewLineOnAttributes = false,
                NamespaceHandling = NamespaceHandling.Default,
            };
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                xmlDoc.Save(writer);
            }

            return sw.ToString();
        }
    }
}