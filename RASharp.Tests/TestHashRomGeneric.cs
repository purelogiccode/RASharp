// Ported from rcheevos (MIT) — test/rhash/test_hash_rom.c (Phase 1 subset)
// Generic whole-file console vectors (test_hash_full_file entries only;
// cartridge algorithms land in Phase 2).

using RASharp.Core;
using RASharp.Core.Models;

namespace RASharp.Tests;

/// <summary>Ported from rcheevos (MIT) — test/rhash/test_hash_rom.c (Phase 1 subset) Generic whole-file console vectors (test_hash_full_file entries only; cartridge algorit</summary>
public class TestHashRomGeneric
{
    public TestHashRomGeneric()
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

        Assert.True(RcHash.GenerateFromFile(out var hashFile, consoleId, "test.m3u"));
        Assert.Equal(expectedMd5, hashFile);

        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "test.m3u", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    [Theory]
    [InlineData((uint)73, "test.bin", 4096, "572686c3a073162e4ec6eff86e6f6e3a")] /* Arcadia 2001 */
    [InlineData((uint)25, "test.bin", 2048, "02c3f2fa186388ba8eede9147fb431c4")] /* Atari 2600 */
    [InlineData((uint)17, "test.jag", 0x400000, "a247ec8a8c42e18fcb80702dfadac14b")] /* Atari Jaguar */
    [InlineData((uint)44, "test.col", 16384, "455f07d8500f3fabc54906737866167f")] /* Colecovision */
    [InlineData((uint)75, "test.pgm", 4096, "572686c3a073162e4ec6eff86e6f6e3a")] /* Elektor TV Games Computer */
    [InlineData((uint)75, "test.tvc", 1861, "37097124a29aff663432d049654a17dc")]
    [InlineData((uint)57, "test.bin", 2048, "02c3f2fa186388ba8eede9147fb431c4")] /* Fairchild Channel F */
    [InlineData((uint)57, "test.chf", 2048, "02c3f2fa186388ba8eede9147fb431c4")]
    [InlineData((uint)4, "test.gb", 131072, "a0f425b23200568132ba76b2405e3933")] /* Gameboy */
    [InlineData((uint)6, "test.gbc", 2097152, "cf86acf519625a25a17b1246975e90ae")] /* Gameboy Color */
    [InlineData((uint)6, "test.gba", 4194304, "a247ec8a8c42e18fcb80702dfadac14b")] /* Gameboy Advance (console id as in C test) */
    [InlineData((uint)15, "test.gg", 524288, "68f0f13b598e0b66461bc578375c3888")] /* Game Gear */
    [InlineData((uint)45, "test.bin", 8192, "ce1127f881b40ce6a67ecefba50e2835")] /* Intellivision */
    [InlineData((uint)74, "test.bin", 2048, "02c3f2fa186388ba8eede9147fb431c4")] /* Interton VC 4000 */
    [InlineData((uint)23, "test.bin", 4096, "572686c3a073162e4ec6eff86e6f6e3a")] /* Magnavox Odyssey 2 */
    [InlineData((uint)11, "test.sms", 131072, "a0f425b23200568132ba76b2405e3933")] /* Master System */
    [InlineData((uint)1, "test.md", 1048576, "da9461b3b0f74becc3ccf6c2a094c516")] /* Mega Drive */
    [InlineData((uint)69, "test.bin", 65536, "8e6576cd5c21e44e0bbfc4480577b040")] /* Mega Duck */
    [InlineData((uint)14, "test.ngc", 2097152, "cf86acf519625a25a17b1246975e90ae")] /* Neo Geo Pocket */
    [InlineData((uint)32, "test.tap", 18119, "953a2baa3232c63286aeae36b2172cef")] /* Oric */
    [InlineData((uint)24, "test.min", 524288, "68f0f13b598e0b66461bc578375c3888")] /* Pokemon Mini */
    [InlineData((uint)10, "test.bin", 3145728, "07d733f252896ec41b4fd521fe610e2c")] /* Sega 32X */
    [InlineData((uint)33, "test.sg", 32768, "6a2305a2b6675a97ff792709be1ca857")] /* SG-1000 */
    /* SUPER_CASSETTEVISION (55, "test.bin") vectors return in Phase 2 with rc_hash_scv */
    [InlineData((uint)79, "test.83g", 1695, "bfb6048395a425c69743900785987c42")] /* TI-83 */
    [InlineData((uint)79, "test.83p", 2500, "6e81d530ee9a79d4f4f505729ad74bb5")]
    [InlineData((uint)65, "test.tic", 67682, "79b96f4ffcedb3ce8210a83b22cd2c69")] /* TIC-80 */
    [InlineData((uint)80, "test.uze", 53654, "a9aab505e92edc034d3c732869159789")] /* Uzebox */
    [InlineData((uint)33, "test.vec", 4096, "572686c3a073162e4ec6eff86e6f6e3a")] /* Vectrex (console id as in C test) */
    [InlineData((uint)33, "test.vb", 524288, "68f0f13b598e0b66461bc578375c3888")] /* VirtualBoy (console id as in C test) */
    [InlineData((uint)63, "test.sv", 32768, "6a2305a2b6675a97ff792709be1ca857")] /* Watara Supervision */
    [InlineData((uint)72, "test.wasm", 33454, "bce38bb5f05622fc7e0e56757059d180")] /* WASM-4 */
    [InlineData((uint)53, "test.ws", 524288, "68f0f13b598e0b66461bc578375c3888")] /* WonderSwan */
    [InlineData((uint)53, "test.wsc", 4194304, "a247ec8a8c42e18fcb80702dfadac14b")]
    public void TestHashFullFileVectors(uint consoleId, string filename, int size, string expectedMd5)
    {
        TestHashFullFile(consoleId, filename, size, expectedMd5);
    }

    /// <summary>Tests hash mega drive m3u.</summary>
    [Fact]
    public void TestHashMegaDriveM3U()
    {
        TestHashM3U(ConsoleIds.RcConsoleMegaDrive, "test.md", 1048576, "da9461b3b0f74becc3ccf6c2a094c516");
    }
}
