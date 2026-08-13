// Tests for the Wii hashers (RcHashWii — disc and WiiWare) — the only
// console family with zero coverage before this file. The fixtures are
// synthetic: a minimal Wii disc (magic, encryption byte, region code, one
// game partition with TMD + two encrypted clusters) and a minimal WiiWare
// title ("Is\0\0" header, TMD + one content). Expected hashes are computed
// from the exact byte ranges the hasher should read, so any offset/size
// regression in the region selection fails the test.
//
// The disc fixture is deliberately laid out like a real disc: the
// partition-table scan region is 0x40000..0x40020 (32 bytes), and the
// partition entry table starts right after it at 0x40020. Putting the
// entry inside the scan region would make the scan read the entry's
// values as further table entries (a self-referential partition that gets
// hashed a second time — exactly what a real disc avoids).

using System.Security.Cryptography;
using RASharp.Core;
using RASharp.Core.Models;

namespace RASharp.Tests;

/// <summary>Tests for the Wii hashers (RcHashWii — disc and WiiWare) — the only console family with zero coverage before this file. The fixtures are synthetic: a minimal Wi</summary>
public class TestHashWii
{
    public TestHashWii()
    {
        MockFilereader.InitMockFilereader();
    }

    /* deterministic per-index pattern: distinct regions hash differently */
    private static byte[] Pattern(int size)
    {
        var data = new byte[size];
        for (var i = 0; i < size; ++i)
        {
            data[i] = (byte)((i * 7 + 3) & 0xFF);
        }

        return data;
    }

