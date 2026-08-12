// Phase E3 (Part II) — .neo (Geolith Neo Geo cart) vectors, ported from
// rcheevos 12.4.0 test/rhash/test_hash_rom.c (test_hash_neo,
// test_hash_neo_header_variants, test_hash_neo_bad_magic) plus the
// "neo"/"sms" extension-table mapping checks.

using System.Text;
using RASharp.Core;
using Xunit;

namespace RASharp.Tests;


using RASharp.Core.Models;
/// <summary>Phase E3 (Part II) — .neo (Geolith Neo Geo cart) vectors, ported from rcheevos 12.4.0 test/rhash/test_hash_rom.c (test_hash_neo, test_hash_neo_header_variants, </summary>
public class TestHashNeo
{
    public TestHashNeo()
    {
        MockFilereader.InitMockFilereader();
    }

    /* generate_neo_file from test_hash_rom.c (12.4.0): 4096-byte header with
     * NEO\1 magic, P ROM size (LE) and tool-variant text fields, followed by
     * payload_size bytes of ROM data. fill_image seeds from size, so the
     * payload is byte-identical to GenerateGenericFile(payload_size). */
/// <summary>generate_neo_file from test_hash_rom.c (12.4.0): 4096-byte header with NEO\1 magic, P ROM size (LE) and tool-variant text fields, followed by payload_size bytes</summary>
/// <param name="payloadSize">the payload size parameter</param>
/// <param name="name">the name parameter</param>
/// <param name="manufacturer">the manufacturer parameter</param>
/// <returns>the result</returns>
    internal static byte[] GenerateNeoFile(int payloadSize, string name, string manufacturer)
    {
        const int headerSize = 4096;
        byte[] image = new byte[headerSize + payloadSize];
        image[0] = (byte)'N';
        image[1] = (byte)'E';
        image[2] = (byte)'O';
        image[3] = 1;
        image[4] = (byte)(payloadSize & 0xFF);
        image[5] = (byte)((payloadSize >> 8) & 0xFF);
        image[6] = (byte)((payloadSize >> 16) & 0xFF);
        image[7] = (byte)((payloadSize >> 24) & 0xFF);
        Encoding.ASCII.GetBytes(name).CopyTo(image, 44);
        Encoding.ASCII.GetBytes(manufacturer).CopyTo(image, 77);
        TestDataGen.FillImage(image, headerSize, payloadSize);
        return image;
    }

/// <summary>Tests hash neo file.</summary>
    [Fact]
    public void TestHashNeoFile()
    {
        /* the hash of a .neo file is the hash of its ROM data (everything
         * after the 4096-byte header), so it must match a plain full-buffer
         * hash of the payload alone */
        const int payloadSize = 131072;
        byte[] image = GenerateNeoFile(payloadSize, "Test Game", "TestCorp");
        byte[] payload = TestDataGen.GenerateGenericFile(payloadSize);

        Assert.True(RcHash.GenerateFromBuffer(out string hashPayload, ConsoleIds.RC_CONSOLE_MEGA_DRIVE, payload, payloadSize));

        MockFilereader.MockFile(0, "game.neo", image, image.Length);
        Assert.True(RcHash.GenerateFromFile(out string hashFile, ConsoleIds.RC_CONSOLE_ARCADE, "game.neo"));
        Assert.Equal(hashFile, hashPayload);

        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.neo", null, 0);
        Assert.True(HashIterator.Iterate(out string hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);
        Assert.Equal(hashIterator, hashPayload);
    }

/// <summary>Tests hash neo header variants.</summary>
    [Fact]
    public void TestHashNeoHeaderVariants()
    {
        /* conversion tools fill the header text fields differently - two .neo
         * files with the same ROM data but different headers must hash the same */
        const int payloadSize = 131072;
        byte[] image1 = GenerateNeoFile(payloadSize, "Test Game", "TestCorp");
        byte[] image2 = GenerateNeoFile(payloadSize, "test game (alt name)", "OtherTool");

        MockFilereader.MockFile(0, "game1.neo", image1, image1.Length);
        MockFilereader.MockFile(1, "game2.neo", image2, image2.Length);

        Assert.True(RcHash.GenerateFromFile(out string hash1, ConsoleIds.RC_CONSOLE_ARCADE, "game1.neo"));
        Assert.True(RcHash.GenerateFromFile(out string hash2, ConsoleIds.RC_CONSOLE_ARCADE, "game2.neo"));
        Assert.Equal(hash1, hash2);
    }

/// <summary>Tests hash neo bad magic.</summary>
    [Fact]
    public void TestHashNeoBadMagic()
    {
        /* a .neo file without the NEO\1 magic must not hash */
        const int payloadSize = 131072;
        byte[] image = GenerateNeoFile(payloadSize, "Test Game", "TestCorp");
        image[3] = 2; /* unsupported version */

        MockFilereader.MockFile(0, "game.neo", image, image.Length);
        Assert.False(RcHash.GenerateFromFile(out _, ConsoleIds.RC_CONSOLE_ARCADE, "game.neo"));
    }

/// <summary>Tests ext table neo and sms.</summary>
    [Fact]
    public void TestExtTableNeoAndSms()
    {
        /* rcheevos 12.4.0 adds "neo" (→ Arcade content hash) and "sms"
         * (→ Master System) to the bsearch-sorted extension table */
        HashIterator.GetIteratorExtHandlers(out int numHandlers);

        ExtHandlerEntry? neo = null;
        ExtHandlerEntry? sms = null;
        foreach (ExtHandlerEntry entry in HashIterator.GetIteratorExtHandlers(out _))
        {
            if (string.Equals(entry.Ext, "neo", StringComparison.Ordinal))
                neo = entry;
            else if (string.Equals(entry.Ext, "sms", StringComparison.Ordinal))
                sms = entry;
        }

        Assert.NotNull(neo);
        Assert.Equal((int)ConsoleIds.RC_CONSOLE_ARCADE, neo!.Data);

        Assert.NotNull(sms);
        Assert.Equal((int)ConsoleIds.RC_CONSOLE_MASTER_SYSTEM, sms!.Data);
    }
}
