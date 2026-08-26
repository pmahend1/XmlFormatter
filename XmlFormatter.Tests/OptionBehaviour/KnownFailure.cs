using Xunit.Sdk;

namespace XmlFormatter.Tests.OptionBehaviour;

/// <summary>
/// Wraps an assertion that states what the formatter *should* produce for a case where it
/// currently produces something else.
///
/// The test passes while the bug is present and fails the moment it is fixed, so an
/// exemption cannot quietly outlive the bug it was written for - the same arrangement the
/// perf guard uses for its KnownFailing cases. The alternative, pinning the wrong output as
/// if it were intended, is what the fixture baselines already do; there is no value in
/// repeating it here.
/// </summary>
internal static class KnownFailure
{
    /// <param name="reason">What is wrong, in the terms someone fixing it would use.</param>
    /// <param name="assertion">The assertion that will hold once the bug is fixed.</param>
    public static void Expect(string reason, Action assertion)
    {
        try
        {
            assertion();
        }
        catch (XunitException)
        {
            /*
             * Any assertion failure counts as "still broken". That is broader than checking
             * the output matches today's wrong value exactly, and deliberately so: pinning
             * the wrong output would make this a characterization test with extra steps, and
             * it would fail on an unrelated near-miss change instead of on the fix.
             */
            return;
        }

        throw new XunitException($"FIXED: {reason}\n\n"
                               + "The formatter now produces the expected output. Remove the KnownFailure.Expect "
                               + "wrapper so the assertion stands on its own, and drop the issue from AGENTS.md.");
    }
}
