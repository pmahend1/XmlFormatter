import fs = require("fs");
let content = "";
fs.readFile("./SampleFiles/Sample2.xml", function(err,data){
console.log(data.toString());
content = data.toString();
})

//FormatXML(content);
var parser = require('fast-xml-parser');


var jsonObj = parser.parse(content);

console.log(jsonObj.toString());

function FormatXML(xmlstring: string) {

    var xmlWriterModule = require("xml-writer");
    var xw = new xmlWriterModule();
    xw.startDocument().startElement('root').writeAttribute('foo', 'value').writeElement('tag', 'Some content');

	console.log(xw.toString());
}

