namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// Starting points for the per-option tests.
///
/// Deliberately separate from <see cref="OptionSets"/>: that type's All collection
/// parameterises the fixture baselines, so a set added there would change what those
/// tests record.
/// </summary>
internal static class TestOptions
{
    /// <summary>
    /// Library defaults with the generated XML declaration suppressed, so a test about
    /// indentation does not have to restate a declaration line it does not care about.
    /// The declaration itself is covered by AddXmlDeclarationIfMissingTests.
    /// </summary>
    public static Options NoDeclaration => new() { AddXmlDeclarationIfMissing = false };
}
