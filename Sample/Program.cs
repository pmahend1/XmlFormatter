using System;
using System.IO;

namespace XmlFormatter.Sample;

    internal class Program
    {
        public static void Main(string[] args)
        {
            var files = new string[]
            {
                "XMLFile1.xml",
                "XMLFile2.xml",
                "XMLFile3.xml",
                "XMLFile4.xml",
                "XMLFile5.xml",
                "XMLFile6.xml",
                "XMLFile7.xml",
                "XMLFile8.xml",
                "XMLFile9.xml",
                "XMLFile10.xml",
                "XmlFile11.xml",
                "XmlFile12.xml",
                "XmlFile13.xml",
                "XmlFile14.xml",
                "Sample.xml",
                "Sample2.xml",
                "Sample3.xml",
                "Sample4.xml",
                "ResxSample.xml"
            };

            foreach (var file in files)
            {
                var dashes = new string('-', file.Length + 1);

                WriteToConsole($"{file}: {Environment.NewLine}{dashes}", textColor: ConsoleColor.Blue);
                var xmlString = File.ReadAllText(file);

                WriteToConsole($"Input: {Environment.NewLine}------", textColor: ConsoleColor.Cyan);

                WriteToConsole($"{xmlString}{Environment.NewLine}");
                var formatter = new Formatter();

                var settings = new Options();

                var formattedText = formatter.Format(xmlString, formattingOptions: settings);
                File.WriteAllText("Formatted_" + file, formattedText.ToString());

                WriteToConsole($"Formatted: {Environment.NewLine}----------", textColor: ConsoleColor.Green);

                WriteToConsole($"{formattedText}{Environment.NewLine}{Environment.NewLine}");
            }
        }

        static void WriteToConsole(string text,
                                   ConsoleColor textColor = ConsoleColor.Gray,
                                   ConsoleColor backgroundColor = ConsoleColor.Black)
        {

            Console.ForegroundColor = textColor;
            Console.BackgroundColor = backgroundColor;

            Console.WriteLine(text);
            Console.ResetColor();
    }
}