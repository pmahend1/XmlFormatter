using System;
using System.IO;

namespace XmlFormatter
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var file = @"XMLFile3.xml";
            var xmlString = File.ReadAllText(file);
            var formatter = new Formatter();

            var formattedText = formatter.Format(xmlString).Result;
            Console.WriteLine(formatter.Format(xmlString).Result);
            File.WriteAllText("Formatted_" + file, formattedText.ToString());

            //old way
            Console.WriteLine("--------Old Way------");
            Console.WriteLine(formatter.Beautify(xmlString));
        }
    }
}