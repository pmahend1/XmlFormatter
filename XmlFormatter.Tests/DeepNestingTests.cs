namespace XmlFormatter.Tests;

/// <summary>
/// Nesting depth is bounded by the heap, not the call stack.
///
/// <c>PrintNode</c> used to recurse once per level. At roughly a kilobyte of frame per level
/// that exhausted the ~1 MB stack a thread-pool thread gets somewhere between depth 800 and
/// 900, and XmlFormatter.CommandLine does its work on exactly such a thread - it awaits stdin
/// and resumes off the main thread. The result was a hard stack overflow: the process died
/// with no output and nothing for the caller to report.
///
/// <b>A regression here fails loudly, not gracefully.</b> A .NET stack overflow cannot be
/// caught, so if the traversal ever goes recursive again these tests will take the whole test
/// host down rather than report a failed assertion. That is the bug's real signature.
/// </summary>
public class DeepNestingTests
{
    /// <summary>
    /// Past the old ~850 ceiling while still indenting. Indent width grows with depth, so
    /// output is quadratic in depth on this path - 1500 levels is already ~4.5 MB.
    /// </summary>
    [Theory]
    [InlineData(900)]
    [InlineData(1500)]
    public void An_indented_document_formats_past_the_old_stack_ceiling(int depth)
    {
        var formatted = TestFormatter.Format(SyntheticXml.Chain(depth), TestOptions.NoDeclaration);

        Assert.Equal(depth, CountOccurrences(formatted, "</level>"));
        Assert.Contains("leaf", formatted);
    }

    /// <summary>
    /// Depth far beyond anything a stack could hold. Indentation is what makes deep output
    /// quadratic, so dropping it to zero keeps this linear and lets the depth be absurd - the
    /// point being that the traversal no longer has a depth limit of its own.
    /// </summary>
    [Fact]
    public void A_hundred_thousand_levels_format_without_running_out_of_stack()
    {
        var xml = SyntheticXml.Chain(depth: 100_000);

        var formatted = TestFormatter.Format(xml, TestOptions.NoDeclaration with { IndentLength = 0 });

        Assert.Equal(100_000, CountOccurrences(formatted, "</level>"));
    }

    /// <summary>
    /// The same depth under PreserveNewLines, which keeps the whitespace nodes the traversal
    /// would otherwise discard and so walks a different set of branches on the way down.
    /// </summary>
    [Fact]
    public void Deep_nesting_survives_preserve_new_lines_too()
    {
        var xml = SyntheticXml.Chain(depth: 50_000);

        var formatted = TestFormatter.Format(xml,
                                             TestOptions.NoDeclaration with { IndentLength = 0, PreserveNewLines = true });

        Assert.Equal(50_000, CountOccurrences(formatted, "</level>"));
    }

    /// <summary>
    /// PreserveCommentPlacement walks the whole document once before the traversal does, to read
    /// the comment placements out of whitespace that is then stepped over rather than deleted.
    /// That walk has a stack of its own to keep, and the same depth to keep it over.
    /// </summary>
    [Fact]
    public void Deep_nesting_survives_preserve_comment_placement_too()
    {
        var xml = SyntheticXml.Chain(depth: 50_000);

        var formatted = TestFormatter.Format(xml,
                                             TestOptions.NoDeclaration with { IndentLength = 0, PreserveCommentPlacement = true });

        Assert.Equal(50_000, CountOccurrences(formatted, "</level>"));
    }

    /// <summary>Minimize goes through the DOM writer rather than the traversal, and always could.</summary>
    [Fact]
    public void Minimize_handles_the_same_depth()
    {
        var minimized = new Formatter().Minimize(SyntheticXml.Chain(depth: 100_000));

        Assert.Equal(100_000, CountOccurrences(minimized, "</level>"));
    }

    /// <summary>
    /// The depth-1 case in full, so the deep tests above are read against a known shape rather
    /// than only counting closing tags. The declaration is the generator's own - NoDeclaration
    /// suppresses adding one, it does not drop one that is already there.
    /// </summary>
    [Fact]
    public void A_single_level_chain_is_the_shallow_control()
    {
        var formatted = TestFormatter.Format(SyntheticXml.Chain(depth: 1), TestOptions.NoDeclaration);

        Assert.Equal("""
                     <?xml version="1.0" encoding="utf-8"?>
                     <level depth="0">leaf</level>
                     """,
                     formatted);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var at = text.IndexOf(value, StringComparison.Ordinal); at >= 0; at = text.IndexOf(value, at + value.Length, StringComparison.Ordinal))
        {
            count++;
        }
        return count;
    }
}
