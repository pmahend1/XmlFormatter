namespace XmlFormatter.Benchmarks;

// Names match XmlFormatter.Tests.OptionSets, so a number here and a baseline there agree.
internal static class BenchOptions
{
    public static readonly IReadOnlyDictionary<string, Options> All = new Dictionary<string, Options>
    {
        ["default"] = new Options(),

        // The editor's real configuration: PreserveNewLines roughly doubles the sibling count
        // on already-formatted input.
        ["preserve-newlines"] = new Options
        {
            PreserveNewLines = true,
            PreserveCommentPlacement = true,
            WrapCommentTextWithSpaces = true,
        },

        ["blank-lines"] = new Options
        {
            AddEmptyLineBetweenElements = true,
            PreserveNewLines = true,
        },
    };

    public static Options Resolve(string name)
    {
        return All.TryGetValue(name, out var options) ?
               options :
               throw new ArgumentException($"Unknown option set '{name}'. Known: {string.Join(", ", All.Keys)}");
    }
}
