// Tests for the legacy CLI argument resolution (Program.FindConsoleId):
// group-key matching (case-insensitive) with the numeric atoi fallback.

using RetroAchievementsSharp.Cli;

namespace RetroAchievementsSharp.Tests;

/// <summary>Tests for the legacy CLI argument resolution (Program.FindConsoleId): group-key matching (case-insensitive) with the numeric atoi fallback.</summary>
public class TestProgram
{
    /// <summary>Tests that a console key resolves to its id, case-insensitively.</summary>
    /// <param name="key">the console key</param>
    [Theory]
    [InlineData("NES")]
    [InlineData("nes")]
    [InlineData("Nes")]
    public void FindConsoleIdMatchesGroupKeys(string key)
    {
        Assert.Equal(7, Program.FindConsoleId(key));
    }

    /// <summary>Tests that a numeric argument falls back to atoi.</summary>
    /// <param name="key">the numeric argument</param>
    /// <param name="expected">the expected id</param>
    [Theory]
    [InlineData("62", 62)]
    [InlineData("+7", 7)]
    [InlineData("-1", -1)]
    [InlineData("7x", 7)] /* digits are consumed until the first non-digit */
    public void FindConsoleIdFallsBackToAtoi(string key, int expected)
    {
        Assert.Equal(expected, Program.FindConsoleId(key));
    }

    /// <summary>Tests that unknown keys resolve to 0.</summary>
    /// <param name="key">the unknown argument</param>
    [Theory]
    [InlineData("bogus")]
    [InlineData("?")]
    [InlineData("")]
    public void FindConsoleIdUnknownResolvesToZero(string key)
    {
        Assert.Equal(0, Program.FindConsoleId(key));
    }

    /// <summary>Tests that consoles without a group fall through to numeric parsing.</summary>
    [Fact]
    public void FindConsoleIdIgnoresGroupLessConsoles()
    {
        /* "3DS" (id 62) has a null group in the table: no key match, and the
         * atoi fallback parses the leading digit — so it resolves to 3 */
        Assert.Equal(3, Program.FindConsoleId("3DS"));
    }
}
