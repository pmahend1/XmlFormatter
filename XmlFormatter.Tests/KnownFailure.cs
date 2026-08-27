using Xunit.Sdk;

namespace XmlFormatter.Tests;

/// <summary>
/// Asserts what the formatter *should* produce where it currently produces something else.
/// Passes while the bug is present, fails once it is fixed, so the exemption cannot outlive
/// the bug.
/// </summary>
internal static class KnownFailure
{
    public static void Expect(string reason, Action assertion)
    {
        try
        {
            assertion();
        }
        catch (XunitException)
        {
            return;
        }

        throw new XunitException($"FIXED: {reason}\n\n"
                               + "Remove the KnownFailure.Expect wrapper so the assertion stands on its own.");
    }
}
