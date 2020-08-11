using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace XmlFormatter
{
    public class Program
    {
        private static int firstLength = 0;

        private static void Main(string[] args)
        {

            var content = File.ReadAllText(@"Sample.xml");
            var temp = new Formatter().Format(content);

             Console.WriteLine(temp);
            //new Formatter().FormatWithReader(new StringReader(content));
            //old way

            //Console.WriteLine("Old way");
            //XmlDocument xmlDocument = new XmlDocument();
            //xmlDocument.LoadXml(content);
            //var sb = new StringBuilder();
            //using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { 
            //Indent= true,
            //NewLineOnAttributes = true,
            //}))
            //{
            //    xmlDocument.WriteTo(writer);
            //    writer.Close();

            //}
            //Console.WriteLine(sb.ToString()); ;
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
            sb.Append("\n" + SymbolConstants.EndTagStart + root.Name + ">");
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

            switch (node.NodeType)
            {
                case XmlNodeType.None:
                    break;
                case XmlNodeType.Element:
                    break;
                case XmlNodeType.Attribute:
                    break;
                case XmlNodeType.Text:
                    break;
                case XmlNodeType.CDATA:
                    break;
                case XmlNodeType.EntityReference:
                    break;
                case XmlNodeType.Entity:
                    break;
                case XmlNodeType.ProcessingInstruction:
                    break;
                case XmlNodeType.Comment:
                    break;
                case XmlNodeType.Document:
                    break;
                case XmlNodeType.DocumentType:
                    break;
                case XmlNodeType.DocumentFragment:
                    break;
                case XmlNodeType.Notation:
                    break;
                case XmlNodeType.Whitespace:
                    break;
                case XmlNodeType.SignificantWhitespace:
                    break;
                case XmlNodeType.EndElement:
                    break;
                case XmlNodeType.EndEntity:
                    break;
                case XmlNodeType.XmlDeclaration:
                    break;
                default:
                    break;
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
                        sb.Append("\n" + new string(' ', currentSpace) + SymbolConstants.EndTagStart + node.ChildNodes[j].Name + ">");
                }
            }

            return;
        }
    }
}