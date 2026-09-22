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
    public void Formatting_an_already_formatted_fixture_changes_nothing(string fixture, string optionSet)
    {
        var options = OptionSets.ByName(optionSet);
        var input = File.ReadAllText(Path.Combine(TestPaths.FixtureDir, fixture));

        var once = new Formatter().Format(input, options);
        var twice = new Formatter().Format(once, options);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Every_fixture_can_be_reformatted()
    {
        // The formatter must be able to read its own output back - XMLFile8 could not until the
        // DOCTYPE SYSTEM keyword was emitted, and it is the reason this test names the offenders.
        var unparseable = TestPaths.Fixtures()
                                   .Select(path => Path.GetFileName(path))
                                   .Where(SecondFormatThrows)
                                   .ToList();

        Assert.Empty(unparseable);
    }

    // The causes, each on the smallest document that shows it. The fixture sweep above catches
    // a regression; these say which one it was.

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

    /// <summary>
    /// After text the separator opens no line, so the indent landed in the text node's own
    /// character data and came back as text on the next pass - one space beside a comment
    /// widened by IndentLength on every format, without ever settling.
    /// </summary>
    [Fact]
    public void A_comment_beside_text_gains_no_indent_inside_the_text()
    {
        const string mixed = "<r><p>a <!-- c --> b</p></r>";

        var once = TestFormatter.Format(mixed, TestOptions.NoDeclaration);
        var twice = TestFormatter.Format(once, TestOptions.NoDeclaration);

        Assert.Equal("<r>\n    <p>a <!-- c --> b</p>\n</r>", once);
        Assert.Equal(once, twice);
    }

    [Theory]
    // The comment last in the element, so nothing follows it to show the drift.
    [InlineData("<r><p>a <!-- c --></p></r>")]
    // Two of them, which widened independently and so grew twice as fast.
    [InlineData("<r><p>a <!--x--> b <!--y--> c</p></r>")]
    // An element before the comment still opens a line for it - this one must keep its indent.
    [InlineData("<r><p><a/><!-- c --> b</p></r>")]
    public void A_comment_in_mixed_content_settles_after_one_pass(string mixed)
    {
        foreach (var options in new[] { TestOptions.NoDeclaration,
                                        TestOptions.NoDeclaration with { PreserveNewLines = true },
                                        TestOptions.NoDeclaration with { PreserveCommentPlacement = true } })
        {
            var once = TestFormatter.Format(mixed, options);

            Assert.Equal(once, TestFormatter.Format(once, options));
        }
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
