//#region imports
import * as fs from "fs";
import * as xmljs from "xml-js";
import * as sf from "typescript-string-operations";
//#endregion

//#region declarations
let content = "";
var stringBuilder = new sf.StringBuilder();
let currentStartLength = 0;
let currentAttributeSpace = 0;
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
            var formatted = FormatXML(obj);
        }
    }

})




function FormatXML(jsonObj: any)
{
    var declaration = jsonObj["declaration"];
    if (declaration)
    {
        var decAttr = declaration["attributes"];

        stringBuilder.Append("<?xml ");
        for (const [key, value] of Object.entries(decAttr))
        {
            stringBuilder.AppendFormat(`${key}="${value}" `);
        }
        stringBuilder.Append(`>\n`);
    }
    var elements: any[] = jsonObj["elements"];

    elements.forEach(ele =>
    {

        var type = ele["type"];
        let name: string = ele["name"];

        switch (type)
        {
            case "element":
                stringBuilder.AppendFormat("<{0}", name);

                break;

            default:
                break;
        }

        var attributes: any = ele["attributes"];
        if (attributes)
        {

            stringBuilder.Append(" ");
            currentAttributeSpace = currentStartLength + name.length + 2;
            for (let i = 0; i < Object.keys(attributes).length; i++)
            {
                const attributeName = Object.keys(attributes)[i];
                const attributeValue = Object.values(attributes)[i];
                var isLast = (i === Object.keys(attributes).length - 1);
                var newLine = isLast ? sf.String.Empty : `\n`;
                stringBuilder.AppendFormat(`${attributeName}="${attributeValue}"${newLine}`);
                if (isLast && ele["elements"])
                    stringBuilder.Append(">");
                else if (!isLast)
                    stringBuilder.Append(" ".repeat(currentAttributeSpace));
            }

        }

    });

    console.log(stringBuilder.ToString());

}

