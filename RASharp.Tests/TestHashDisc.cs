// Ported from rcheevos (MIT) — test/rhash/test_hash_disc.c (Phase 3 subset)
// Disc hashing vectors: 3DO, Jaguar CD, Dreamcast, GameCube, Neo Geo CD,
// PCE-CD, PC-FX, PSX, Sega CD, Saturn + generic whole-file consoles.
// PS2/PSP vectors land in Phase 4.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using RASharp;
using RASharp.Models;

namespace RASharp.Tests;

/// <summary>Ported from rcheevos (MIT) — test/rhash/test_hash_disc.c (Phase 3 subset) Disc hashing vectors: 3DO, Jaguar CD, Dreamcast, GameCube, Neo Geo CD, PCE-CD, PC-FX, </summary>
public class TestHashDisc
{
    public TestHashDisc()
    {
        MockFilereader.InitMockFilereader();
        MockFilereader.InitMockCdreader();
    }

    /* ========================================================================= */

    private static void TestHashFullFile(uint consoleId, string filename, int size, string expectedMd5)
    {
        var image = TestDataGen.GenerateGenericFile(size);

        MockFilereader.MockFile(0, filename, image, size);

        /* test full file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, consoleId, filename));
        Assert.Equal(expectedMd5, hashFile);

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, filename, null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    private static void TestHashM3U(uint consoleId, string filename, int size, string expectedMd5)
    {
        var image = TestDataGen.GenerateGenericFile(size);

        MockFilereader.MockFile(0, filename, image, size);
        MockFilereader.MockFileText(1, "test.m3u", filename);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, consoleId, "test.m3u"));
        Assert.Equal(expectedMd5, hashFile);

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "test.m3u", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    private static void TestHashUnknownFormat(uint consoleId, string path)
    {
        /* test file hash (won't match) */
        Assert.False(RcHash.GenerateFromFile(out var hashFile, consoleId, path));
        Assert.Equal("", hashFile);

        /* test file identification from iterator (won't match) */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, path, null, 0);
        Assert.Equal(0, HashIterator.Iterate(out var hashIterator, iterator));
        Assert.Equal("", hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHash3DoBin()
    {
        var image = TestDataGenDisc.Generate3DoBin(1, 123456, out var imageSize);
        const string expectedMd5 = "9b2266b8f5abed9c12cce780750e88d6";

        MockFilereader.MockFile(0, "game.bin", image, imageSize);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsole3Do, "game.bin"));

        /* test file identification from iterator */
        MockFilereader.MockFileSize(0, 45678901); /* must be > 32MB for iterator to consider CD formats for bin */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.bin", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a 3DO image.</summary>
    [Fact]
    public void TestHash3DoCue()
    {
        var image = TestDataGenDisc.Generate3DoBin(1, 9347, out var imageSize);
        const string expectedMd5 = "257d1d19365a864266b236214dbea29c";

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsole3Do, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a 3DO image.</summary>
    [Fact]
    public void TestHash3DoIso()
    {
        var image = TestDataGenDisc.Generate3DoBin(1, 9347, out var imageSize);
        const string expectedMd5 = "257d1d19365a864266b236214dbea29c";

        MockFilereader.MockFile(0, "game.iso", image, imageSize);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsole3Do, "game.iso"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.iso", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a 3DO image.</summary>
    [Fact]
    public void TestHash3DoInvalidHeader()
    {
        /* this is meant to simulate attempting to open a non-3DO CD. TODO: generate PSX CD */
        var image = TestDataGenDisc.Generate3DoBin(1, 12, out var imageSize);

        /* make the header not match */
        image[3] = 0x34;

        MockFilereader.MockFile(0, "game.bin", image, imageSize);

        /* test file hash */
        Assert.False(RcHash.GenerateFromFile(out _, ConsoleIds.RcConsole3Do, "game.bin"));
    }

    /// <summary>Tests hashing of a 3DO image.</summary>
    [Fact]
    public void TestHash3DoLaunchmeCaseInsensitive()
    {
        /* main executable for "Captain Quazar" is "launchme" */
        /* main executable for "Rise of the Robots" is "launchMe" */
        /* main executable for "Road Rash" is "LaunchMe" */
        /* main executable for "Sewer Shark" is "Launchme" */
        var image = TestDataGenDisc.Generate3DoBin(1, 6543, out var imageSize);
        const string expectedMd5 = "59622882e3261237e8a1e396825ae4f5";

        "launchme"u8.ToArray().CopyTo(image, 2048 + 0x14 + 0x48 + 0x20);
        MockFilereader.MockFile(0, "game.bin", image, imageSize);

        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsole3Do, "game.bin"));
        Assert.Equal(expectedMd5, hashFile);
    }

