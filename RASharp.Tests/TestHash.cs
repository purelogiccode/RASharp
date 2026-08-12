// Ported from rcheevos (MIT) — test/rhash/test_hash.c (Phase 1 subset)
// Full-file hashing, m3u playlist handling, and the extension-handler table
// order test. test_hash_file_without_ext is deferred to Phase 2 (needs NES).

using RASharp.Core;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace RASharp.Tests;


using RASharp.Core.Models;
/// <summary>Ported from rcheevos (MIT) — test/rhash/test_hash.c (Phase 1 subset) Full-file hashing, m3u playlist handling, and the extension-handler table order test. test_</summary>
public class TestHash
{
    public TestHash()
    {
        MockFilereader.InitMockFilereader();
    }

    private void TestHashFullFile(uint consoleId, string filename, int size, string expectedMd5)
    {
        byte[] image = TestDataGen.GenerateGenericFile(size);

        MockFilereader.MockFile(0, filename, image, size);

        /* test full buffer hash */
        Assert.True(RcHash.GenerateFromBuffer(out string hashBuffer, consoleId, image, size));
        Assert.Equal(expectedMd5, hashBuffer);

        /* test full file hash */
        Assert.True(RcHash.GenerateFromFile(out string hashFile, consoleId, filename));
        Assert.Equal(expectedMd5, hashFile);

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, filename, null, 0);
        Assert.True(HashIterator.Iterate(out string hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    private void TestHashM3u(uint consoleId, string filename, int size, string expectedMd5)
    {
        byte[] image = TestDataGen.GenerateGenericFile(size);

        MockFilereader.MockFile(0, filename, image, size);
        MockFilereader.MockFileText(1, "test.m3u", filename);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out string hashFile, consoleId, "test.m3u"));
        Assert.Equal(expectedMd5, hashFile);

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "test.m3u", null, 0);
        Assert.True(HashIterator.Iterate(out string hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    private void AssertValidM3u(string discFilename, string m3uFilename, string m3uContents)
    {
        const int size = 131072;
        const string expectedMd5 = "a0f425b23200568132ba76b2405e3933";
        byte[] image = TestDataGen.GenerateGenericFile(size);

        MockFilereader.MockFile(0, discFilename, image, size);
        MockFilereader.MockFileText(1, m3uFilename, m3uContents);

        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out string hashFile, ConsoleIds.RC_CONSOLE_PC8800, m3uFilename));
        Assert.Equal(expectedMd5, hashFile);

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, m3uFilename, null, 0);
        Assert.True(HashIterator.Iterate(out string hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

/// <summary>Tests hash m3u buffered.</summary>
    [Fact]
    public void TestHashM3uBuffered()
    {
        const int size = 131072;
        const string expectedMd5 = "a0f425b23200568132ba76b2405e3933";
        const string m3uFilename = "test.m3u";
        const string filename = "test.d88";
        byte[] image = TestDataGen.GenerateGenericFile(size);
        byte[] m3uContents = System.Text.Encoding.ASCII.GetBytes(filename);

        MockFilereader.MockFile(0, filename, image, size);
        MockFilereader.MockFile(1, m3uFilename, m3uContents, m3uContents.Length);

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, m3uFilename, m3uContents, m3uContents.Length);
        Assert.True(HashIterator.Iterate(out string hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

/// <summary>Tests hash m3u with comments.</summary>
    [Fact]
    public void TestHashM3uWithComments()
    {
        AssertValidM3u("test.d88", "test.m3u",
            "#EXTM3U\r\n\r\n#EXTBYT:131072\r\ntest.d88\r\n");
    }

/// <summary>Tests hash m3u empty.</summary>
    [Fact]
    public void TestHashM3uEmpty()
    {
        const string m3uFilename = "test.m3u";
        const string m3uContents = "#EXTM3U\r\n\r\n#EXTBYT:131072\r\n";

        MockFilereader.MockFileText(0, m3uFilename, m3uContents);

        /* test file hash */
        Assert.False(RcHash.GenerateFromFile(out _, ConsoleIds.RC_CONSOLE_PC8800, m3uFilename));

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, m3uFilename, null, 0);
        Assert.Equal(0, HashIterator.Iterate(out _, iterator));
        HashIterator.DestroyIterator(iterator);
    }

/// <summary>Tests hash m3u trailing whitespace.</summary>
    [Fact]
    public void TestHashM3uTrailingWhitespace()
    {
        AssertValidM3u("test.d88", "test.m3u",
            "#EXTM3U  \r\n  \r\n#EXTBYT:131072  \r\ntest.d88  \t  \r\n");
    }

/// <summary>Tests hash m3u line ending.</summary>
    [Fact]
    public void TestHashM3uLineEnding()
    {
        AssertValidM3u("test.d88", "test.m3u",
            "#EXTM3U\n\n#EXTBYT:131072\ntest.d88\n");
    }

/// <summary>Tests hash m3u extension case.</summary>
    [Fact]
    public void TestHashM3uExtensionCase()
    {
        AssertValidM3u("test.D88", "test.M3U",
            "#EXTM3U\r\n\r\n#EXTBYT:131072\r\ntest.D88\r\n");
    }

/// <summary>Tests hash m3u relative path.</summary>
    [Fact]
    public void TestHashM3uRelativePath()
    {
        AssertValidM3u("folder1/folder2/test.d88", "folder1/test.m3u",
            "#EXTM3U\r\n\r\n#EXTBYT:131072\r\nfolder2/test.d88");
    }

/// <summary>Tests hash m3u absolute path.</summary>
/// <param name="absolutePath">the absolute path parameter</param>
    [Theory]
    [InlineData("/absolute/test.d88")]
    [InlineData("\\absolute\\test.d88")]
    [InlineData("C:\\absolute\\test.d88")]
    [InlineData("\\\\server\\absolute\\test.d88")]
    [InlineData("samba:/absolute/test.d88")]
    public void TestHashM3uAbsolutePath(string absolutePath)
    {
        string m3uContents = "#EXTM3U\r\n\r\n#EXTBYT:131072\r\n" + absolutePath;
        AssertValidM3u(absolutePath, "relative/test.m3u", m3uContents);
    }

/// <summary>Tests hash handler table order.</summary>
    [Fact]
    public void TestHashHandlerTableOrder()
    {
        ExtHandlerEntry[] handlers = HashIterator.GetIteratorExtHandlers(out _);
        for (int index = 1; index < handlers.Length; ++index)
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
    public void TestHashM3uVectors(uint consoleId, string filename, int size, string expectedMd5)
    {
        TestHashM3u(consoleId, filename, size, expectedMd5);
    }
}
