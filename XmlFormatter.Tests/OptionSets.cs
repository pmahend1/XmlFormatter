using XmlFormatter;

namespace XmlFormatter.Tests;

/// <summary>
/// The option sets the fixture tests run every sample document through.
/// Each one exercises a different branch of the child-node loop in PrintNode.
/// </summary>
public static class OptionSets
{
    /// <summary>Library defaults.</summary>
    public static Options Default => new();

    /// <summary>What Sample/Program.cs uses - whitespace and comment placement preserved.</summary>
    public static Options SampleProgram => new()
    {
        PreserveNewLines = true,
        PreserveCommentPlacement = true,
        WrapCommentTextWithSpaces = true,
    };

    /// <summary>
    /// Drives the blank-line-between-elements branch, which is the only caller that
    /// still needs a child count. Without this set that path is untested.
    /// </summary>
    public static Options BlankLinesBetweenElements => new()
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

    public static Options ByName(string name) =>
        All.First(pair => pair.Name == name).Options;
}