    /// <summary>Tests hashing of a 3DO image.</summary>
    [Fact]
    public void TestHash3DoNoLaunchme()
    {
        /* this case should not happen */
        var image = TestDataGenDisc.Generate3DoBin(1, 6543, out var imageSize);

        "filename"u8.ToArray().CopyTo(image, 2048 + 0x14 + 0x48 + 0x20);
        MockFilereader.MockFile(0, "game.bin", image, imageSize);

        Assert.False(RcHash.GenerateFromFile(out _, ConsoleIds.RcConsole3Do, "game.bin"));
    }

    /// <summary>Tests hashing of a 3DO image.</summary>
    [Fact]
    public void TestHash3DoLongDirectory()
    {
        /* root directory for "Dragon's Lair" uses more than one sector */
        var image = TestDataGenDisc.Generate3DoBin(3, 6543, out var imageSize);
        const string expectedMd5 = "8979e876ae502e0f79218f7ff7bd8c2a";

        MockFilereader.MockFile(0, "game.bin", image, imageSize);

        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsole3Do, "game.bin"));
        Assert.Equal(expectedMd5, hashFile);
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashAtariJaguarCd()
    {
        const string cueFile =
            "REM SESSION 01\n" +
            "FILE \"track01.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "REM SESSION 02\n" +
            "FILE \"track02.bin\" BINARY\n" +
            "  TRACK 02 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"track03.bin\" BINARY\n" +
            "  TRACK 03 AUDIO\n" +
            "    INDEX 01 00:00:00\n";
        var image = TestDataGenDisc.GenerateJaguarcdBin(2, 60024, 0, out var imageSize);
        const string expectedMd5 = "c324d95dc5831c2d5c470eefb18c346b";

        MockFilereader.MockFile(0, "game.cue", Encoding.ASCII.GetBytes(cueFile), cueFile.Length);
        MockFilereader.MockFile(1, "track02.bin", image, imageSize);

        RcHash.InitDefaultCdreader(); /* want to test actual FIRST_OF_SECOND_SESSION calculation */

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleAtariJaguarCd, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        MockFilereader.InitMockCdreader();

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hash atari jaguar cd byteswapped.</summary>
    [Fact]
    public void TestHashAtariJaguarCdByteswapped()
    {
        const string cueFile =
            "REM SESSION 01\n" +
            "FILE \"track01.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "REM SESSION 02\n" +
            "FILE \"track02.bin\" BINARY\n" +
            "  TRACK 02 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"track03.bin\" BINARY\n" +
            "  TRACK 03 AUDIO\n" +
            "    INDEX 01 00:00:00\n";
        var image = TestDataGenDisc.GenerateJaguarcdBin(2, 60024, 1, out var imageSize);
        const string expectedMd5 = "c324d95dc5831c2d5c470eefb18c346b";

        MockFilereader.MockFile(0, "game.cue", Encoding.ASCII.GetBytes(cueFile), cueFile.Length);
        MockFilereader.MockFile(1, "track02.bin", image, imageSize);

        RcHash.InitDefaultCdreader(); /* want to test actual FIRST_OF_SECOND_SESSION calculation */

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleAtariJaguarCd, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        MockFilereader.InitMockCdreader();

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hash atari jaguar cd track3.</summary>
    [Fact]
    public void TestHashAtariJaguarCdTrack3()
    {
        const string cueFile =
            "REM SESSION 01\n" +
            "FILE \"track01.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"track02.bin\" BINARY\n" +
            "  TRACK 02 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "REM SESSION 02\n" +
            "FILE \"track03.bin\" BINARY\n" +
            "  TRACK 03 AUDIO\n" +
            "    INDEX 01 00:00:00\n";
        var image = TestDataGenDisc.GenerateJaguarcdBin(1470, 99200, 1, out var imageSize);
        const string expectedMd5 = "060e9d223c584b581cf7d7ce17c0e5dc";

        MockFilereader.MockFile(0, "game.cue", Encoding.ASCII.GetBytes(cueFile), cueFile.Length);
        MockFilereader.MockFile(1, "track03.bin", image, imageSize);

        RcHash.InitDefaultCdreader(); /* want to test actual FIRST_OF_SECOND_SESSION calculation */

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleAtariJaguarCd, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        MockFilereader.InitMockCdreader();

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hash atari jaguar cd no header.</summary>
    [Fact]
    public void TestHashAtariJaguarCdNoHeader()
    {
        const string cueFile =
            "REM SESSION 01\n" +
            "FILE \"track01.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "REM SESSION 02\n" +
            "FILE \"track02.bin\" BINARY\n" +
            "  TRACK 02 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"track03.bin\" BINARY\n" +
            "  TRACK 03 AUDIO\n" +
            "    INDEX 01 00:00:00\n";
        var image = TestDataGenDisc.GenerateJaguarcdBin(2, 60024, 1, out var imageSize);

        /* make the header not match */
        image[2 + 64 + 12] = (byte)'B'; /* corrupt the header */

        MockFilereader.MockFile(0, "game.cue", Encoding.ASCII.GetBytes(cueFile), cueFile.Length);
        MockFilereader.MockFile(1, "track02.bin", image, imageSize);

        RcHash.InitDefaultCdreader(); /* want to test actual FIRST_OF_SECOND_SESSION calculation */

        TestHashUnknownFormat(ConsoleIds.RcConsoleAtariJaguarCd, "game.cue");

        MockFilereader.InitMockCdreader();
    }

    /// <summary>Tests hash atari jaguar cd no sessions.</summary>
    [Fact]
    public void TestHashAtariJaguarCdNoSessions()
    {
        const string cueFile =
            "FILE \"track01.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"track02.bin\" BINARY\n" +
            "  TRACK 02 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"track03.bin\" BINARY\n" +
            "  TRACK 03 AUDIO\n" +
            "    INDEX 01 00:00:00\n";
        var image = TestDataGenDisc.GenerateJaguarcdBin(2, 99200, 1, out var imageSize);

        MockFilereader.MockFile(0, "game.cue", Encoding.ASCII.GetBytes(cueFile), cueFile.Length);
        MockFilereader.MockFile(1, "track03.bin", image, imageSize);

        RcHash.InitDefaultCdreader(); /* want to test actual FIRST_OF_SECOND_SESSION calculation */

        TestHashUnknownFormat(ConsoleIds.RcConsoleAtariJaguarCd, "game.cue");

        MockFilereader.InitMockCdreader();
    }

    /// <summary>Tests hash atari jaguar cd homebrew.</summary>
    [Fact]
    public void TestHashAtariJaguarCdHomebrew()
    {
        /* Jaguar CD homebrew games all appear to have a common bootloader in the primary boot executable space. They only
         * differ in a secondary executable in the second track (part of the first session). This doesn't appear to be
         * intentional behavior based on the CD BIOS documentation, which states that all developer code should be in the
         * first track of the second session. I speculate this is done to work around the authentication logic. */
        const string cueFile =
            "REM SESSION 01\n" +
            "FILE \"track01.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"track02.bin\" BINARY\n" +
            "  TRACK 02 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "REM SESSION 02\n" +
            "FILE \"track03.bin\" BINARY\n" +
            "  TRACK 03 AUDIO\n" +
            "    INDEX 01 00:00:00\n";
        var image = TestDataGenDisc.GenerateJaguarcdBin(2, 45760, 1, out var imageSize);
        var image2 = TestDataGenDisc.GenerateJaguarcdBin(2, 986742, 1, out var imageSize2);
        const string expectedMd5 = "3fdf70e362c845524c9e447aacaed0a9";

        MockFilereader.MockFile(0, "game.cue", Encoding.ASCII.GetBytes(cueFile), cueFile.Length);
        MockFilereader.MockFile(1, "track03.bin", image, imageSize);
        image2[0x60] = 0x21; /* ATARI APPROVED DATA HEADER ATRI! */
        image2[0xA2] = image2[0x62];
        image2[0xA3] = image2[0x63];
        image2[0xA4] = image2[0x64];
        image2[0xA5] = image2[0x65];
        image2[0xA6] = image2[0x66];
        image2[0xA7] = image2[0x67];
        image2[0xA8] = image2[0x68];
        image2[0xA9] = image2[0x69];
        "RTKARTKARTKARTKA"u8.ToArray().CopyTo(image2, 0x62);
        "RTKARTKARTKARTKA"u8.ToArray().CopyTo(image2, 0x72);
        "RTKARTKARTKARTKA"u8.ToArray().CopyTo(image2, 0x82);
        "RTKARTKARTKARTKA"u8.ToArray().CopyTo(image2, 0x92);
        MockFilereader.MockFile(2, "track02.bin", image2, imageSize2);

        RcHash.InitDefaultCdreader(); /* want to test actual FIRST_OF_SECOND_SESSION calculation */
        HashDisc.JaguarCdHomebrewHash = "4e4114b2675eff21bb77dd41e141ddd6"; /* mock the hash of the homebrew bootloader */

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleAtariJaguarCd, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        /* cleanup */
        HashDisc.JaguarCdHomebrewHash = null;
        MockFilereader.InitMockCdreader();

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashDreamcastSingleBin()
    {
        var image = TestDataGenDisc.GenerateDreamcastBin(45000, 1458208, out var imageSize);
        const string expectedMd5 = "2a550500caee9f06e5d061fe10a46f6e";

        MockFilereader.MockFile(0, "track03.bin", image, imageSize);
        MockFilereader.MockFileFirstSector(0, 45000);
        MockFilereader.MockFileText(1, "game.gdi", "game.bin");
        MockFilereader.MockCdNumTracks(3);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleDreamcast, "game.gdi"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.gdi", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a Dreamcast image.</summary>
    [Fact]
    public void TestHashDreamcastSplitBin()
    {
        var image = TestDataGenDisc.GenerateDreamcastBin(548106, 1830912, out var imageSize);
        const string expectedMd5 = "771e56aff169230ede4505013a4bcf9f";

        MockFilereader.MockFileText(0, "game.gdi", "game.bin");
        MockFilereader.MockFile(1, "track03.bin", image, imageSize);
        MockFilereader.MockFileFirstSector(1, 45000);
        MockFilereader.MockFile(2, "track26.bin", image, imageSize);
        MockFilereader.MockFileFirstSector(2, 548106);
        MockFilereader.MockCdNumTracks(26);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleDreamcast, "game.gdi"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.gdi", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a Dreamcast image.</summary>
    [Fact]
    public void TestHashDreamcastCue()
    {
        const string cueFile =
            "FILE \"track01.bin\" BINARY\n" +
            "  TRACK 01 MODE1/2352\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"track02.bin\" BINARY\n" +
            "  TRACK 02 AUDIO\n" +
            "    INDEX 00 00:00:00\n" +
            "    INDEX 01 00:02:00\n" +
            "FILE \"track03.bin\" BINARY\n" +
            "  TRACK 03 MODE1/2352\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"track04.bin\" BINARY\n" +
            "  TRACK 04 AUDIO\n" +
            "    INDEX 00 00:00:00\n" +
            "    INDEX 01 00:02:00\n" +
            "FILE \"track05.bin\" BINARY\n" +
            "  TRACK 05 MODE1/2352\n" +
            "    INDEX 00 00:00:00\n" +
            "    INDEX 01 00:03:00\n";
        var image = TestDataGenDisc.ConvertTo2352(TestDataGenDisc.GenerateDreamcastBin(45000, 1697028, out var imageSize), ref imageSize, 45000);
        const string expectedMd5 = "c952864c3364591d2a8793ce2cfbf3a0";

        MockFilereader.MockFile(0, "game.cue", Encoding.ASCII.GetBytes(cueFile), cueFile.Length);
        MockFilereader.MockFile(1, "track01.bin", image, 1425312); /* 606 sectors */
        MockFilereader.MockFile(2, "track02.bin", image, 1589952); /* 676 sectors */
        MockFilereader.MockFile(3, "track03.bin", image, imageSize); /* 737 sectors */
        MockFilereader.MockFile(4, "track04.bin", image, 1237152); /* 526 sectors */
        MockFilereader.MockFile(5, "track05.bin", image, imageSize);

        RcHash.InitDefaultCdreader(); /* want to test actual first_track_sector calculation */

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleDreamcast, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        /* cleanup */
        MockFilereader.InitMockCdreader();

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashGamecube()
    {
        var image = TestDataGenDisc.GenerateGamecubeIso(32, out var imageSize);
        const string expectedMd5 = "c7803b704fa43d22d8f6e55f4789cb45";

        MockFilereader.MockFile(0, "test.iso", image, imageSize);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleGamecube, "test.iso"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "test.iso", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
        Assert.Equal(32 * 1024 * 1024, imageSize);
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashNeogeoCd()
    {
        const string iplTxt = "FIXA.FIX,0,0\r\nPROG.PRG,0,0\r\nSOUND.PCM,0,0\r\n\x1a";
        const int progPrgSize = 273470;
        var progPrg = TestDataGen.GenerateGenericFile(progPrgSize);
        var image = TestDataGenDisc.GenerateIso9660Bin(160, "TEST", out var imageSize);
        const string expectedMd5 = "96f35b20c6cf902286da45e81a50b2a3";

        var iplBytes = Encoding.ASCII.GetBytes(iplTxt);
        TestDataGenDisc.GenerateIso9660File(image, "IPL.TXT", iplBytes, iplBytes.Length);
        TestDataGenDisc.GenerateIso9660File(image, "PROG.PRG", progPrg, progPrgSize);

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleNeoGeoCd, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a Neo Geo CD image.</summary>
    [Fact]
    public void TestHashNeogeoCdMultiplePrg()
    {
        const string iplTxt = "FIXA.FIX,0,0\r\nPROG1.PRG,0,0\r\nSOUND.PCM,0,0\r\nPROG2.PRG,0,44000\r\n\x1a";
        const int prog1PrgSize = 273470;
        var prog1Prg = TestDataGen.GenerateGenericFile(prog1PrgSize);
        const int prog2PrgSize = 13768;
        var prog2Prg = TestDataGen.GenerateGenericFile(prog2PrgSize);
        var image = TestDataGenDisc.GenerateIso9660Bin(160, "TEST", out var imageSize);
        const string expectedMd5 = "d62df483c4786d3c63f27b6c5f17eeca";

        var iplBytes = Encoding.ASCII.GetBytes(iplTxt);
        TestDataGenDisc.GenerateIso9660File(image, "IPL.TXT", iplBytes, iplBytes.Length);
        TestDataGenDisc.GenerateIso9660File(image, "PROG1.PRG", prog1Prg, prog1PrgSize);
        TestDataGenDisc.GenerateIso9660File(image, "PROG2.PRG", prog2Prg, prog2PrgSize);

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleNeoGeoCd, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a Neo Geo CD image.</summary>
    [Fact]
    public void TestHashNeogeoCdLowercaseIplContents()
    {
        const string iplTxt = "fixa.fix,0,0\r\nprog.prg,0,0\r\nsound.pcm,0,0\r\n\x1a";
        const int progPrgSize = 273470;
        var progPrg = TestDataGen.GenerateGenericFile(progPrgSize);
        var image = TestDataGenDisc.GenerateIso9660Bin(160, "TEST", out var imageSize);
        const string expectedMd5 = "96f35b20c6cf902286da45e81a50b2a3";

        var iplBytes = Encoding.ASCII.GetBytes(iplTxt);
        TestDataGenDisc.GenerateIso9660File(image, "IPL.TXT", iplBytes, iplBytes.Length);
        TestDataGenDisc.GenerateIso9660File(image, "PROG.PRG", progPrg, progPrgSize);

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleNeoGeoCd, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashPceCd()
    {
        var image = TestDataGenDisc.GeneratePceCdBin(72, out var imageSize);
        const string expectedMd5 = "6565819195a49323e080e7539b54f251";

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePcEngineCd, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a PC Engine image.</summary>
    [Fact]
    public void TestHashPceCdInvalidHeader()
    {
        var image = TestDataGenDisc.GeneratePceCdBin(72, out var imageSize);

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* make the header not match */
        image[2048 + 0x24] = 0x34;

        TestHashUnknownFormat(ConsoleIds.RcConsolePcEngineCd, "game.cue");
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashPcfx()
    {
        var image = TestDataGenDisc.GeneratePcfxBin(72, out var imageSize);
        const string expectedMd5 = "0a03af66559b8529c50c4e7788379598";

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePcfx, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a PC-FX image.</summary>
    [Fact]
    public void TestHashPcfxInvalidHeader()
    {
        var image = TestDataGenDisc.GeneratePcfxBin(72, out var imageSize);

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* make the header not match */
        image[12] = 0x34;

        TestHashUnknownFormat(ConsoleIds.RcConsolePcfx, "game.cue");
    }

    /// <summary>Tests hashing of a PC-FX image.</summary>
    [Fact]
    public void TestHashPcfxPceCd()
    {
        /* Battle Heat is formatted as a PC-Engine CD */
        var image = TestDataGenDisc.GeneratePceCdBin(72, out var imageSize);
        const string expectedMd5 = "6565819195a49323e080e7539b54f251";

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");
        MockFilereader.MockFile(2, "game2.bin", image, imageSize); /* PC-Engine CD check only applies to track 2 */

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePcfx, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashPsxCd()
    {
        /* BOOT=cdrom:\SLUS_007.45 */
        var image = TestDataGenDisc.GeneratePsxBin("SLUS_007.45", 0x07D800, out var imageSize);
        const string expectedMd5 = "db433fb038cde4fb15c144e8c7dea6e3";

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePlaystation, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a PlayStation image.</summary>
    [Fact]
    [SuppressMessage("ReSharper", "ConvertToConstant.Local")]
    public void TestHashPsxCdNoSystemCnf()
    {
        const uint binarySize = 0x12000;
        const uint sectorsNeeded = ((binarySize + 2047) / 2048) + 20;
        var image = TestDataGenDisc.GenerateIso9660Bin(sectorsNeeded, "HOMEBREW", out var imageSize);
        const string expectedMd5 = "e494c79a7315be0dc3e8571c45df162c";

        var exe = TestDataGenDisc.GenerateIso9660File(image, "PSX.EXE", null, (int)binarySize);
        "PS-X EXE"u8.ToArray().CopyTo(image, exe);
        /* binary_size is a runtime parameter in the C reference (data.c:
         * generate_psx_bin), so the size-field shifts below are runtime
         * expressions — keep adjustedSize non-const to mirror that */
        var adjustedSize = binarySize - 2048;
        image[exe + 28] = (byte)(adjustedSize & 0xFF);
        image[exe + 29] = (byte)((adjustedSize >> 8) & 0xFF);
        image[exe + 30] = (byte)((adjustedSize >> 16) & 0xFF);
        image[exe + 31] = (byte)((adjustedSize >> 24) & 0xFF);

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePlaystation, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a PlayStation image.</summary>
    [Fact]
    public void TestHashPsxCdExeInSubfolder()
    {
        /* BOOT=cdrom:\bin\SCES_012.37 */
        var image = TestDataGenDisc.GeneratePsxBin("bin\\SCES_012.37", 0x07D800, out var imageSize);
        const string expectedMd5 = "674018e23a4052113665dfb264e9c2fc";

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePlaystation, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a PlayStation image.</summary>
    [Fact]
    public void TestHashPsxCdExtraSlash()
    {
        /* BOOT=cdrom:\\SLUS_007.45 */
        var image = TestDataGenDisc.GeneratePsxBin("\\SLUS_007.45", 0x07D800, out var imageSize);
        const string expectedMd5 = "db433fb038cde4fb15c144e8c7dea6e3";

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePlaystation, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashPs2Iso()
    {
        var image = TestDataGenDisc.GeneratePs2Bin("SLUS_200.64", 0x07D800, out var imageSize);
        const string expectedMd5 = "01a517e4ad72c6c2654d1b839be7579d";

        MockFilereader.MockFile(0, "game.iso", image, imageSize);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePlaystation2, "game.iso"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.iso", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a PlayStation 2 image.</summary>
    [Fact]
    public void TestHashPs2Psx()
    {
        var image = TestDataGenDisc.GeneratePsxBin("SLUS_007.45", 0x07D800, out var imageSize);
        const string expectedMd5 = "db433fb038cde4fb15c144e8c7dea6e3"; /* PSX hash */

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.False(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePlaystation2, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator); /* PSX hash */
        Assert.Equal(0, HashIterator.Iterate(out var hashIterator2, iterator));
        HashIterator.DestroyIterator(iterator);

        /* validation (should not generate PS2 hash for PSX file) */
        Assert.Equal("", hashFile);
        Assert.Equal("", hashIterator2);
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashPsp()
    {
        const int paramSfoSize = 690;
        var paramSfo = TestDataGen.GenerateGenericFile(paramSfoSize);
        const int ebootBinSize = 273470;
        var ebootBin = TestDataGen.GenerateGenericFile(ebootBinSize);
        var image = TestDataGenDisc.GenerateIso9660Bin(160, "TEST", out var imageSize);
        const string expectedMd5 = "27ec2f9b7238b2ef29af31ddd254f201";

        TestDataGenDisc.GenerateIso9660File(image, "PSP_GAME\\PARAM.SFO", paramSfo, paramSfoSize);
        TestDataGenDisc.GenerateIso9660File(image, @"PSP_GAME\SYSDIR\EBOOT.BIN", ebootBin, ebootBinSize);

        MockFilereader.MockFile(0, "game.iso", image, imageSize);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePsp, "game.iso"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.iso", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a PSP image.</summary>
    [Fact]
    public void TestHashPspVideo()
    {
        const int paramSfoSize = 690;
        var paramSfo = TestDataGen.GenerateGenericFile(paramSfoSize);
        const int ebootBinSize = 273470;
        var ebootBin = TestDataGen.GenerateGenericFile(ebootBinSize);
        var image = TestDataGenDisc.GenerateIso9660Bin(160, "TEST", out var imageSize);

        /* UMD video disc may have an UPDATE folder, but nothing in the PSP_GAME or SYSDIR folders. */
        TestDataGenDisc.GenerateIso9660File(image, @"PSP_GAME\SYSDIR\UPDATE\EBOOT.BIN", ebootBin, ebootBinSize);
        /* the PARAM.SFO file is in the UMD_VIDEO folder. */
        TestDataGenDisc.GenerateIso9660File(image, "UMD_VIDEO\\PARAM.SFO", paramSfo, paramSfoSize);

        MockFilereader.MockFile(0, "game.iso", image, imageSize);

        /* test file hash */
        Assert.False(RcHash.GenerateFromFile(out _, ConsoleIds.RcConsolePsp, "game.iso"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.iso", null, 0);
        Assert.Equal(0, HashIterator.Iterate(out _, iterator));
        HashIterator.DestroyIterator(iterator);
    }

    /// <summary>Tests hashing of a PSP image.</summary>
    [Fact]
    public void TestHashPspHomebrew()
    {
        const int imageSize = 3532124;
        var image = TestDataGen.GenerateGenericFile(imageSize);
        const string expectedMd5 = "fcde8760893b09e508e5f4fe642eb132";

        MockFilereader.MockFile(0, "eboot.pbp", image, imageSize);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePsp, "eboot.pbp"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "eboot.pbp", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashSegaCd()
    {
        const int imageSize = 512;
        var image = TestDataGen.GenerateGenericFile(imageSize);
        const string expectedMd5 = "574498e1453cb8934df60c4ab906e783";

        "SEGADISCSYSTEM  "u8.ToArray().CopyTo(image, 0);

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleSegaCd, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a Sega CD image.</summary>
    [Fact]
    public void TestHashSegaCdInvalidHeader()
    {
        const int imageSize = 512;
        var image = TestDataGen.GenerateGenericFile(imageSize);

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        TestHashUnknownFormat(ConsoleIds.RcConsoleSegaCd, "game.cue");
    }

    /// <summary>Tests hashing of a Sega Saturn image.</summary>
    [Fact]
    public void TestHashSaturn()
    {
        const int imageSize = 512;
        var image = TestDataGen.GenerateGenericFile(imageSize);
        const string expectedMd5 = "4cd9c8e41cd8d137be15bbe6a93ae1d8";

        "SEGA SEGASATURN "u8.ToArray().CopyTo(image, 0);

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleSaturn, "game.cue"));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cue", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        HashIterator.DestroyIterator(iterator);

        Assert.Equal(expectedMd5, hashFile);
        Assert.Equal(expectedMd5, hashIterator);
    }

    /// <summary>Tests hashing of a Sega Saturn image.</summary>
    [Fact]
    public void TestHashSaturnInvalidHeader()
    {
        const int imageSize = 512;
        var image = TestDataGen.GenerateGenericFile(imageSize);

        MockFilereader.MockFile(0, "game.bin", image, imageSize);
        MockFilereader.MockFileText(1, "game.cue", "game.bin");

        TestHashUnknownFormat(ConsoleIds.RcConsoleSaturn, "game.cue");
    }

    /* ========================================================================= */

    /// <summary>=========================================================================</summary>
    [Fact]
    public void TestHashFullFileAmstradPc()
    {
        TestHashFullFile(ConsoleIds.RcConsoleAmstradPc, "test.dsk", 194816, "9d616e4ad3f16966f61422c57e22aadd");
        TestHashM3U(ConsoleIds.RcConsoleAmstradPc, "test.dsk", 194816, "9d616e4ad3f16966f61422c57e22aadd");
    }

    /// <summary>Tests hash full file apple ii.</summary>
    [Fact]
    public void TestHashFullFileAppleIi()
    {
        TestHashFullFile(ConsoleIds.RcConsoleAppleIi, "test.nib", 232960, "96e8d33bdc385fd494327d6e6791cbe4");
        TestHashFullFile(ConsoleIds.RcConsoleAppleIi, "test.dsk", 143360, "88be638f4d78b4072109e55f13e8a0ac");
        TestHashM3U(ConsoleIds.RcConsoleAppleIi, "test.dsk", 143360, "88be638f4d78b4072109e55f13e8a0ac");
    }

    /// <summary>Tests hash full file commodore64.</summary>
    [Fact]
    public void TestHashFullFileCommodore64()
    {
        TestHashFullFile(ConsoleIds.RcConsoleCommodore64, "test.nib", 327936, "e7767d32b23e3fa62c5a250a08caeba3");
        TestHashFullFile(ConsoleIds.RcConsoleCommodore64, "test.d64", 174848, "ecd5a8ef4e77f2e9469d9b6e891394f0");
        TestHashM3U(ConsoleIds.RcConsoleCommodore64, "test.d64", 174848, "ecd5a8ef4e77f2e9469d9b6e891394f0");
    }

    /// <summary>Tests hash full file msx.</summary>
    [Fact]
    public void TestHashFullFileMsx()
    {
        TestHashFullFile(ConsoleIds.RcConsoleMsx, "test.dsk", 737280, "0e73fe94e5f2e2d8216926eae512b7a6");
        TestHashM3U(ConsoleIds.RcConsoleMsx, "test.dsk", 737280, "0e73fe94e5f2e2d8216926eae512b7a6");
    }

    /// <summary>Tests hash full file pc8800.</summary>
    [Fact]
    public void TestHashFullFilePc8800()
    {
        TestHashFullFile(ConsoleIds.RcConsolePc8800, "test.d88", 348288, "8cca4121bf87200f45e91b905a9f5afd");
        TestHashM3U(ConsoleIds.RcConsolePc8800, "test.d88", 348288, "8cca4121bf87200f45e91b905a9f5afd");
    }

    /// <summary>Tests hash full file zx spectrum.</summary>
    [Fact]
    public void TestHashFullFileZxSpectrum()
    {
        TestHashFullFile(ConsoleIds.RcConsoleZxSpectrum, "test.tap", 1596, "714a9f455e616813dd5421c5b347e5e5");
        TestHashFullFile(ConsoleIds.RcConsoleZxSpectrum, "test.tzx", 14971, "93723e6d1100f9d1d448a27cf6618c47");
    }
}
