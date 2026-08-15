// Ported from rcheevos (MIT) — test/rhash/test_hash.c (Phase 1 subset)
// Full-file hashing, m3u playlist handling, and the extension-handler table
// order test. test_hash_file_without_ext is deferred to Phase 2 (needs NES).

using System.Text;
using RetroAchievementsSharp.Models;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace RetroAchievementsSharp.Tests;

/// <summary>Ported from rcheevos (MIT) — test/rhash/test_hash.c (Phase 1 subset) Full-file hashing, m3u playlist handling, and the extension-handler table order test. test_</summary>
public class TestHash
{
    public TestHash()
    {
        MockFilereader.InitMockFilereader();
    }

    private static void TestHashFullFile(uint consoleId, string filename, int size, string expectedMd5)
    {
        var image = TestDataGen.GenerateGenericFile(size);

        MockFilereader.MockFile(0, filename, image, size);

        /* test full buffer hash */
        Assert.True(RcHash.GenerateFromBuffer(out var hashBuffer, consoleId, image, size));
        Assert.Equal(expectedMd5, hashBuffer);

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

    private static void AssertValidM3U(string discFilename, string m3UFilename, string m3UContents)
    {
        const int size = 131072;
        const string expectedMd5 = "a0f425b23200568132ba76b2405e3933";
        var image = TestDataGen.GenerateGenericFile(size);

        MockFilereader.MockFile(0, discFilename, image, size);
        MockFilereader.MockFileText(1, m3UFilename, m3UContents);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsolePc8800, m3UFilename));
        Assert.Equal(expectedMd5, hashFile);

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, m3UFilename, null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    /// <summary>Tests hash m3u buffered.</summary>
    [Fact]
    public void TestHashM3UBuffered()
    {
        const int size = 131072;
        const string expectedMd5 = "a0f425b23200568132ba76b2405e3933";
        const string m3UFilename = "test.m3u";
        const string filename = "test.d88";
        var image = TestDataGen.GenerateGenericFile(size);
        var m3UContents = Encoding.ASCII.GetBytes(filename);

        MockFilereader.MockFile(0, filename, image, size);
        MockFilereader.MockFile(1, m3UFilename, m3UContents, m3UContents.Length);

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, m3UFilename, m3UContents, m3UContents.Length);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    /// <summary>Tests hash m3u with comments.</summary>
    [Fact]
    public void TestHashM3UWithComments()
    {
        AssertValidM3U("test.d88", "test.m3u",
            "#EXTM3U\r\n\r\n#EXTBYT:131072\r\ntest.d88\r\n");
    }

