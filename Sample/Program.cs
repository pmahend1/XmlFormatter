using System;
using System.IO;

namespace XmlFormatter
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var xmlString = File.ReadAllText(@"Sample.xml");
            var formatter = new Formatter();

            Console.WriteLine(formatter.Format(xmlString));

            //old way
            Console.WriteLine("--------Old Way------");
            Console.WriteLine(formatter.Beautify(xmlString));
        }
    }
}