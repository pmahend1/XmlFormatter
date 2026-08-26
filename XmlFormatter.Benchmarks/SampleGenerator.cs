using System.Text;

namespace XmlFormatter.Benchmarks;

/// <summary>
/// Builds the perf corpus. Every document is derived from its index - there is no RNG, so
/// two runs on two machines produce byte-identical files and timings stay comparable.
///
/// Deliberately separate from XmlFormatter.Tests.SyntheticXml: that one is pinned by a
/// recorded hash and must never change, this one is free to grow new shapes.
/// </summary>
internal static class SampleGenerator
{
    private const string XmlDeclaration = """<?xml version="1.0" encoding="utf-8"?>""";

    /// <summary>The size ladder. Each step is roughly 4-5x the last, which is what makes
    /// the scaling column readable - work should rise by about the same factor as size.</summary>
    private static readonly (string Name, int Records)[] Ladder =
    [
        ("01-tiny.xml", 2),
        ("02-small.xml", 120),
        ("03-medium.xml", 1_200),
        ("04-large.xml", 6_000),
        ("05-xlarge.xml", 25_000),
    ];

    /// <summary>
    /// Fixed-size documents that each lean on one code path, so a regression can be
    /// attributed rather than just observed. Sized to land near the medium rung.
    /// </summary>
    private static readonly (string Name, Func<string> Build)[] Shapes =
    [
        /*
         * 100 levels, not 800: PrintNode recurses once per level and blows the stack a
         * little past 797 on macOS - and a .NET exe on Windows gets an eighth of that
         * stack, so anything deep enough to be interesting here is not portable.
         * Depth is exercised by repeating a safe chain, not by one very deep one.
         */
        ("deep.xml", () => Deep(depth: 100, chains: 200, leavesPerLevel: 2)),
        ("attributes.xml", () => AttributeHeavy(elements: 4_000, attributesEach: 20)),
        ("comments.xml", () => CommentHeavy(records: 4_000)),
        ("unicode.xml", () => Unicode(records: 4_000)),
        ("text.xml", () => TextHeavy(blocks: 400, charsEach: 2_000)),
    ];

    public static void GenerateAll()
    {
        Directory.CreateDirectory(PerfPaths.SampleDir);
        Directory.CreateDirectory(PerfPaths.FormattedDir);
        Directory.CreateDirectory(PerfPaths.ShapeDir);

        Console.WriteLine($"ladder -> {Relative(PerfPaths.SampleDir)}");
        var formatter = new Formatter();

        foreach (var (name, records) in Ladder)
        {
            var minified = Orders(records);
            WriteSample(Path.Combine(PerfPaths.SampleDir, name), minified);
            ReportSample(name, minified.Length, $"{records} records");

            /*
             * The same document after one formatting pass. Re-formatting an already
             * formatted file is what an editor actually does, and with PreserveNewLines
             * on it carries whitespace nodes the minified version never had.
             */
            WriteSample(Path.Combine(PerfPaths.FormattedDir, name), formatter.Format(minified));
        }

        Console.WriteLine($"\nformatted ladder -> {Relative(PerfPaths.FormattedDir)}");
        Console.WriteLine($"\nshapes -> {Relative(PerfPaths.ShapeDir)}");

        foreach (var (name, build) in Shapes)
        {
            var document = build();
            WriteSample(Path.Combine(PerfPaths.ShapeDir, name), document);
            ReportSample(name, document.Length);
        }

        Console.WriteLine("\nAll of the above is generated and gitignored - regenerate with `generate`.");
    }

    /// <summary>An order list: attributes, nested elements, a comment and CDATA per record.</summary>
    public static string Orders(int records)
    {
        var xml = new StringBuilder(XmlDeclaration);
        xml.Append("""<orders xmlns="http://example.com/orders" generated="true">""");

        for (var record = 0; record < records; record++)
        {
            var status = (record % 3) switch { 0 => "open", 1 => "shipped", _ => "cancelled" };

            xml.Append($"""<order id="ORD-{record:D6}" status="{status}" priority="{record % 5 + 1}">""")
               .Append($"""<customer name="Customer {record}" email="user{record}@example.com"/>""")
               .Append($"""<items><item sku="SKU-{record}-A" qty="{record % 9 + 1}" price="{record % 900 + 5}.99"/>""")
               .Append($"""<item sku="SKU-{record}-B" qty="1"/></items>""")
               .Append($"<!-- generated record {record} -->")
               .Append($"<notes><![CDATA[free form note for order {record}]]></notes>")
               .Append($"""<shipping><address line1="{record} Main Street" city="Springfield" zip="{10_000 + record}"/></shipping>""")
               .Append("</order>");
        }

        return xml.Append("</orders>").ToString();
    }

