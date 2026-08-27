namespace XmlFormatter.Tests;

internal static class TestOptions
{
    // Kept out of OptionSets: that type's All collection parameterizes the fixture baselines.
    public static Options NoDeclaration => new() { AddXmlDeclarationIfMissing = false };
}
