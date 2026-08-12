// Phase 7 — ZIP hashing tests (port of test_hash_zip.c).
// The mock zip builder reproduces the C's mock_zip_add_file/mock_zip_finalize
// byte-for-byte (incl. the Zip64 variant); the expected hashes are the ones
// embedded in the upstream test file.

using RASharp.Core;
using Xunit;

namespace RASharp.Tests;


using RASharp.Core.Models;
public class TestHashZip
{
    public sealed class MockZipFileDebug : MockZipFile
    {
        public MockZipFileDebug(int capacity) : base(capacity) { }
        public byte[] Take(int n) { var r = new byte[n]; Array.Copy(Buffer, 0, r, 0, n); return r; }
        public new void AddFile(string f, uint c, uint s) { base.AddFile(f, c, s); }
    }

    public class MockZipFile
    {
        public byte[] Buffer;
        public int FinalizePublic(string c) { return Finalize(c); }
        public int Ptr;
        public int[] FilePtr = new int[8];
        public int NumFiles;
        public bool IsZip64;

        public MockZipFile(int capacity)
        {
            Buffer = new byte[capacity];
        }

        public void AddFile(string filename, uint crc32, uint size)
        {
            byte[] name = System.Text.Encoding.ASCII.GetBytes(filename);
            int filenameLen = name.Length;
            int outPos = Ptr;

            FilePtr[NumFiles++] = outPos;

            /* local file signature */
            Buffer[outPos++] = (byte)'P';
            Buffer[outPos++] = (byte)'K';
            Buffer[outPos++] = 0x03;
            Buffer[outPos++] = 0x04;

            /* version needed to extract */
            Buffer[outPos++] = 0x14;
            Buffer[outPos++] = 0x00;

            /* general purpose bit flag */
            Buffer[outPos++] = 0x02;
            Buffer[outPos++] = 0x00;

            /* compression method */
            Buffer[outPos++] = 0x08;
            Buffer[outPos++] = 0x00;

            /* file last modified time */
            Buffer[outPos++] = 0x00;
            Buffer[outPos++] = 0xBC;

            /* file last modified date */
            Buffer[outPos++] = 0x98;
            Buffer[outPos++] = 0x21;

            /* CRC-32 */
            Buffer[outPos++] = (byte)(crc32 & 0xFF);
            Buffer[outPos++] = (byte)((crc32 >> 8) & 0xFF);
            Buffer[outPos++] = (byte)((crc32 >> 16) & 0xFF);
            Buffer[outPos++] = (byte)((crc32 >> 24) & 0xFF);

            /* compressed size */
            Buffer[outPos++] = (byte)(size & 0xFF);
            Buffer[outPos++] = (byte)((size >> 8) & 0xFF);
            Buffer[outPos++] = (byte)((size >> 16) & 0xFF);
            Buffer[outPos++] = (byte)((size >> 24) & 0xFF);

            /* uncompressed size */
            Buffer[outPos++] = (byte)(size & 0xFF);
            Buffer[outPos++] = (byte)((size >> 8) & 0xFF);
            Buffer[outPos++] = (byte)((size >> 16) & 0xFF);
            Buffer[outPos++] = (byte)((size >> 24) & 0xFF);

            /* file name length */
            Buffer[outPos++] = (byte)(filenameLen & 0xFF);
            Buffer[outPos++] = (byte)((filenameLen >> 8) & 0xFF);

            /* extra field length */
            Buffer[outPos++] = 0;
            Buffer[outPos++] = 0;

            /* file name */
            Array.Copy(name, 0, Buffer, outPos, filenameLen);
            outPos += filenameLen;

            /* compressed content */
            Buffer[outPos++] = 0x73;
            Buffer[outPos++] = 0x02;
            Buffer[outPos++] = 0x00;

            Ptr = outPos;
        }

