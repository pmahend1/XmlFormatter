//#region imports
import * as fs from "fs";
import * as xmljs from "xml-js";
import * as sf from "typescript-string-operations";
//#endregion

//#region declarations
let content = "";
let currentStartLength = 0;
let currentAttributeSpace = 0;
let lastNodeType = "";
//#endregion

fs.readFile("./SampleFiles/Sample.xml", function (err, data)
{
    if (err)
    {
        console.log(err);
        return;
    } else if (data)
    {
        content = data.toString();

        var convert = xmljs;
        var result = convert.xml2json(content, { compact: false, spaces: 4 });
        if (result)
        {
            var obj = JSON.parse(result);
            var formatted = FormatXMLDocument(obj,4);
            console.log(formatted);
        }
    }

})

function FormatXMLDocument(jsonObj: any, indentLength : number)
{
    var sb = new sf.StringBuilder();
    var declaration = jsonObj["declaration"];
    if (declaration)
    {
        var decAttr = declaration["attributes"];

        sb.Append("<?xml ");
        for (const [key, value] of Object.entries(decAttr))
        {
            sb.AppendFormat(`${key}="${value}"`);
        }
        sb.Append(`?>\n`);
    }
    // if (xml.DocumentType != null)
    // {
    //     sb.Append(xml.DocumentType.OuterXml + SymbolConstants.Newline);
    // }
    var root = jsonObj["elements"][0];
    lastNodeType = "document";

    PrintNode(root, sb,indentLength);
    return sb.ToString();
}


function PrintNode(node: any, sb: sf.StringBuilder, indentLength : number)
{
    var prevNode = lastNodeType;
    lastNodeType = node["type"];
    switch (lastNodeType)
    {
        case "attribute":
            break;
        case "cdata":
            var newLine = (prevNode == "text") ? sf.String.Empty : `\n`;
            var spaces = (prevNode == "text") ? sf.String.Empty : " ".repeat(currentStartLength);
            sb.AppendFormat(`${newLine}${spaces}<![CDATA[${node["cdata"]}]]>`)
            return;
        case "comment":
            sb.AppendFormat(`${" ".repeat(currentStartLength)}<!--${node["comment"]}-->`);
            return;
        case "documenttype":
            //sb.Append(SymbolConstants.DocTypeStart + SymbolConstants.Space + SymbolConstants.DocTypeEnd(node.Value));
            sb.AppendFormat(`<!DOCTYPE  [${node["document"]}]`);
            return;
        case "element":
            //Done
            break;

        case "endelement":
            break;

        case "endentity":
            break;

        case "entity":
            break;

        case "entityreference":
            sb.Append(node[1]);
            return;

        case "none":
            break;

        case "notation":
            break;

        case "processinginstruction":
            sb.AppendFormat(`<?${node.Name} ${node.Value}?>`);
            return;

        case "significantwhitespace":
            break;

        case "text":
            sb.Append(node["text"]);
            return;

        case "whitespace":
            break;

        case "declaration":
            //done
            break;

        default:
            break;
    }

    //print start tag
    var space = prevNode != "text" ? " ".repeat(currentStartLength) : sf.String.Empty;
    sb.AppendFormat(`${space}<${node["name"]}`);

    //print attributes
    var attributes = node["attributes"];

    if (attributes)
    {

        sb.Append(" ");
        var nodeName: string = node["name"];
        currentAttributeSpace = nodeName.length + indentLength;
        for (let i = 0; i < Object.keys(attributes).length; i++)
        {
            const attributeName = Object.keys(attributes)[i];
            const attributeValue = Object.values(attributes)[i];
            var isLast = (i === Object.keys(attributes).length - 1);
            var newLine = isLast ? sf.String.Empty : `\n`;
            sb.AppendFormat(`${attributeName}="${attributeValue}"${newLine}`);
            if (isLast && node["elements"])
                sb.Append(">");
            else if (!isLast)
                sb.Append(" ".repeat(currentAttributeSpace));
        }


    } else 
    {
        var values: any[] = Object.values(node);
        var nodeValue: string = values[0];
        if (!nodeValue.endsWith("/>"))
        {
            sb.Append(">");
        }

    }

    //print nodes
    var childNodes: any[] = node["elements"];
    if (childNodes)
    {
        var prevStartLength = currentStartLength;
        if (childNodes[0]["type"] != "text")
        {
            prevStartLength = currentStartLength;
            currentStartLength += indentLength;
        }

        for (let j = 0; j < childNodes.length; j++)
        {
            const currentChild = childNodes[j];
            if (currentChild["type"] != "text" && currentChild["type"] != "cdata" && currentChild["type"] != "entityreference" && lastNodeType != "text")
            {
                sb.Append(`\n`);
            }
            //
            PrintNode(currentChild, sb,indentLength);
        }
        if (node.NodeType != "comment"
            && node.NodeType != "cdata"
            && node.NodeType != "documenttype"
            && node.NodeType != "text")
        {
            if (currentStartLength >= indentLength
                && lastNodeType != "text"
                && lastNodeType != "cdata"
                && lastNodeType != "documenttype"
                && lastNodeType != "entityreference"
            )
            {
                currentStartLength -= indentLength;
            }
            var newLine = (lastNodeType != "text" &&
                lastNodeType != "cdata"
                && lastNodeType != "entityreference") ? `\n` : sf.String.Empty;
            var spaces = (lastNodeType != "text" &&
                lastNodeType != "entityreference") ? " ".repeat(currentStartLength) : sf.String.Empty;
            sb.Append(newLine
                + spaces
                + "</"
                + node["name"]
                + ">");
            lastNodeType = node.NodeType;
        }
    }
    //close tag after all child nodes
    else
    {
        sb.Append(" />");
    }

    return;

}
