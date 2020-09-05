//#region imports
import * as fs from "fs";
import * as xmljs from "xml-js";
import * as sf from "typescript-string-operations";
import { DOMParser } from 'xmldom'
import { debuglog } from "util";
//import * as xmldom from "xmldom";
//#endregion

//#region declarations
let content = "";
let currentStartLength = 0;
let currentAttributeSpace = 0;
let lastNodeType = "";

//#endregion

fs.readFile("./SampleFiles/Sample2.xml", function (err, data)
{
    if (err)
    {
        console.log(err);
        return;
    } else if (data)
    {
        content = data.toString();

        var convert = xmljs;
        //var result = convert.xml2json(content, { compact: false, spaces: 4 });
        var dom = new DOMParser();
        const doc = dom.parseFromString(content);
        if (doc)
        {
            console.log(doc);


            //var obj = JSON.parse(result);
            var formatted = FormatXMLDocument(doc, 4);
            console.log(formatted);

        }
    }

})

function FormatXMLDocument(xmlDocument: any, indentLength: number)
{
    var sb = new sf.StringBuilder();

    var first = xmlDocument.childNodes[0];
    // var declaration = xmlDocument.
    // if (declaration)
    // {
    //     var decAttr = declaration["attributes"];

    //     sb.Append("<?xml ");
    //     for (const [key, value] of Object.entries(decAttr))
    //     {
    //         sb.AppendFormat(`${key}="${value}"`);
    //     }
    //     sb.Append(`?>\n`);
    // }
    // if (xml.DocumentType !== null)
    // {
    //     sb.Append(xml.DocumentType.OuterXml + SymbolConstants.Newline);
    // }
    var root = xmlDocument.documentElement;

    console.log(root.documenttype);
    lastNodeType = root.constructor.name;

    PrintNode(root, sb, indentLength);
    return sb.ToString();
}


function PrintNode(node: any, sb: sf.StringBuilder, indentLength: number)
{
    var prevNode = lastNodeType;
    lastNodeType = node.constructor.name.toString();
    switch (lastNodeType)
    {
        case "Attr":
            sb.AppendFormat(`${node.name}=${node.value}`);
            return;
        case "CDATASection":
            var newLine = (prevNode === "Text") ? sf.String.Empty : `\n`;
            var spaces = (prevNode === "Text") ? sf.String.Empty : " ".repeat(currentStartLength);
            sb.AppendFormat(`${newLine}${spaces}<![CDATA[${node.nodeValue}]]>`)
            return;
        case "Comment":
            sb.AppendFormat(`${" ".repeat(currentStartLength)}<!--${node.nodeValue}-->`);
            return;
        case "DocumentType":
            //sb.Append(SymbolConstants.DocTypeStart + SymbolConstants.Space + SymbolConstants.DocTypeEnd(node.Value));
            sb.AppendFormat(`<!DOCTYPE  [${node.nodeValue}]`);
            return;
        case "Element":
            //Done
            break;

        case "EndElement":
            break;

        case "EndEntity":
            break;

        case "Entity":
            break;

        case "EntityReference":
            sb.Append(node.nodeValue);
            return;

        case "None":
            break;

        case "Notation":
            break;

        case "ProcessingInstruction":
            sb.AppendFormat(`<?${node.nodeName} ${node.nodeValue}?>`);
            return;

        case "SignificantWhitespace":
            break;

        case "Text":
            const str: string = node.nodeValue;
            if (!sf.String.IsNullOrWhiteSpace(str))
            {
                sb.Append(str);
            }

            return;

        case "Whitespace":
            break;

        case "Declaration":
            //done
            break;

        default:
            break;
    }

    //print start tag
    var space = lastNodeType ==="Element" && node.previousSibling.constructor.name !== "Text" ? " ".repeat(currentStartLength) : sf.String.Empty;
    // var space = " ".repeat(currentStartLength);
    sb.AppendFormat(`${space}<${node.nodeName}`);

    //print attributes
    var attributes: Attr[] = node.attributes;
    var closed = false;
    if (attributes)
    {
        if (attributes.length > 0)
        {
            sb.Append(" ");
            var nodeName: string = node.nodeName;
            currentAttributeSpace = currentStartLength + nodeName.length + 2;
            for (let i = 0; i < attributes.length; i++)
            {
                //const attributeName = Object.keys(attributes)[i];
                const attribute = attributes[i];

                var isLast = (i === attributes.length - 1);
                var newLine = isLast ? sf.String.Empty : `\n`;
                sb.AppendFormat(`${attribute.name}="${attribute.value}"${newLine}`);
                if (node.nodeName === "more")
                {
                    console.log("dil maange more");
                }
                if (isLast && node.childNodes !== null)
                {
                    closed = true;
                    sb.Append(">");
                }

                else if (!isLast)
                    sb.Append(" ".repeat(currentAttributeSpace));


            }
        }



    }
    //if (!node.nodeValue?.endsWith("/>"))
    // {

    if (!closed)
    {
        sb.Append(">");
    }


    // }




    //print nodes
    var childNodes: Node[] = node.childNodes;
    if (childNodes)
    {
        if (childNodes.length > 0)
        {
            var prevStartLength = currentStartLength;
            var yes = false;

            for (let index = 0; index < childNodes.length; index++)
            {
                const element = childNodes[index];
                if (element.constructor.name === "Element")
                {
                    yes = true;
                    break;
                }


            }
            if (lastNodeType == "Element" && yes)
            {
                prevStartLength = currentStartLength;
                currentStartLength += indentLength;
            }

            for (let j = 0; j < childNodes.length; j++)
            {
                const currentChild = childNodes[j];
                var nodeType: string = currentChild.constructor.name;
                if (nodeType !== 'Text' && nodeType !== 'CDATASection' && nodeType !== 'EntityReference'
                    && node.previousSibling.constructor.name !== 'Text'
                )
                {
                    sb.Append("\n");
                }
                //
                PrintNode(currentChild, sb, indentLength);
            }
            if (node.constructor.name !== "Comment"
                && node.constructor.name !== "CDATASection"
                && node.constructor.name !== "DocumentType"
                && node.constructor.name !== "Text")
            {
                if (currentStartLength >= indentLength
                    && lastNodeType !== "Text"
                    && lastNodeType !== "CDATASection"
                    && lastNodeType !== "DocumentType"
                    && lastNodeType !== "EntityReference"
                )
                {
                    currentStartLength -= indentLength;
                }
                var newLine = (lastNodeType !== "Text" &&
                    lastNodeType !== "CDATASection"
                    && lastNodeType !== "EntityReference") ? `\n` : sf.String.Empty;
                var spaces = (lastNodeType !== "Text" && lastNodeType !== "EntityReference") ? " ".repeat(currentStartLength) : sf.String.Empty;
                sb.Append(newLine
                    + spaces
                    + "</"
                    + node.nodeName
                    + ">");
                lastNodeType = node.constructor.name;
            }

        }


    }
    //close tag after all child nodes
    else
    {
        sb.Append(" />");
    }

    return;
}
