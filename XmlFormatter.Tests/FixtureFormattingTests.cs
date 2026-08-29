namespace XmlFormatter.Tests;

/// <summary>
/// Characterization: asserts output has not *changed*, not that it is correct.
///
/// To re-baseline, delete the affected files in Baseline/ and run once - missing baselines
/// are written rather than failed - then read the diff before committing.
///
/// Both sides are compared with LF endings. The formatter emits Environment.NewLine, so
/// without this the committed baselines would only match on the platform that recorded them.
/// Line endings are covered deliberately by <see cref="LineEndingTests"/> instead of
/// incidentally here, where a CRLF diff would surface as 23 unreadable fixture failures.
/// </summary>
public class FixtureFormattingTests
{
    private static string WithLfEndings(string text) => text.Replace("\r\n", "\n");

    public static TheoryData<string, string> Cases()
    {
        var data = new TheoryData<string, string>();
        foreach (var path in TestPaths.Fixtures())
        {
            foreach (var (name, _) in OptionSets.All)
            {
                data.Add(Path.GetFileName(path), name);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Formatting_matches_baseline(string fixture, string optionSet)
    {
        var input = File.ReadAllText(Path.Combine(TestPaths.FixtureDir, fixture));

        var actual = WithLfEndings(new Formatter().Format(input, OptionSets.ByName(optionSet)));

        var baselinePath = Path.Combine(TestPaths.BaselineDir, $"{fixture}.{optionSet}.txt");
        if (!File.Exists(baselinePath))
        {
            Directory.CreateDirectory(TestPaths.BaselineDir);
            File.WriteAllText(baselinePath, actual);
            return; // first run records the baseline
        }

        Assert.Equal(WithLfEndings(File.ReadAllText(baselinePath)), actual);
    }

    [Fact]
    public void Every_fixture_is_covered()
    {
        // Guards against a fixture being added to Sample/ without picking up coverage.
        Assert.Equal(23, TestPaths.Fixtures().Count);
    }
}
