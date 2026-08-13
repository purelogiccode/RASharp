// Tests for the HashMd5 wrapper (rcheevos src/rhash/md5.c port). The wrapper
// delegates to the BCL MD5 provider; these tests pin the known-answer
// vectors and the Append/Finish state machine against the BCL directly.

using System.Security.Cryptography;
using System.Text;
using RASharp.Core;

namespace RASharp.Tests;

/// <summary>Tests for the HashMd5 wrapper (rcheevos src/rhash/md5.c port). The wrapper delegates to the BCL MD5 provider; these tests pin the known-answer vectors and th</summary>
public class TestHashMd5
{
    private static string HashString(string text)
    {
        var md5 = new HashMd5();
        md5.Append(Encoding.ASCII.GetBytes(text), text.Length);
        return Convert.ToHexString(md5.Finish()).ToLowerInvariant();
    }

    /// <summary>Tests the standard MD5 known-answer vectors.</summary>
    /// <param name="input">the input string</param>
    /// <param name="expected">the expected md5 hex</param>
    [Theory]
    [InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("abc", "900150983cd24fb0d6963f7d28e17f72")]
    [InlineData("The quick brown fox jumps over the lazy dog", "9e107d9d372bb6826bd81d3542a419d6")]
    public void KnownAnswerVectors(string input, string expected)
    {
        Assert.Equal(expected, HashString(input));
    }

    /// <summary>Tests that splitting an append across calls yields the same digest.</summary>
    [Fact]
    public void AppendSplitAcrossCallsMatchesSingleAppend()
    {
        var bytes = "The quick brown fox jumps over the lazy dog"u8.ToArray();

        var single = new HashMd5();
        single.Append(bytes, bytes.Length);

        var split = new HashMd5();
        foreach (var b in bytes)
        {
            split.Append([b], 1);
        }

        Assert.Equal(Convert.ToHexString(single.Finish()), Convert.ToHexString(split.Finish()));
    }

    /// <summary>Tests the offset/length Append overload.</summary>
    [Fact]
    public void AppendWithOffsetUsesOnlyTheRequestedRange()
    {
        /* buffer: [x][a][b][c][x] — hash only the middle three bytes */
        var buffer = "xabcx"u8.ToArray();
        var md5 = new HashMd5();
        md5.Append(buffer, 1, 3);

        Assert.Equal("900150983cd24fb0d6963f7d28e17f72", Convert.ToHexString(md5.Finish()).ToLowerInvariant());
    }

    /// <summary>Tests that Finish resets the state for reuse.</summary>
    [Fact]
    public void FinishResetsState()
    {
        var bytes = "abc"u8.ToArray();
        var md5 = new HashMd5();

        md5.Append(bytes, bytes.Length);
        var first = Convert.ToHexString(md5.Finish());

        md5.Append(bytes, bytes.Length);
        var second = Convert.ToHexString(md5.Finish());

        Assert.Equal(first, second);
        Assert.Equal("900150983cd24fb0d6963f7d28e17f72", first.ToLowerInvariant());
    }

    /// <summary>Tests a larger payload against the BCL MD5 implementation.</summary>
    [Fact]
    public void MatchesBclMd5ForLargerPayload()
    {
        var bytes = new byte[1000];
        for (var i = 0; i < bytes.Length; ++i)
        {
            bytes[i] = (byte)(i * 31 + 7);
        }

        var md5 = new HashMd5();
        md5.Append(bytes, bytes.Length);

        var expected = MD5.HashData(bytes);
        Assert.Equal(expected, md5.Finish());
    }
}
