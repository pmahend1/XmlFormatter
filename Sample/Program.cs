using System;
using System.Text;
using System.Xml;

namespace XmlFormatter
{
    internal class Program
    {
        private static int firstLength = 0;

        private static void Main(string[] args)
        {
            var xmlString = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
       xmlns:d=""http://xamarin.com/schemas/2014/forms/design""
             xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
             mc:Ignorable=""d""
             x:Class=""App9.MainPage"">

    <StackLayout>
        <!-- Place new controls here -->
        <Label Text=""Welcome to Xamarin.Forms!""
           HorizontalOptions=""Center""
           VerticalOptions=""CenterAndExpand"" />
    </StackLayout>

</ContentPage>";
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlString); //
                                    // XDocument doc = XDocument.Parse(xml);

            Console.WriteLine("Unformatted xml\n" + xml);

            var formattedXml = Beautify(xml);

            FormatXML(xml);

            Console.ReadLine();
        }

        static public string Beautify(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                NewLineOnAttributes = true,
                OmitXmlDeclaration = false,
                NamespaceHandling = NamespaceHandling.OmitDuplicates,
            };
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }

            return sb.ToString();
        }

        private static void FormatXML(XmlDocument xml)
        {
            StringBuilder sb = new StringBuilder();
            var root = xml.DocumentElement;
            PrintNode(root, sb);
            sb.Append("\n" + "</" + root.Name + ">");
            Console.WriteLine(sb.ToString());
        }

        private static void PrintNode(XmlNode node, StringBuilder sb)
        {
            //identity if comment


            if (node.NodeType == XmlNodeType.Comment)
            {
                sb.Append(new string(' ', firstLength) + "<!-- " + node.Value + " -->");
                return;
            }


            //print attributes
            sb.Append(new string(' ', firstLength) + "<" + node.Name);

            if (node.Attributes?.Count > 0)
            {
                firstLength += new string("<" + node.Name + " ").Length;
                sb.Append(" ");

                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    var attribute = node.Attributes[i] as XmlAttribute;
                    var isLast = (i == (node.Attributes.Count - 1));
                    var newline = isLast ? "" : "\n";
                    sb.Append(attribute.Name + "=\"" + attribute.Value + "\"" + newline);
                    if (isLast)
                        sb.Append(">");
                    else
                        sb.Append(new String(' ', firstLength));
                }
            }
            else
            {
                sb.Append(">");
            }

            //prints nodes
            if (node.HasChildNodes)
            {
                firstLength += 2;
                for (int j = 0; j < node.ChildNodes.Count; j++)
                {
                    sb.Append("\n");
                    var currentSpace = firstLength;
                    PrintNode(node.ChildNodes[j], sb);
                    //close tag
                    if(node.ChildNodes[j].NodeType !=  XmlNodeType.Comment)
                        sb.Append("\n" + new string(' ', currentSpace) + "</" + node.ChildNodes[j].Name + ">");
                }
            }

            return;
        }
    }
}