    private static void PutBe16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value >> 8);
        buffer[offset + 1] = (byte)value;
    }

    private static void PutBe32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static string Md5Hex(params byte[][] parts)
    {
        using var md5 = MD5.Create();
        foreach (var part in parts)
        {
            md5.TransformBlock(part, 0, part.Length, null, 0);
        }

        md5.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(md5.Hash!).ToLowerInvariant();
    }

    /* ========================================================================= */
    /* Wii disc                                                                  */

    private const uint Partition = 0x80000;
    private const uint TmdSize = 0x400;
    private const uint DataOffset = 0x80800; /* partition + TMD (0x800) */
    private const uint DiscPartSize = 2 * 0x8000;
    private const int ClusterSize = 0x7C00;

    private static byte[] MakeWiiDisc()
    {
        var image = new byte[DataOffset + DiscPartSize];

        /* magic words at 0x18 */
        image[0x18] = 0x5D;
        image[0x19] = 0x1C;
        image[0x1A] = 0x9E;
        image[0x1B] = 0xA3;
        /* encryption byte at 0x61: 0 -> encrypted clusters */
        image[0x61] = 0;

        /* partition table at 0x40000 (8 u32): (count, entry table offset >> 2);
         * the entry table itself must live outside this 32-byte region */
        PutBe32(image, 0x40000, 1);
        PutBe32(image, 0x40004, 0x40020 >> 2);
        /* first (only) entry at 0x40020: (partition offset >> 2, type 0 = game) */
        PutBe32(image, 0x40020, Partition >> 2);
        PutBe32(image, 0x40024, 0);

        /* partition header */
        PutBe32(image, (int)Partition + 0x2A4, TmdSize);
        PutBe32(image, (int)Partition + 0x2A8, 0x400 >> 2); /* TMD right after the header */
        PutBe32(image, (int)Partition + 0x2B8, DataOffset >> 2);
        PutBe32(image, (int)Partition + 0x2BC, DiscPartSize >> 2);

        Array.Copy(Pattern((int)TmdSize), 0, image, (int)(Partition + 0x400), (int)TmdSize);
        for (var ix = 0; ix < 2; ++ix)
        {
            Array.Copy(Pattern(ClusterSize), 0, image, (int)(DataOffset + ix * 0x8000 + 0x400), ClusterSize);
        }

        return image;
    }

    private static string ExpectedWiiDiscHash(byte[] image)
    {
        var header = new byte[0x80];
        Array.Copy(image, 0, header, 0, 0x80);
        var region = new byte[4];
        Array.Copy(image, 0x4E000, region, 0, 4);
        var tmd = new byte[TmdSize];
        Array.Copy(image, (int)(Partition + 0x400), tmd, 0, (int)TmdSize);
        /* cluster i is hashed at DataOffset + i*0x8000 + 0x400 — the 0x400-byte
         * cluster headers between the data regions are not part of the hash */
        var c1 = new byte[ClusterSize];
        Array.Copy(image, (int)(DataOffset + 0x400), c1, 0, ClusterSize);
        var c2 = new byte[ClusterSize];
        Array.Copy(image, (int)(DataOffset + 0x8000 + 0x400), c2, 0, ClusterSize);
        return Md5Hex(header, region, tmd, c1, c2);
    }

    /// <summary>Tests the full Wii disc hash through the public API.</summary>
    [Fact]
    public void TestHashWiiDisc()
    {
        var image = MakeWiiDisc();
        MockFilereader.MockFile(0, "game.iso", image, image.Length);

        Assert.True(RcHash.GenerateFromFile(out var hash, ConsoleIds.RcConsoleWii, "game.iso"));
        Assert.Equal(ExpectedWiiDiscHash(image), hash);
    }

    /// <summary>Tests that a disc without partitions is rejected.</summary>
    [Fact]
    public void TestHashWiiDiscNoPartitions()
    {
        var image = new byte[0x50000];
        image[0x18] = 0x5D;
        image[0x19] = 0x1C;
        image[0x1A] = 0x9E;
        image[0x1B] = 0xA3;
        MockFilereader.MockFile(0, "nopart.iso", image, image.Length);

        Assert.False(RcHash.GenerateFromFile(out var hash, ConsoleIds.RcConsoleWii, "nopart.iso"));
        Assert.Equal("", hash);
    }

    /* ========================================================================= */
    /* WiiWare                                                                   */

    private const uint TmdStartAddr = 0x40;
    private const uint WiiwareTmdSize = 0x200;
    private const uint ContentSize = 0x1000;

    private static byte[] MakeWiiware()
    {
        var image = new byte[TmdStartAddr + WiiwareTmdSize + ContentSize];

        /* "Is\0\0" magic at 0x04 */
        image[0x04] = (byte)'I';
        image[0x05] = (byte)'s';
        image[0x06] = 0;
        image[0x07] = 0;
        PutBe32(image, 0x08, 0); /* cert chain size (aligned to 0x40) */
        PutBe32(image, 0x10, 0); /* ticket size (aligned to 0x40) */
        PutBe32(image, 0x14, WiiwareTmdSize);

        Array.Copy(Pattern((int)WiiwareTmdSize), 0, image, (int)TmdStartAddr, (int)WiiwareTmdSize);
        /* one content record: count at TMD + 0x1DE, size at TMD + 0x1E4 + 8 */
        PutBe16(image, (int)TmdStartAddr + 0x1DE, 1);
        PutBe32(image, (int)TmdStartAddr + 0x1E4 + 8, 0); /* size high dword */
        PutBe32(image, (int)TmdStartAddr + 0x1E4 + 12, ContentSize); /* size low dword */

        Array.Copy(Pattern((int)ContentSize), 0, image, (int)(TmdStartAddr + WiiwareTmdSize), (int)ContentSize);
        return image;
    }

    private static string ExpectedWiiwareHash(byte[] image)
    {
        var tmd = new byte[WiiwareTmdSize];
        Array.Copy(image, (int)TmdStartAddr, tmd, 0, (int)WiiwareTmdSize);
        var content = new byte[ContentSize];
        Array.Copy(image, (int)(TmdStartAddr + WiiwareTmdSize), content, 0, (int)ContentSize);
        return Md5Hex(tmd, content);
    }

    /// <summary>Tests the WiiWare hash through the public API.</summary>
    [Fact]
    public void TestHashWiiware()
    {
        var image = MakeWiiware();
        MockFilereader.MockFile(0, "game.wad", image, image.Length);

        Assert.True(RcHash.GenerateFromFile(out var hash, ConsoleIds.RcConsoleWii, "game.wad"));
        Assert.Equal(ExpectedWiiwareHash(image), hash);
    }

    /* ========================================================================= */
    /* error paths                                                               */

    private static (int Result, string Error) HashWiiDirect(byte[] image, string filename)
    {
        /* GetMockFilereader() resets the mock table — fetch it first */
        var filereader = MockFilereader.GetMockFilereader();
        MockFilereader.MockFile(0, filename, image, image.Length);

        var iterator = new RcHashIterator
        {
            Path = filename,
            Callbacks = { Filereader = filereader }
        };
        var errors = new List<string>();
        iterator.Callbacks.ErrorMessage = (message, _) => errors.Add(message);

        var result = HashDisc.RcHashWii(out _, iterator);
        return (result, string.Join(" | ", errors));
    }

    /// <summary>Tests that a file without a Wii magic is rejected.</summary>
    [Fact]
    public void TestHashWiiUnsupportedFile()
    {
        var (result, error) = HashWiiDirect([0x00, 0x01, 0x02, 0x03], "junk.bin");

        Assert.Equal(0, result);
        Assert.Contains("Not a supported Wii file", error, StringComparison.Ordinal);
    }

    /// <summary>Tests that the no-partitions error is reported through the iterator callback.</summary>
    [Fact]
    public void TestHashWiiNoPartitionsReportsError()
    {
        var image = new byte[0x50000];
        image[0x18] = 0x5D;
        image[0x19] = 0x1C;
        image[0x1A] = 0x9E;
        image[0x1B] = 0xA3;

        var (result, error) = HashWiiDirect(image, "nopart.iso");

        Assert.Equal(0, result);
        Assert.Contains("No partitions found", error, StringComparison.Ordinal);
    }
}
