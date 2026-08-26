namespace XmlFormatter.Benchmarks;

/// <summary>
/// The option sets the harness measures under. Names match XmlFormatter.Tests.OptionSets
/// where they overlap, so a number here and a baseline there refer to the same settings.
/// </summary>
internal static class BenchOptions
{
    public static readonly IReadOnlyDictionary<string, Options> All = new Dictionary<string, Options>
    {
        // Library defaults. Whitespace nodes are dropped at load, so a minified and an
        // indented copy of the same document produce the same DOM and the same timing.
        ["default"] = new Options(),

        /*
         * The editor's real configuration. PreserveNewLines keeps whitespace nodes, which
         * roughly doubles the sibling count on already-formatted input - the case the
         * minified-only corpus used to miss entirely.
         */
        ["preserve-newlines"] = new Options
        {
            PreserveNewLines = true,
            PreserveCommentPlacement = true,
            WrapCommentTextWithSpaces = true,
        },

        // The widest-output configuration: a blank line between every pair of elements.
        ["blank-lines"] = new Options
        {
            AddEmptyLineBetweenElements = true,
            PreserveNewLines = true,
        },
    };

    public static Options Resolve(string name)
    {
        return All.TryGetValue(name, out var options)
            ? options
            : throw new ArgumentException($"Unknown option set '{name}'. Known: {string.Join(", ", All.Keys)}");
    }
}