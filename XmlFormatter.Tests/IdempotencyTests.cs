using System.Xml;

namespace XmlFormatter.Tests;

/// <summary>
/// Formatting an already-formatted document must change nothing. The extension formats on save
/// and on demand, so the second press has to be a no-op - and it was not: the indentation the
/// formatter emitted around comments and CDATA came back as a whitespace node that it emitted
/// again, so the output grew a line per pass and never settled.
/// </summary>
public class IdempotencyTests
{
    /*
     * XMLFile8's output does not parse: its DOCTYPE loses the SYSTEM keyword on the way out, so
     * there is no second pass to compare. That is the documented DTD limitation, pinned by
     * DocumentTypeTests.A_system_identifier_should_keep_its_keyword. Only_one_fixture_cannot_be_reformatted
     * fails if that ever stops being the only one, so this exclusion cannot quietly widen.
     */
    private const string OutputDoesNotParse = "XMLFile8.xml";

    public static TheoryData<string, string> Cases()
    {
        var data = new TheoryData<string, string>();
        foreach (var path in TestPaths.Fixtures())
        {
            var fixture = Path.GetFileName(path);
            if (fixture == OutputDoesNotParse)
            {
                continue;
            }

            foreach (var (name, _) in OptionSets.All)
            {
                data.Add(fixture, name);
            }
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Formatting_an_already_formatted_fixture_changes_nothing(string fixture, string optionSet)
    {
        var options = OptionSets.ByName(optionSet);
        var input = File.ReadAllText(Path.Combine(TestPaths.FixtureDir, fixture));

        var once = new Formatter().Format(input, options);
        var twice = new Formatter().Format(once, options);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Only_one_fixture_cannot_be_reformatted()
    {
        var unparseable = TestPaths.Fixtures()
                                   .Select(path => Path.GetFileName(path))
                                   .Where(SecondFormatThrows)
                                   .ToList();

        Assert.Equal(new[] { OutputDoesNotParse }, unparseable);
    }

    /*
     * The three causes, each on the smallest document that shows it. The fixture sweep above
     * catches a regression; these say which one it was.
     */

    [Fact]
    public void A_generated_declaration_is_written_as_it_will_be_read_back()
    {
        var once = TestFormatter.Format("<r/>", new Options());

        Assert.Equal(once, TestFormatter.Format(once, new Options()));
    }

    [Fact]
    public void Indentation_around_a_comment_does_not_accumulate()
    {
        var options = TestOptions.NoDeclaration with { PreserveNewLines = true };

        var once = TestFormatter.Format("<r>\n  <!--why-->\n  <a/>\n</r>", options);

        Assert.Equal(once, TestFormatter.Format(once, options));
    }

    [Fact]
    public void A_text_body_that_starts_inline_does_not_gain_trailing_spaces()
    {
        var once = TestFormatter.Format("<r><d>first line,\n      second line.</d></r>", TestOptions.NoDeclaration);

        Assert.Equal(once, TestFormatter.Format(once, TestOptions.NoDeclaration));
    }

    [Fact]
    public void Whitespace_that_is_an_elements_only_content_does_not_accumulate()
    {
        var options = TestOptions.NoDeclaration with { PreserveNewLines = true };

        var once = TestFormatter.Format("<r>\n  </r>", options);

        Assert.Equal(once, TestFormatter.Format(once, options));
    }

    private static bool SecondFormatThrows(string fixture)
    {
        var options = OptionSets.ByName("default");
        var input = File.ReadAllText(Path.Combine(TestPaths.FixtureDir, fixture));

        try
        {
            new Formatter().Format(new Formatter().Format(input, options), options);
            return false;
        }
        catch (XmlException)
        {
            return true;
        }
    }
}
