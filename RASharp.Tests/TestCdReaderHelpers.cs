// Direct tests for the small byte-buffer helpers in CdReader (the C
// strncasecmp/memcmp/atoi-string decode equivalents). They are exercised
// indirectly by the cue/gdi parsing tests but are pure functions with
// their own edge cases.

namespace RASharp.Tests;

/// <summary>Direct tests for the small byte-buffer helpers in CdReader (the C strncasecmp/memcmp/atoi-string decode equivalents). They are exercised indirectly by the cue/gd</summary>
public class TestCdReaderHelpers
{
    /// <summary>Tests the whitespace predicate.</summary>
    /// <param name="b">the byte to test</param>
    [Theory]
    [InlineData((byte)' ')]
    [InlineData((byte)'\t')]
    [InlineData((byte)'\n')]
    [InlineData((byte)'\v')]
    [InlineData((byte)'\f')]
    [InlineData((byte)'\r')]
    public void IsSpaceAcceptsWhitespace(byte b)
    {
        Assert.True(CdReader.IsSpace(b));
    }

    /// <summary>Tests that non-whitespace bytes are rejected.</summary>
    /// <param name="b">the byte to test</param>
    [Theory]
    [InlineData((byte)'a')]
    [InlineData((byte)'0')]
    [InlineData((byte)'_')]
    public void IsSpaceRejectsOthers(byte b)
    {
        Assert.False(CdReader.IsSpace(b));
    }

    /// <summary>Tests the case-insensitive buffer prefix comparison.</summary>
    [Fact]
    public void StartsWithIgnoreCaseMatchesPrefix()
    {
        var buffer = "FILE \"game.bin\" BINARY\n"u8.ToArray();

        Assert.True(CdReader.StartsWithIgnoreCase(buffer, 0, buffer.Length, "FILE "));
        Assert.True(CdReader.StartsWithIgnoreCase(buffer, 0, buffer.Length, "file "));
        Assert.True(CdReader.StartsWithIgnoreCase(buffer, 0, buffer.Length, "FiLe "));
        Assert.False(CdReader.StartsWithIgnoreCase(buffer, 1, buffer.Length, "FILE "));
        Assert.False(CdReader.StartsWithIgnoreCase(buffer, 0, 4, "FILE ")); /* not enough bytes */
    }

    /// <summary>Tests the case-sensitive string prefix comparison.</summary>
    [Fact]
    public void StartsWithStringIsCaseSensitive()
    {
        Assert.True(CdReader.StartsWith("MODE2/2352", "MODE"));
        Assert.False(CdReader.StartsWith("mode2/2352", "MODE"));
        Assert.False(CdReader.StartsWith("MODE", "MODE2/2352")); /* prefix longer than text */
    }

    /// <summary>Tests the case-sensitive buffer prefix comparison.</summary>
    [Fact]
    public void StartsWithBufferIsCaseSensitive()
    {
        var buffer = "TRACK 01 AUDIO"u8.ToArray();

        Assert.True(CdReader.StartsWith(buffer, 0, "TRACK"));
        Assert.False(CdReader.StartsWith(buffer, 0, "track"));
        Assert.False(CdReader.StartsWith(buffer, 1, "TRACK"));
        Assert.False(CdReader.StartsWith(buffer, 10, "TRACK")); /* out of range, no throw */
    }

    /// <summary>Tests the byte-pattern Matches overload.</summary>
    [Fact]
    public void MatchesBytePattern()
    {
        var buffer = "ABCDEF"u8.ToArray();

        Assert.True(CdReader.Matches(buffer, 0, "ABCDEF"u8.ToArray(), 6));
        Assert.True(CdReader.Matches(buffer, 2, "CDE"u8.ToArray(), 3));
        Assert.False(CdReader.Matches(buffer, 0, "ABCEFF"u8.ToArray(), 6));
    }

    /// <summary>Tests the string-pattern Matches overload.</summary>
    [Fact]
    public void MatchesStringPattern()
    {
        var buffer = "1CD001"u8.ToArray();

        Assert.True(CdReader.Matches(buffer, 0, "1CD001", 6));
        Assert.True(CdReader.Matches(buffer, 1, "CD0", 3));
        Assert.False(CdReader.Matches(buffer, 0, "1CD002", 6));
    }

    /// <summary>Tests the explicit-length case-insensitive comparison.</summary>
    [Fact]
    public void CompareIgnoreCaseUsesExplicitLength()
    {
        var buffer = "track.bin"u8.ToArray();

        Assert.True(CdReader.CompareIgnoreCase(buffer, 0, "TRACK", 5));
        Assert.True(CdReader.CompareIgnoreCase(buffer, 0, "track", 5));
        Assert.True(CdReader.CompareIgnoreCase(buffer, 0, "TrAcK", 5));
        Assert.False(CdReader.CompareIgnoreCase(buffer, 0, "TRACKX", 6)); /* '.' != 'x' */
        Assert.False(CdReader.CompareIgnoreCase(buffer, 1, "TRACK", 5));
    }

    /// <summary>Tests NUL-terminated string decoding.</summary>
    [Fact]
    public void GetNulTerminatedStringStopsAtNul()
    {
        var buffer = "ABC\0DEF"u8.ToArray();

        Assert.Equal("ABC", CdReader.GetNulTerminatedString(buffer, 0));
        Assert.Equal("DEF", CdReader.GetNulTerminatedString(buffer, 4));
    }

    /// <summary>Tests NUL-terminated string decoding without a terminator.</summary>
    [Fact]
    public void GetNulTerminatedStringWithoutNulReadsToEnd()
    {
        var buffer = "ABCDEF"u8.ToArray();

        Assert.Equal("ABCDEF", CdReader.GetNulTerminatedString(buffer, 0));
        Assert.Equal("CDEF", CdReader.GetNulTerminatedString(buffer, 2));
    }

    /// <summary>Tests the maxLength bound on NUL-terminated decoding.</summary>
    [Fact]
    public void GetNulTerminatedStringHonorsMaxLength()
    {
        var buffer = "ABCDEF"u8.ToArray();

        Assert.Equal("ABC", CdReader.GetNulTerminatedString(buffer, 0, 3));
        Assert.Equal("CDE", CdReader.GetNulTerminatedString(buffer, 2, 3));
        /* maxLength beyond the buffer clamps to the buffer end */
        Assert.Equal("ABCDEF", CdReader.GetNulTerminatedString(buffer, 0, 100));
    }
}
