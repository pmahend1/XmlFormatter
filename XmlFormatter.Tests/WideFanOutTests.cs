using System.Security.Cryptography;
using System.Text;
using XmlFormatter;

namespace XmlFormatter.Tests;

/// <summary>
/// Correctness coverage for wide-sibling traversal in PrintNode.
///
/// The widest parent in any Sample/ fixture holds 17 children, so the fixture suite
/// barely exercises the child-node loop. This formats a document with 5000 siblings
/// under one root and pins the result by hash - a baseline file would be ~3 MB per
/// option set, which is not worth committing.
///
/// Note what this does and does not do: it is an *output* check. It cannot detect a
/// performance regression - the O(n^2) traversal this replaced produced byte-identical
/// output. Guarding against that needs a scaling measurement, which belongs with the
/// perf tooling, not here.
///
/// The expected hashes were recorded from the pre-fix formatter at 00df332, so they
/// verify the rewritten traversal against the original behaviour rather than against
/// a snapshot of itself.
/// </summary>
public class WideFanOutTests
{
    private const int Records = 5000;

    [Theory]
    [InlineData("default", "81e2517105039d1d1edb63af557b654ac5f31e09c2a4fea54804fb9d0ed0b5a0")]
    [InlineData("sample-program", "c774471edef2bf943a0bf45effde4e730b30a51c9b0c498334fadb3c7a0d5947")]
    [InlineData("blank-lines", "9d2db85c00c0fdd83e4c43e99f22931df587c01b084a2b5cfa7e766582894664")]
    public void Wide_document_output_is_unchanged(string optionSet, string expectedSha256)
    {
        var xml = SyntheticXml.Orders(Records);

        var formatted = new Formatter().Format(xml, OptionSets.ByName(optionSet));

        Assert.Equal(expectedSha256, Sha256(formatted));
    }

    [Fact]
    public void Synthetic_document_really_is_wide()
    {
        // Guards the premise: if the generator changes shape, the test above stops
        // covering what it claims to cover.
        var doc = new System.Xml.XmlDocument();
        doc.LoadXml(SyntheticXml.Orders(Records));

        var siblings = 0;
        for (var child = doc.DocumentElement!.FirstChild; child is not null; child = child.NextSibling)
        {
            siblings++;
        }

        Assert.Equal(Records, siblings);
    }

    private static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