        public int Finalize(string comment)
        {
            byte[] commentBytes = System.Text.Encoding.ASCII.GetBytes(comment);
            int commentLen = commentBytes.Length;
            int outPos = Ptr;
            int firstCdirEntry = Ptr;
            int offset;

            for (int i = 0; i < NumFiles; i++)
            {
                int inPos = FilePtr[i];

                /* central directory file header */
                Buffer[outPos++] = (byte)'P';
                Buffer[outPos++] = (byte)'K';
                Buffer[outPos++] = 0x01;
                Buffer[outPos++] = 0x02;

                /* version made by */
                Buffer[outPos++] = 0x14;
                Buffer[outPos++] = 0x00;

                /* copy the rest of the local file header */
                Array.Copy(Buffer, inPos + 4, Buffer, outPos, 26);
                outPos += 26;

                /* file comment length */
                Buffer[outPos++] = 0;
                Buffer[outPos++] = 0;

                /* disk number start */
                Buffer[outPos++] = 0;
                Buffer[outPos++] = 0;

                /* internal file attributes */
                Buffer[outPos++] = 0;
                Buffer[outPos++] = 0;

                /* external file attributes */
                Buffer[outPos++] = 0;
                Buffer[outPos++] = 0;
                Buffer[outPos++] = 0;
                Buffer[outPos++] = 0;

                /* relative offset of local header */
                offset = inPos;
                Buffer[outPos++] = (byte)(offset & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 8) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 16) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 24) & 0xFF);

                /* file name */
                int filenameLen = (Buffer[inPos + 27] << 8) | Buffer[inPos + 26];
                Array.Copy(Buffer, inPos + 30, Buffer, outPos, filenameLen);
                outPos += filenameLen;

