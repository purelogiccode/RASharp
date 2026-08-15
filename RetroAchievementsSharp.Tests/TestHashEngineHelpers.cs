// Direct tests for the HashEngine path helpers and byteswap routines.
// These are exercised indirectly by every full-file hash (via file paths)
// and by the N64/Jaguar-CD disc paths, but the pure functions deserve
// their own vectors.

namespace RetroAchievementsSharp.Tests;

/// <summary>Direct tests for the HashEngine path helpers and byteswap routines. These are exercised indirectly by every full-file hash (via file paths) and by the N64/Jag</summary>
public class TestHashEngineHelpers
{
    /* ========================================================================= */
    /* path helpers                                                              */

    /// <summary>Tests PathGetFilename with both separator styles.</summary>
    /// <param name="path">the input path</param>
    /// <param name="expected">the expected file name</param>
    [Theory]
    [InlineData("a/b/c.bin", "c.bin")]
    [InlineData(@"c:\dir\file.bin", "file.bin")]
    [InlineData("noSep", "noSep")]
    [InlineData("a/b/", "")]
    [InlineData("", "")]
    public void PathGetFilenameTakesLastSegment(string path, string expected)
    {
        Assert.Equal(expected, HashEngine.PathGetFilename(path));
    }

    /// <summary>Tests PathGetExtension (last dot, extension without the leading dot).</summary>
    /// <param name="path">the input path</param>
    /// <param name="expected">the expected extension</param>
    [Theory]
    [InlineData("a.txt", "txt")]
    [InlineData("a", "")]
    [InlineData("a.b.c", "c")]
    [InlineData("dir/file", "")]
    [InlineData(".hidden", "hidden")]
    public void PathGetExtensionTakesLastDot(string path, string expected)
    {
        Assert.Equal(expected, HashEngine.PathGetExtension(path));
    }

    /// <summary>Tests PathCompareExtension — matching extensions.</summary>
    /// <param name="path">the input path</param>
    /// <param name="ext">the candidate extension without a leading dot</param>
    [Theory]
    [InlineData("game.cue", "cue")]
    [InlineData("game.CUE", "cue")] /* path side is case-insensitive */
    public void PathCompareExtensionMatches(string path, string ext)
    {
        Assert.Equal(1, HashEngine.PathCompareExtension(path, ext));
    }

    /// <summary>Tests PathCompareExtension — non-matching cases.</summary>
    /// <param name="path">the input path</param>
    /// <param name="ext">the candidate extension</param>
    [Theory]
    [InlineData("game.cue", "CUE")] /* uppercase candidate does not match */
    [InlineData("game.cue", "cuex")]
    [InlineData("game.cue", "e")] /* the dot must precede the candidate exactly */
    [InlineData("cue", "cue")] /* no dot in the path */
    [InlineData("game", "gamecue")] /* candidate longer than the path */
    [InlineData("", "cue")]
    public void PathCompareExtensionRejects(string path, string ext)
    {
        Assert.Equal(0, HashEngine.PathCompareExtension(path, ext));
    }

    /* ========================================================================= */
    /* byteswap helpers                                                          */

    /// <summary>Tests Byteswap16 — swaps the 16-bit halves of each 32-bit group.</summary>
    [Fact]
    public void Byteswap16SwapsHalves()
    {
        var buffer = new byte[] { 0x11, 0x22, 0x33, 0x44 };

        HashEngine.Byteswap16(buffer, buffer.Length);

        Assert.Equal(new byte[] { 0x22, 0x11, 0x44, 0x33 }, buffer);
    }

    /// <summary>Tests Byteswap32 — reverses each 32-bit group.</summary>
    [Fact]
    public void Byteswap32ReversesGroup()
    {
        var buffer = new byte[] { 0x11, 0x22, 0x33, 0x44 };

        HashEngine.Byteswap32(buffer, buffer.Length);

        Assert.Equal(new byte[] { 0x44, 0x33, 0x22, 0x11 }, buffer);
    }

    /// <summary>Tests that trailing bytes (not forming a full group) are left untouched.</summary>
    [Fact]
    public void ByteswapLeavesTrailingBytesUntouched()
    {
        var buffer = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };

        HashEngine.Byteswap32(buffer, buffer.Length);

        Assert.Equal(new byte[] { 0x04, 0x03, 0x02, 0x01, 0x05, 0x06 }, buffer);
    }

    /// <summary>Tests that a zero count is a no-op.</summary>
    [Fact]
    public void ByteswapZeroCountIsNoOp()
    {
        var buffer = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        HashEngine.Byteswap16(buffer, 0);

        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, buffer);
    }
}
