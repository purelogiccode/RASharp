// Application constants.
//
// The bug report API key is deliberately NOT stored in plaintext: it is
// double-encoded (Base64 applied twice, see InstructionsToSendBugs.md of the
// AspNet_BugReportEmailService project) so it does not appear as a readable
// secret in the source tree. The application decodes it once at startup
// (BugReportApiKey) and uses the decoded value; an explicit
// RASHARP_BUGREPORT_API_KEY environment variable always takes precedence.

using System.Text;

namespace RASharp.Cli;

/// <summary>Application constants. The bug report API key is deliberately NOT stored in plaintext: it is double-encoded (Base64 applied twice, see InstructionsToSendBugs.md</summary>
internal static class Constants
{
    /* double-encoded value of the bug report API key */
    public const string BugReportApiKeyEncoded =
        "YUdwb04zbDFOblExTm5SNWNqVTBNRzg1ZFRnM05qYzJOelp5TlRZM05EVXpORFExTXpJek5USTJOR00zTldJMmREZG5aMmRvWjJjM05uUnlaalUyTkdVPQ==";

    /* decoded at first use (application startup) */
    public static string BugReportApiKey { get; } = Decode(BugReportApiKeyEncoded);

    private static string Decode(string value)
    {
        var once = Encoding.UTF8.GetString(Convert.FromBase64String(value));
        return Encoding.UTF8.GetString(Convert.FromBase64String(once));
    }
}