    /// <summary>Tests hash m3u empty.</summary>
    [Fact]
    public void TestHashM3UEmpty()
    {
        const string m3UFilename = "test.m3u";
        const string m3UContents = "#EXTM3U\r\n\r\n#EXTBYT:131072\r\n";

        MockFilereader.MockFileText(0, m3UFilename, m3UContents);

        /* test file hash */
        Assert.False(RcHash.GenerateFromFile(out _, ConsoleIds.RcConsolePc8800, m3UFilename));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, m3UFilename, null, 0);
        Assert.Equal(0, HashIterator.Iterate(out _, iterator));
        HashIterator.DestroyIterator(iterator);
    }

    /// <summary>Tests hash m3u trailing whitespace.</summary>
    [Fact]
    public void TestHashM3UTrailingWhitespace()
    {
        AssertValidM3U("test.d88", "test.m3u",
            "#EXTM3U  \r\n  \r\n#EXTBYT:131072  \r\ntest.d88  \t  \r\n");
    }

    /// <summary>Tests hash m3u line ending.</summary>
    [Fact]
    public void TestHashM3ULineEnding()
    {
        AssertValidM3U("test.d88", "test.m3u",
            "#EXTM3U\n\n#EXTBYT:131072\ntest.d88\n");
    }

    /// <summary>Tests hash m3u extension case.</summary>
    [Fact]
    public void TestHashM3UExtensionCase()
    {
        AssertValidM3U("test.D88", "test.M3U",
            "#EXTM3U\r\n\r\n#EXTBYT:131072\r\ntest.D88\r\n");
    }

    /// <summary>Tests hash m3u relative path.</summary>
    [Fact]
    public void TestHashM3URelativePath()
    {
        AssertValidM3U("folder1/folder2/test.d88", "folder1/test.m3u",
            "#EXTM3U\r\n\r\n#EXTBYT:131072\r\nfolder2/test.d88");
    }

    /// <summary>Tests hash m3u absolute path.</summary>
    /// <param name="absolutePath">the absolute path parameter</param>
    [Theory]
    [InlineData("/absolute/test.d88")]
    [InlineData(@"\absolute\test.d88")]
    [InlineData(@"C:\absolute\test.d88")]
    [InlineData(@"\\server\absolute\test.d88")]
    [InlineData("samba:/absolute/test.d88")]
    public void TestHashM3UAbsolutePath(string absolutePath)
    {
        var m3UContents = "#EXTM3U\r\n\r\n#EXTBYT:131072\r\n" + absolutePath;
        AssertValidM3U(absolutePath, "relative/test.m3u", m3UContents);
    }

    /// <summary>Tests hash handler table order.</summary>
    [Fact]
    public void TestHashHandlerTableOrder()
    {
        var handlers = HashIterator.GetIteratorExtHandlers(out _);
        for (var index = 1; index < handlers.Length; ++index)
        {
            Assert.True(string.CompareOrdinal(handlers[index].Ext, handlers[index - 1].Ext) > 0,
                $"handler[{handlers[index].Ext}] after handler[{handlers[index - 1].Ext}]");
        }
    }

    /* ========================================================================= */
    /* test_hash() suite — full-file and m3u console vectors                     */

    /// <summary>========================================================================= test_hash() suite — full-file and m3u console vectors</summary>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="filename">the filename parameter</param>
    /// <param name="size">the size</param>
    /// <param name="expectedMd5">the expected md5 parameter</param>
    [Theory]
    [InlineData((uint)37, "test.dsk", 194816, "9d616e4ad3f16966f61422c57e22aadd")] /* Amstrad CPC */
    [InlineData((uint)38, "test.nib", 232960, "96e8d33bdc385fd494327d6e6791cbe4")] /* Apple II */
    [InlineData((uint)38, "test.dsk", 143360, "88be638f4d78b4072109e55f13e8a0ac")]
    [InlineData((uint)30, "test.nib", 327936, "e7767d32b23e3fa62c5a250a08caeba3")] /* Commodore 64 */
    [InlineData((uint)30, "test.d64", 174848, "ecd5a8ef4e77f2e9469d9b6e891394f0")]
    [InlineData((uint)29, "test.dsk", 737280, "0e73fe94e5f2e2d8216926eae512b7a6")] /* MSX */
    [InlineData((uint)47, "test.d88", 348288, "8cca4121bf87200f45e91b905a9f5afd")] /* PC-8800 */
    [InlineData((uint)59, "test.tap", 1596, "714a9f455e616813dd5421c5b347e5e5")] /* ZX Spectrum */
    [InlineData((uint)59, "test.tzx", 14971, "93723e6d1100f9d1d448a27cf6618c47")]
    public void TestHashFullFileVectors(uint consoleId, string filename, int size, string expectedMd5)
    {
        TestHashFullFile(consoleId, filename, size, expectedMd5);
    }

    /// <summary>Tests hash m3u vectors.</summary>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="filename">the filename parameter</param>
    /// <param name="size">the size</param>
    /// <param name="expectedMd5">the expected md5 parameter</param>
    [Theory]
    [InlineData((uint)37, "test.dsk", 194816, "9d616e4ad3f16966f61422c57e22aadd")] /* Amstrad CPC */
    [InlineData((uint)38, "test.dsk", 143360, "88be638f4d78b4072109e55f13e8a0ac")] /* Apple II */
    [InlineData((uint)30, "test.d64", 174848, "ecd5a8ef4e77f2e9469d9b6e891394f0")] /* Commodore 64 */
    [InlineData((uint)29, "test.dsk", 737280, "0e73fe94e5f2e2d8216926eae512b7a6")] /* MSX */
    [InlineData((uint)47, "test.d88", 348288, "8cca4121bf87200f45e91b905a9f5afd")] /* PC-8800 */
    public void TestHashM3UVectors(uint consoleId, string filename, int size, string expectedMd5)
    {
        TestHashM3U(consoleId, filename, size, expectedMd5);
    }
}