    /// <summary>
    /// <paramref name="chains"/> sibling chains, each <paramref name="depth"/> elements deep.
    ///
    /// Measures what recursion costs per level. It deliberately does not measure how deep
    /// the formatter can go before the stack runs out - see the note on the shape list.
    /// </summary>
    public static string Deep(int depth, int chains, int leavesPerLevel)
    {
        var xml = new StringBuilder(XmlDeclaration);
        xml.Append("<tree>");

        for (var chain = 0; chain < chains; chain++)
        {
            for (var level = 0; level < depth; level++)
            {
                xml.Append($"""<level n="{level}" path="/chain{chain}/level{level}">""");

                for (var leaf = 0; leaf < leavesPerLevel; leaf++)
                {
                    xml.Append($"""<leaf id="{chain}-{level}-{leaf}" value="{level * leavesPerLevel + leaf}"/>""");
                }
            }

            for (var level = 0; level < depth; level++)
            {
                xml.Append("</level>");
            }
        }

        return xml.Append("</tree>").ToString();
    }

    /// <summary>
    /// Many attributes per element - the path through the attribute wrapping rules,
    /// AttributesInNewlineThreshold and the wildcard exception matching.
    /// </summary>
    public static string AttributeHeavy(int elements, int attributesEach)
    {
        var xml = new StringBuilder(XmlDeclaration);
        xml.Append("<config>");

        for (var element = 0; element < elements; element++)
        {
            xml.Append("<entry").Append(Attribute("index", element));

            for (var attribute = 0; attribute < attributesEach; attribute++)
            {
                xml.Append(Attribute($"attr{attribute}", $"value-{element}-{attribute}"));
            }

            xml.Append("/>");
        }

        return xml.Append("</config>").ToString();
    }

    private static string Attribute(string name, object value) => $" {name}=\"{value}\"";

    /// <summary>Comment-heavy - the comment regexes and PreserveCommentPlacement.</summary>
    public static string CommentHeavy(int records)
    {
        var xml = new StringBuilder(XmlDeclaration);
        xml.Append("<document>");

        for (var record = 0; record < records; record++)
        {
            xml.Append($"<!-- section {record} -->")
               .Append($"""<section id="{record}">""")
               .Append($"<!--   leading comment with irregular spacing for {record}   -->")
               .Append($"<value>{record}</value>")
               .Append($"<!-- multi\n     line\n     comment {record} -->")
               .Append("</section>")
               .Append($"<!-- trailing {record} -->");
        }

        return xml.Append("</document>").ToString();
    }

    /// <summary>
    /// Non-ASCII content: CJK, combining marks, RTL and astral-plane emoji. The emoji are
    /// surrogate pairs, which is the case the custom escaping in the formatter exists for.
    /// </summary>
    public static string Unicode(int records)
    {
        /*
         * The combining marks and the emoji are escapes on purpose. Typed as literals,
         * an editor or a careless rewrite normalises them to their precomposed forms and
         * the combining-mark case quietly stops being tested - the document still looks
         * identical and is simply 4 bytes per accent shorter. This already happened once.
         */
        string[] scripts =
        [
            "中文文本内容",
            "cafe\u0301 na\u0308ive re\u0301sume\u0301",
            "שלום עולם",
            "\U0001F600\U0001F680\U0001F30D",
            "Αθήνα πόλη",
        ];

        var xml = new StringBuilder(XmlDeclaration);
        xml.Append("<catalogue>");

        for (var record = 0; record < records; record++)
        {
            var text = scripts[record % scripts.Length];

            xml.Append($"""<item id="{record}" label="{text}">""")
               .Append($"<title>{text}</title>")
               .Append($"<body>{text} {text} {text}</body>")
               .Append($"<note><![CDATA[{text} & <raw> markup]]></note>")
               .Append("</item>");
        }

        return xml.Append("</catalogue>").ToString();
    }

    /// <summary>
    /// Few nodes, enormous text and CDATA payloads - separates "cost per node" from
    /// "cost per byte", which the other shapes conflate.
    /// </summary>
    public static string TextHeavy(int blocks, int charsEach)
    {
        var xml = new StringBuilder(XmlDeclaration);
        xml.Append("<corpus>");

        for (var block = 0; block < blocks; block++)
        {
            var body = Filler(block, charsEach);

            xml.Append($"""<block id="{block}">""")
               .Append($"<prose>{body}</prose>")
               .Append($"<verbatim><![CDATA[{body}]]></verbatim>")
               .Append("</block>");
        }

        return xml.Append("</corpus>").ToString();
    }

    /// <summary>Deterministic filler text, offset by <paramref name="seed"/> so blocks differ.</summary>
    private static string Filler(int seed, int length)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz ";

        var text = new StringBuilder(length);

        for (var position = 0; position < length; position++)
        {
            text.Append(alphabet[(seed + position) % alphabet.Length]);
        }

        return text.ToString();
    }

    private static void WriteSample(string path, string content) => File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

    private static void ReportSample(string name, int bytes, string detail = "") => Console.WriteLine($"  {name,-18}{bytes / 1024.0,9:F1} KB  {detail}");

    private static string Relative(string path) => Path.GetRelativePath(PerfPaths.RepoRoot, path);
}
