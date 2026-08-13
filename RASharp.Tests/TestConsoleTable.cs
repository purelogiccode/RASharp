// Tests for the ConsoleTable key lookup used by scan/identify to render
// console ids as CLI keys ("NES", "GB", ...).

using RASharp.Cli;

namespace RASharp.Tests;

/// <summary>Tests for the ConsoleTable key lookup used by scan/identify to render console ids as CLI keys ("NES", "GB", ...).</summary>
public class TestConsoleTable
{
    /// <summary>Tests that known console ids map to their CLI keys.</summary>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="expected">the expected key</param>
    [Theory]
    [InlineData((uint)7, "NES")]
    [InlineData((uint)4, "GB")]
    [InlineData((uint)62, "3DS")]
    public void KeyMapsKnownConsoles(uint consoleId, string expected)
    {
        Assert.Equal(expected, ConsoleTable.Key(consoleId));
    }

    /// <summary>Tests that unknown console ids map to the "?" marker.</summary>
    /// <param name="consoleId">the console identifier</param>
    [Theory]
    [InlineData((uint)0)]
    [InlineData((uint)999999)]
    public void KeyMapsUnknownToQuestionMark(uint consoleId)
    {
        Assert.Equal("?", ConsoleTable.Key(consoleId));
    }
}