                if (IsZip64)
                {
                    /* zip64 extended information extra field header id */
                    Buffer[outPos++] = 0x01;
                    Buffer[outPos++] = 0x00;

                    /* size of extra field chunk */
                    Buffer[outPos++] = 0x10; /* only providing file sizes */
                    Buffer[outPos++] = 0x00;

                    /* uncompressed file size */
                    for (int k = 0; k < 4; k++)
                        Buffer[outPos++] = 0;
                    Array.Copy(Buffer, inPos + 22, Buffer, outPos, 4);
                    outPos += 8;

                    /* compressed file size */
                    for (int k = 0; k < 4; k++)
                        Buffer[outPos++] = 0;
                    Array.Copy(Buffer, inPos + 18, Buffer, outPos, 4);
                    outPos += 8;
                }
            }

            Ptr = outPos;

            if (IsZip64)
            {
                /* end of central directory header */
                Buffer[outPos++] = (byte)'P';
                Buffer[outPos++] = (byte)'K';
                Buffer[outPos++] = 0x06;
                Buffer[outPos++] = 0x06;

                /* size of EOCD64 minus 12 */
                Buffer[outPos++] = 0x2C;
                for (int k = 0; k < 7; k++)
                    Buffer[outPos++] = 0;

                /* version made by */
                Buffer[outPos++] = 0x2D;
                Buffer[outPos++] = 0x00;

                /* version needed to extract */
                Buffer[outPos++] = 0x2D;
                Buffer[outPos++] = 0x00;

                /* disk number */
                for (int k = 0; k < 4; k++)
                    Buffer[outPos++] = 0;

                /* disk number of central directory */
                for (int k = 0; k < 4; k++)
                    Buffer[outPos++] = 0;

                /* number of central directory records on this disk */
                Buffer[outPos++] = (byte)(NumFiles & 0xFF);
                for (int k = 0; k < 7; k++)
                    Buffer[outPos++] = 0;

                /* total number of central directory records */
                Buffer[outPos++] = (byte)(NumFiles & 0xFF);
                for (int k = 0; k < 7; k++)
                    Buffer[outPos++] = 0;

                /* size of central directory */
                offset = Ptr - firstCdirEntry;
                Buffer[outPos++] = (byte)(offset & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 8) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 16) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 24) & 0xFF);
                for (int k = 0; k < 4; k++)
                    Buffer[outPos++] = 0;

                /* address of first central directory entry */
                offset = firstCdirEntry;
                Buffer[outPos++] = (byte)(offset & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 8) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 16) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 24) & 0xFF);
                for (int k = 0; k < 4; k++)
                    Buffer[outPos++] = 0;

                /* end of central directory locator header */
                Buffer[outPos++] = (byte)'P';
                Buffer[outPos++] = (byte)'K';
                Buffer[outPos++] = 0x06;
                Buffer[outPos++] = 0x07;

                /* disk number */
                for (int k = 0; k < 4; k++)
                    Buffer[outPos++] = 0;

                /* address of central directory 64 */
                offset = Ptr;
                Buffer[outPos++] = (byte)(offset & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 8) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 16) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 24) & 0xFF);
                for (int k = 0; k < 4; k++)
                    Buffer[outPos++] = 0;

                /* total number of disks */
                Buffer[outPos++] = 1;
                Buffer[outPos++] = 0;
                Buffer[outPos++] = 0;
                Buffer[outPos++] = 0;

                Ptr = outPos;
            }

            /* end of central directory header */
            Buffer[outPos++] = (byte)'P';
            Buffer[outPos++] = (byte)'K';
            Buffer[outPos++] = 0x05;
            Buffer[outPos++] = 0x06;

            /* disk number */
            Buffer[outPos++] = 0;
            Buffer[outPos++] = 0;

            /* central directory disk number */
            Buffer[outPos++] = 0;
            Buffer[outPos++] = 0;

            /* number of central directory records on this disk */
            Buffer[outPos++] = (byte)(NumFiles & 0xFF);
            Buffer[outPos++] = 0;

            /* total number of central directory records */
            Buffer[outPos++] = (byte)(NumFiles & 0xFF);
            Buffer[outPos++] = 0;

            if (IsZip64)
            {
                /* size and address of central directory are -1 in zip64 */
                for (int k = 0; k < 8; k++)
                    Buffer[outPos++] = 0xFF;
            }
            else
            {
                /* size of central directory */
                offset = Ptr - firstCdirEntry;
                Buffer[outPos++] = (byte)(offset & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 8) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 16) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 24) & 0xFF);

                /* address of first central directory entry */
                offset = firstCdirEntry;
                Buffer[outPos++] = (byte)(offset & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 8) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 16) & 0xFF);
                Buffer[outPos++] = (byte)((offset >> 24) & 0xFF);
            }

            /* comment length */
            Buffer[outPos++] = (byte)(commentLen & 0xFF);
            Buffer[outPos++] = (byte)((commentLen >> 8) & 0xFF);

            if (commentLen > 0)
            {
                Array.Copy(commentBytes, 0, Buffer, outPos, commentLen);
                outPos += commentLen;
            }

            Ptr = outPos;
            return Ptr;
        }
    }

    /* ========================================================================= */

    [Fact]
    public void TestHashArduboyFx()
    {
        var zip = new MockZipFile(768);
        zip.AddFile("info.json", 0xA40B2541, 35);
        zip.AddFile("game.bin", 0x5AA654C0, 96);
        zip.AddFile("save.bin", 0xFF000000, 1);
        zip.AddFile("interp_s2_ArduboyFX.hex", 0x50648360, 71);
        zip.AddFile("screenshot.png", 0x30056694, 48);
        int zipSize = zip.Finalize("");
        MockFilereader.InitMockFilereader(); /* xUnit runs classes in parallel; keep the global ours */
        Assert.True(zipSize <= zip.Buffer.Length);

        byte[] zipContents = new byte[zipSize];
        Array.Copy(zip.Buffer, 0, zipContents, 0, zipSize);
        MockFilereader.MockFile(0, "game.arduboy", zipContents, zipContents.Length);

        Assert.True(RcHash.GenerateFromFile(out string hashFile, ConsoleIds.RC_CONSOLE_ARDUBOY, "game.arduboy"));
        Assert.Equal("e696445c353e9d6b3d60bf5d194b82cf", hashFile);

        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.arduboy", null, 0);
        Assert.True(HashIterator.Iterate(out string hashIterator, iterator) != 0);
        Assert.Equal("e696445c353e9d6b3d60bf5d194b82cf", hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    [Fact]
    public void TestHashMsdosDosz()
    {
        var zip = new MockZipFile(512);
        zip.AddFile("FOLDER/", 0, 0);
        zip.AddFile("FOLDER/SUB.TXT", 0x4AD0CF31, 1);
        zip.AddFile("ROOT.TXT", 0xD3D99E8B, 1);
        int zipSize = zip.Finalize("TORRENTZIPPED-FD07C52C");
        MockFilereader.InitMockFilereader(); /* xUnit runs classes in parallel; keep the global ours */
        Assert.True(zipSize <= zip.Buffer.Length);

        byte[] zipContents = new byte[zipSize];
        Array.Copy(zip.Buffer, 0, zipContents, 0, zipSize);
        MockFilereader.MockFile(0, "game.dosz", zipContents, zipContents.Length);

        Assert.True(RcHash.GenerateFromFile(out string hashFile, ConsoleIds.RC_CONSOLE_MS_DOS, "game.dosz"));
        Assert.Equal("59a255662262f5ada32791b8c36e8ea7", hashFile);

        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.dosz", null, 0);
        Assert.True(HashIterator.Iterate(out string hashIterator, iterator) != 0);
        Assert.Equal("59a255662262f5ada32791b8c36e8ea7", hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    [Fact]
    public void TestHashMsdosDoszZip64()
    {
        var zip = new MockZipFile(512);
        zip.IsZip64 = true;
        zip.AddFile("README", 0x69FFE77E, 36);
        int zipSize = zip.Finalize("");
        MockFilereader.InitMockFilereader(); /* xUnit runs classes in parallel; keep the global ours */
        Assert.True(zipSize <= zip.Buffer.Length);

        byte[] zipContents = new byte[zipSize];
        Array.Copy(zip.Buffer, 0, zipContents, 0, zipSize);
        MockFilereader.MockFile(0, "game.dosz", zipContents, zipContents.Length);

        Assert.True(RcHash.GenerateFromFile(out string hashFile, ConsoleIds.RC_CONSOLE_MS_DOS, "game.dosz"));
        Assert.Equal("927dad0a57a2860267ab7bcdb8bc3f61", hashFile);
    }

    [Fact]
    public void TestHashMsdosDoszWithDosc()
    {
        var zip = new MockZipFile(512);
        zip.AddFile("FOLDER/", 0, 0);
        zip.AddFile("FOLDER/SUB.TXT", 0x4AD0CF31, 1);
        zip.AddFile("ROOT.TXT", 0xD3D99E8B, 1);
        int zipSize = zip.Finalize("TORRENTZIPPED-FD07C52C");
        MockFilereader.InitMockFilereader(); /* xUnit runs classes in parallel; keep the global ours */
        Assert.True(zipSize <= zip.Buffer.Length);

        byte[] zipContents = new byte[zipSize];
        Array.Copy(zip.Buffer, 0, zipContents, 0, zipSize);
        MockFilereader.MockFile(0, "game.dosz", zipContents, zipContents.Length);
        MockFilereader.MockFile(1, "game.dosc", zipContents, zipContents.Length);

        Assert.True(RcHash.GenerateFromFile(out string hashDosc, ConsoleIds.RC_CONSOLE_MS_DOS, "game.dosz"));
        Assert.Equal("dd0c0b0c170c30722784e5e962764c35", hashDosc);
    }

    [Fact]
    public void TestHashMsdosDoszWithParent()
    {
        var dosz = new MockZipFile(512);
        dosz.AddFile("FOLDER/", 0, 0);
        dosz.AddFile("FOLDER/SUB.TXT", 0x4AD0CF31, 1);
        dosz.AddFile("ROOT.TXT", 0xD3D99E8B, 1);
        int doszSize = dosz.Finalize("TORRENTZIPPED-FD07C52C");

        var dosc = new MockZipFile(512);
        dosc.AddFile("base.dosz.parent", 0, 0);
        dosc.AddFile("CHILD.TXT", 0x22B35429, 5);
        int doscSize = dosc.Finalize("");
        Assert.True(doscSize <= dosc.Buffer.Length);

        byte[] doszContents = new byte[doszSize];
        Array.Copy(dosz.Buffer, 0, doszContents, 0, doszSize);
        byte[] doscContents = new byte[doscSize];
        Array.Copy(dosc.Buffer, 0, doscContents, 0, doscSize);

        /* Add base dosz file and child dosz file which will get hashed together */
        MockFilereader.MockFile(0, "base.dosz", doszContents, doszContents.Length);
        MockFilereader.MockFile(1, "child.dosz", doscContents, doscContents.Length);

        Assert.True(RcHash.GenerateFromFile(out string hashDosz, ConsoleIds.RC_CONSOLE_MS_DOS, "child.dosz"));
        Assert.Equal("623c759476b8b5adb46362f8f0b60769", hashDosz);

        /* test file hash with base.dosc also existing */
        MockFilereader.MockFile(2, "base.dosc", doszContents, doszContents.Length);
        Assert.True(RcHash.GenerateFromFile(out string hashDosc2, ConsoleIds.RC_CONSOLE_MS_DOS, "child.dosz"));
        Assert.Equal("ecd9d776cbaad63094829d7b8dbe5959", hashDosc2);

        /* test file hash with child.dosc also existing */
        MockFilereader.MockFile(3, "child.dosc", doszContents, doszContents.Length);
        Assert.True(RcHash.GenerateFromFile(out string hashDosc3, ConsoleIds.RC_CONSOLE_MS_DOS, "child.dosz"));
        Assert.Equal("cb55c123936ad84479032ea6444cb1a1", hashDosc3);
    }
}
