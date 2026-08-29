namespace XmlFormatter.Tests;

/// <summary>
/// Deterministic synthetic documents for code paths the Sample/ fixtures do not reach.
/// </summary>
internal static class SyntheticXml
{
    // Wide-sibling traversal: the widest Sample/ fixture holds only 17 children.
    public static string Orders(int records)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("""<?xml version="1.0" encoding="utf-8"?>""");
        sb.Append("""<orders xmlns="http://example.com/orders" generated="true">""");
        for (var i = 0; i < records; i++)
        {
            // Index-derived, never random: WideFanOutTests pins the output by hash.
            var status = (i % 3) switch { 0 => "open", 1 => "shipped", _ => "cancelled" };
            sb.Append($"""<order id="ORD-{i:D6}" status="{status}" priority="{i % 5 + 1}">""")
              .Append($"""<customer name="Customer {i}" email="user{i}@example.com"/>""")
              .Append($"""<items><item sku="SKU-{i}-A" qty="{i % 9 + 1}" price="{i % 900 + 5}.99"/>""")
              .Append($"""<item sku="SKU-{i}-B" qty="1"/></items>""")
              .Append($"<!-- generated record {i} -->")
              .Append($"<notes><![CDATA[free form note for order {i}]]></notes>")
              .Append($"""<shipping><address line1="{i} Main Street" city="Springfield" zip="{10000 + i}"/></shipping>""")
              .Append("</order>");
        }
        sb.Append("</orders>");
        return sb.ToString();
    }
}
