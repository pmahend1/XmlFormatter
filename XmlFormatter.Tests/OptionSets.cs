namespace XmlFormatter.Tests;

// One per branch of the child-node loop in PrintNode.
internal static class OptionSets
{
    private static Options Default => new();

    // What Sample/Program.cs uses.
    private static Options SampleProgram => new()
    {
        PreserveNewLines = true,
        PreserveCommentPlacement = true,
        WrapCommentTextWithSpaces = true,
    };

    // The only caller that still needs a child count.
    private static Options BlankLinesBetweenElements => new()
    {
        AddEmptyLineBetweenElements = true,
        PreserveNewLines = true,
    };

    public static IEnumerable<(string Name, Options Options)> All =>
    [
        ("default", Default),
        ("sample-program", SampleProgram),
        ("blank-lines", BlankLinesBetweenElements),
    ];

    public static Options ByName(string name) =>  All.First(pair => pair.Name == name).Options;
}
