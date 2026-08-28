using System.Text;
using XmlFormatter;

namespace XmlFormatter.Tests;

/// <summary>
/// Characterization tests: every fixture in Sample/ is formatted under each option set and
/// compared against a committed baseline file. These do not assert that the output is
/// *correct* - they assert it has not *changed*. That is what makes a refactor of
/// PrintNode (which has no other test coverage) safe to review.
///
/// To re-baseline after an intentional formatting change, delete the affected files in
/// Baseline/ and run the suite once - missing baselines are written, not failed - then read
/// the resulting diff carefully before committing it.
/// </summary>
public class FixtureFormattingTests
{
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

        var actual = new Formatter().Format(input, OptionSets.ByName(optionSet));

        var baselinePath = Path.Combine(TestPaths.BaselineDir, $"{fixture}.{optionSet}.txt");
        if (!File.Exists(baselinePath))
        {
            Directory.CreateDirectory(TestPaths.BaselineDir);
            File.WriteAllText(baselinePath, actual);
            return;                 // first run records the baseline
        }

        Assert.Equal(File.ReadAllText(baselinePath), actual);
    }

    [Fact]
    public void Every_fixture_is_covered()
    {
        // Guards against a fixture being added to Sample/ without picking up coverage.
        Assert.Equal(23, TestPaths.Fixtures().Count);
    }
}
