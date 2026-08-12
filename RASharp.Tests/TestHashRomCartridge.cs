// Ported from rcheevos (MIT) — test/rhash/test_hash_rom.c (Phase 2 subset)
// Cartridge algorithms: Arcade, Atari 7800, NES/FDS, N64, NDS/DSi, SCV,
// Arduboy (Intel HEX), SNES/PCE header-strip vectors.

using RASharp.Core;
using RASharp.Core.Models;

namespace RASharp.Tests;

/// <summary>Ported from rcheevos (MIT) — test/rhash/test_hash_rom.c (Phase 2 subset) Cartridge algorithms: Arcade, Atari 7800, NES/FDS, N64, NDS/DSi, SCV, Arduboy (Intel HE</summary>
public class TestHashRomCartridge
{
    public TestHashRomCartridge()
    {
        MockFilereader.InitMockFilereader();
    }

    private static void TestHashFullFile(uint consoleId, string filename, int size, string expectedMd5)
    {
        var image = TestDataGen.GenerateGenericFile(size);

        MockFilereader.MockFile(0, filename, image, size);

        Assert.True(RcHash.GenerateFromBuffer(out var hashBuffer, consoleId, image, size));
        Assert.Equal(expectedMd5, hashBuffer);

        Assert.True(RcHash.GenerateFromFile(out var hashFile, consoleId, filename));
        Assert.Equal(expectedMd5, hashFile);

        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, filename, null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    private static void TestHashArcade(string path, string expectedMd5)
    {
        /* test file hash */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleArcade, path));
        Assert.Equal(expectedMd5, hashFile);

        /* test file identification from iterator */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, path, null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    /* ========================================================================= */
    /* Arcade                                                                     */

    [Theory]
    [InlineData("game.zip", "c8d46d341bea4fd5bff866a65ff8aea9")]
    [InlineData("game.7z", "c8d46d341bea4fd5bff866a65ff8aea9")]
    [InlineData("/game.zip", "c8d46d341bea4fd5bff866a65ff8aea9")]
    [InlineData("\\game.zip", "c8d46d341bea4fd5bff866a65ff8aea9")]
    [InlineData("roms\\game.zip", "c8d46d341bea4fd5bff866a65ff8aea9")]
    [InlineData(@"C:\roms\game.zip", "c8d46d341bea4fd5bff866a65ff8aea9")]
    [InlineData("/home/user/roms/game.zip", "c8d46d341bea4fd5bff866a65ff8aea9")]
    [InlineData("/home/user/games/game.zip", "c8d46d341bea4fd5bff866a65ff8aea9")]
    [InlineData("/home/user/roms/game.7z", "c8d46d341bea4fd5bff866a65ff8aea9")]
    [InlineData("/home/user/nes_game.zip", "9b7aad36b365712fc93728088de4c209")]
    [InlineData("/home/user/nes/game.zip", "9b7aad36b365712fc93728088de4c209")]
    [InlineData(@"C:\roms\nes\game.zip", "9b7aad36b365712fc93728088de4c209")]
    [InlineData(@"C:\roms\NES\game.zip", "9b7aad36b365712fc93728088de4c209")]
    [InlineData("nes\\game.zip", "9b7aad36b365712fc93728088de4c209")]
    [InlineData("/home/user/snes/game.zip", "c8d46d341bea4fd5bff866a65ff8aea9")]
    [InlineData("/home/user/nes2/game.zip", "c8d46d341bea4fd5bff866a65ff8aea9")]
    /* aliases generate different hashes than a plain arcade ROM with the same filename */
    [InlineData("/home/user/chf/game.zip", "6ef57f16562ea0c7f49d93853b313e32")]
    [InlineData("/home/user/channelf/game.zip", "7b6506637a0cc79bd1d24a43a34fa3b9")]
    [InlineData("/home/user/coleco/game.zip", "c546f63ae7de98add4b9f221a4749260")]
    [InlineData("/home/user/colecovision/game.zip", "47279207b94dbf2a45cb13efa56d685e")]
    [InlineData("/home/user/msx/game.zip", "59ab85f6b56324fd81b4e324b804c29f")]
    [InlineData("/home/user/msx1/game.zip", "33328d832dcb0854383cdd4a4565c459")]
    [InlineData("/home/user/pce/game.zip", "c414a783f3983bbe2e9e01d9d5320c7e")]
    [InlineData("/home/user/pcengine/game.zip", "49370c3cbe98bdcdce545c68379487db")]
    [InlineData("/home/user/sgx/game.zip", "db545ab29694bfda1010317d4bac83b8")]
    [InlineData("/home/user/supergrafx/game.zip", "5665c9ef4c2f6609d8e420c4d86ba692")]
    [InlineData("/home/user/tg16/game.zip", "8b6c5c2e54915be2cdba63973862e143")]
    [InlineData("/home/user/fds/game.zip", "c0c135a97e8c577cfdf9204823ff211f")]
    [InlineData("/home/user/gamegear/game.zip", "f6f471e952b8103032b723f57bdbe767")]
    [InlineData("/home/user/mastersystem/game.zip", "f4805afe0ff5647140a26bd0a1057373")]
    [InlineData("/home/user/sms/game.zip", "43f35f575dead94dd2f42f9caf69fe5a")]
    [InlineData("/home/user/megadriv/game.zip", "f99d0aaf12ba3eb6ced9878c76692c63")]
    [InlineData("/home/user/megadrive/game.zip", "73eb5d7034b382093b1d36414d9e84e4")]
    [InlineData("/home/user/genesis/game.zip", "b62f810c63e1cba7f5b7569643bec236")]
    [InlineData("/home/user/sg1000/game.zip", "e8f6c711c4371f09537b4f2a7a304d6c")]
    [InlineData("/home/user/spectrum/game.zip", "a5f62157b2617bd728c4b1bc885c29e9")]
    [InlineData("/home/user/ngp/game.zip", "d4133b74c4e57274ca514e27a370dcb6")]
    public void TestHashArcadeVectors(string path, string expectedMd5)
    {
        TestHashArcade(path, expectedMd5);
    }

    /* ========================================================================= */
    /* Atari 7800                                                                */

    private static byte[] GenerateAtari7800File(int kb, bool withHeader, out int imageSize)
    {
        return TestDataGen.GenerateAtari7800File(kb, withHeader, out imageSize);
    }

    /// <summary>Tests hash atari7800.</summary>
    [Fact]
    public void TestHashAtari7800()
    {
        var image = GenerateAtari7800File(16, false, out var imageSize);
        Assert.True(RcHash.GenerateFromBuffer(out var hash, ConsoleIds.RcConsoleAtari7800, image, imageSize));
        Assert.Equal("455f07d8500f3fabc54906737866167f", hash);
        Assert.Equal(16384, imageSize);
    }

    /// <summary>Tests hash atari7800 with header.</summary>
    [Fact]
    public void TestHashAtari7800WithHeader()
    {
        var image = GenerateAtari7800File(16, true, out var imageSize);
        Assert.True(RcHash.GenerateFromBuffer(out var hash, ConsoleIds.RcConsoleAtari7800, image, imageSize));
        /* NOTE: expectation is that this hash matches the hash in TestHashAtari7800 */
        Assert.Equal("455f07d8500f3fabc54906737866167f", hash);
        Assert.Equal(16384 + 128, imageSize);
    }

    /* ========================================================================= */
    /* NES / FDS                                                                 */

    /// <summary>========================================================================= NES / FDS</summary>
    [Fact]
    public void TestHashNes32K()
    {
        var image = TestDataGen.GenerateNesFile(32, false, out var imageSize);
        Assert.True(RcHash.GenerateFromBuffer(out var hash, ConsoleIds.RcConsoleNintendo, image, imageSize));
        Assert.Equal("6a2305a2b6675a97ff792709be1ca857", hash);
        Assert.Equal(32768, imageSize);
    }

    /// <summary>Tests hashing of a NES/Famicom image.</summary>
    [Fact]
    public void TestHashNes32KWithHeader()
    {
        var image = TestDataGen.GenerateNesFile(32, true, out var imageSize);
        Assert.True(RcHash.GenerateFromBuffer(out var hash, ConsoleIds.RcConsoleNintendo, image, imageSize));
        /* NOTE: expectation is that this hash matches the hash in TestHashNes32k */
        Assert.Equal("6a2305a2b6675a97ff792709be1ca857", hash);
        Assert.Equal(32768 + 16, imageSize);
    }

    /// <summary>Tests hashing of a NES/Famicom image.</summary>
    [Fact]
    public void TestHashNes256K()
    {
        var image = TestDataGen.GenerateNesFile(256, false, out var imageSize);
        Assert.True(RcHash.GenerateFromBuffer(out var hash, ConsoleIds.RcConsoleNintendo, image, imageSize));
        Assert.Equal("545d527301b8ae148153988d6c4fcb84", hash);
        Assert.Equal(262144, imageSize);
    }

    /// <summary>Tests hash fds two sides.</summary>
    [Fact]
    public void TestHashFdsTwoSides()
    {
        var image = TestDataGen.GenerateFdsFile(2, false, out var imageSize);
        Assert.True(RcHash.GenerateFromBuffer(out var hash, ConsoleIds.RcConsoleNintendo, image, imageSize));
        Assert.Equal("fd770d4d34c00760fabda6ad294a8f0b", hash);
        Assert.Equal(65500 * 2, imageSize);
    }

    /// <summary>Tests hash fds two sides with header.</summary>
    [Fact]
    public void TestHashFdsTwoSidesWithHeader()
    {
        var image = TestDataGen.GenerateFdsFile(2, true, out var imageSize);
        Assert.True(RcHash.GenerateFromBuffer(out var hash, ConsoleIds.RcConsoleNintendo, image, imageSize));
        /* NOTE: expectation is that this hash matches the hash in TestHashFdsTwoSides */
        Assert.Equal("fd770d4d34c00760fabda6ad294a8f0b", hash);
        Assert.Equal(65500 * 2 + 16, imageSize);
    }

    /// <summary>Tests hashing of a NES/Famicom image.</summary>
    [Fact]
    public void TestHashNesFile32K()
    {
        var image = TestDataGen.GenerateNesFile(32, false, out var imageSize);
        MockFilereader.MockFile(0, "test.nes", image, imageSize);
        Assert.True(RcHash.GenerateFromFile(out var hash, ConsoleIds.RcConsoleNintendo, "test.nes"));
        Assert.Equal("6a2305a2b6675a97ff792709be1ca857", hash);
        Assert.Equal(32768, imageSize);
    }

    /// <summary>Tests hashing of a NES/Famicom image.</summary>
    [Fact]
    public void TestHashNesIterator32K()
    {
        var image = TestDataGen.GenerateNesFile(32, false, out var imageSize);
        MockFilereader.MockFile(0, "test.nes", image, imageSize);

        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "test.nes", null, 0);
        Assert.True(HashIterator.Iterate(out var hash1, iterator) != 0);
        Assert.Equal("6a2305a2b6675a97ff792709be1ca857", hash1);

        Assert.Equal(0, HashIterator.Iterate(out var hash2, iterator));
        Assert.Equal("", hash2);
        HashIterator.DestroyIterator(iterator);
    }

    /// <summary>Tests hashing of a NES/Famicom image.</summary>
    [Fact]
    public void TestHashNesFileIterator32K()
    {
        var image = TestDataGen.GenerateNesFile(32, false, out var imageSize);

        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "test.nes", image, imageSize);
        Assert.True(HashIterator.Iterate(out var hash1, iterator) != 0);
        Assert.Equal("6a2305a2b6675a97ff792709be1ca857", hash1);

        Assert.Equal(0, HashIterator.Iterate(out var hash2, iterator));
        Assert.Equal("", hash2);
        HashIterator.DestroyIterator(iterator);
    }

    /// <summary>Tests hash file without ext.</summary>
    [Fact]
    public void TestHashFileWithoutExt()
    {
        var image = TestDataGen.GenerateNesFile(32, true, out var imageSize);
        MockFilereader.MockFile(0, "test", image, imageSize);

        /* specifying a console will use the appropriate hasher */
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleNintendo, "test"));
        Assert.Equal("6a2305a2b6675a97ff792709be1ca857", hashFile);

        /* no extension will use the default full file iterator, so hash should include header */
        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "test", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal("64b131c5c7fec32985d9c99700babb7e", hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    /* ========================================================================= */
    /* Nintendo 64                                                                */

    /* first 64 bytes of SUPER MARIO 64 ROM in each N64 format */
    private static readonly byte[] TestRomZ64 =
    [
        0x80, 0x37, 0x12, 0x40, 0x00, 0x00, 0x00, 0x0F, 0x80, 0x24, 0x60, 0x00, 0x00, 0x00, 0x14, 0x44,
        0x63, 0x5A, 0x2B, 0xFF, 0x8B, 0x02, 0x23, 0x26, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x53, 0x55, 0x50, 0x45, 0x52, 0x20, 0x4D, 0x41, 0x52, 0x49, 0x4F, 0x20, 0x36, 0x34, 0x20, 0x20,
        0x20, 0x20, 0x20, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4E, 0x53, 0x4D, 0x45, 0x00
    ];

    private static readonly byte[] TestRomV64 =
    [
        0x37, 0x80, 0x40, 0x12, 0x00, 0x00, 0x0F, 0x00, 0x24, 0x80, 0x00, 0x60, 0x00, 0x00, 0x44, 0x14,
        0x5A, 0x63, 0xFF, 0x2B, 0x02, 0x8B, 0x26, 0x23, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x55, 0x53, 0x45, 0x50, 0x20, 0x52, 0x41, 0x4D, 0x49, 0x52, 0x20, 0x4F, 0x34, 0x36, 0x20, 0x20,
        0x20, 0x20, 0x20, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4E, 0x00, 0x4D, 0x53, 0x00, 0x45
    ];

    private static readonly byte[] TestRomN64 =
    [
        0x40, 0x12, 0x37, 0x80, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x60, 0x24, 0x80, 0x44, 0x14, 0x00, 0x00,
        0xFF, 0x2B, 0x5A, 0x63, 0x26, 0x23, 0x02, 0x8B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x45, 0x50, 0x55, 0x53, 0x41, 0x4D, 0x20, 0x52, 0x20, 0x4F, 0x49, 0x52, 0x20, 0x20, 0x34, 0x36,
        0x20, 0x20, 0x20, 0x20, 0x00, 0x00, 0x00, 0x00, 0x4E, 0x00, 0x00, 0x00, 0x00, 0x45, 0x4D, 0x53
    ];

    /* first 64 bytes of DOSHIN THE GIANT in ndd format */
    private static readonly byte[] TestRomNdd =
    [
        0xE8, 0x48, 0xD3, 0x16, 0x10, 0x13, 0x00, 0x45, 0x0C, 0x18, 0x24, 0x30, 0x3C, 0x48, 0x54, 0x60,
        0x6C, 0x78, 0x84, 0x90, 0x9C, 0xA8, 0xB4, 0xC0, 0xFF, 0xFF, 0xFF, 0xFF, 0x80, 0x02, 0x5C, 0x00,
        0x10, 0x16, 0x1C, 0x22, 0x28, 0x2A, 0x31, 0x32, 0x3A, 0x40, 0x46, 0x4C, 0x04, 0x0C, 0x14, 0x1C,
        0x24, 0x2C, 0x34, 0x3C, 0x44, 0x4C, 0x54, 0x5C, 0x04, 0x0C, 0x14, 0x1C, 0x24, 0x2C, 0x34, 0x3C
    ];

    private static void TestHashN64(byte[] buffer, string expectedHash)
    {
        HashEngine.ResetFilereader(); /* explicitly unset the filereader */
        Assert.True(RcHash.GenerateFromBuffer(out var hash, ConsoleIds.RcConsoleNintendo64, buffer, buffer.Length));
        MockFilereader.InitMockFilereader(); /* restore the mock filereader */

        Assert.Equal(expectedHash, hash);
    }

    private static void TestHashN64File(string filename, byte[] buffer, string expectedHash)
    {
        MockFilereader.MockFile(0, filename, buffer, buffer.Length);

        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleNintendo64, filename));
        Assert.Equal(expectedHash, hashFile);

        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, filename, null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedHash, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    /// <summary>Tests hashing of a Nintendo 64 image.</summary>
    [Fact]
    public void TestHashN64Z64()
    {
        TestHashN64(TestRomZ64, "06096d7ce21cb6bcde38391534c4eb91");
    }

    /// <summary>Tests hashing of a Nintendo 64 image.</summary>
    [Fact]
    public void TestHashN64V64()
    {
        TestHashN64(TestRomV64, "06096d7ce21cb6bcde38391534c4eb91");
    }

    /// <summary>Tests hashing of a Nintendo 64 image.</summary>
    [Fact]
    public void TestHashN64N64()
    {
        TestHashN64(TestRomN64, "06096d7ce21cb6bcde38391534c4eb91");
    }

    /// <summary>Tests hashing of a Nintendo 64 image.</summary>
    [Fact]
    public void TestHashN64Ndd()
    {
        TestHashN64(TestRomNdd, "a698b32a52970d8a52a5a52c83acc2a9");
    }

    /// <summary>Tests hashing of a Nintendo 64 image.</summary>
    [Fact]
    public void TestHashN64FileZ64()
    {
        TestHashN64File("game.z64", TestRomZ64, "06096d7ce21cb6bcde38391534c4eb91");
    }

    /// <summary>Tests hashing of a Nintendo 64 image.</summary>
    [Fact]
    public void TestHashN64FileV64()
    {
        TestHashN64File("game.v64", TestRomV64, "06096d7ce21cb6bcde38391534c4eb91");
    }

    /// <summary>Tests hashing of a Nintendo 64 image.</summary>
    [Fact]
    public void TestHashN64FileN64()
    {
        TestHashN64File("game.n64", TestRomN64, "06096d7ce21cb6bcde38391534c4eb91");
    }

    /// <summary>Tests hashing of a Nintendo 64 image.</summary>
    [Fact]
    public void TestHashN64FileMisnamedN64()
    {
        TestHashN64File("game.n64", TestRomZ64, "06096d7ce21cb6bcde38391534c4eb91"); /* misnamed */
    }

    /// <summary>Tests hashing of a Nintendo 64 image.</summary>
    [Fact]
    public void TestHashN64FileMisnamedZ64()
    {
        TestHashN64File("game.z64", TestRomN64, "06096d7ce21cb6bcde38391534c4eb91"); /* misnamed */
    }

    /* ========================================================================= */
    /* Nintendo DS / DSi                                                          */

    private static void TestHashNdsCore(bool supercard, bool buffered, bool dsi)
    {
        var image = TestDataGen.GenerateNdsFile(2, 1234567, 654321, out var imageSize);
        const string expectedHash = "56b30c276cba4affa886bd38e8e34d7e";
        var consoleId = dsi ? ConsoleIds.RcConsoleNintendoDsi : ConsoleIds.RcConsoleNintendoDs;

        if (supercard)
        {
            /* inject the SuperCard header (512 bytes) */
            var image2Size = imageSize + 512;
            var image2 = new byte[image2Size];
            Array.Copy(image, 0, image2, 512, imageSize);
            image2[0] = 0x2E;
            image2[1] = 0x00;
            image2[2] = 0x00;
            image2[3] = 0xEA;
            image2[0xB0] = 0x44;
            image2[0xB1] = 0x46;
            image2[0xB2] = 0x96;
            image2[0xB3] = 0x00;

            MockFilereader.MockFile(0, "game.nds", image2, image2Size);
            Assert.True(RcHash.GenerateFromFile(out var hashFile, consoleId, "game.nds"));
            Assert.Equal(expectedHash, hashFile);

            var iterator = new RcHashIterator();
            HashIterator.InitializeIterator(iterator, "game.nds", null, 0);
            Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
            Assert.Equal(expectedHash, hashIterator);
            HashIterator.DestroyIterator(iterator);
            return;
        }

        if (buffered)
        {
            Assert.True(RcHash.GenerateFromBuffer(out var hashBuffer, consoleId, image, imageSize));
            Assert.Equal(expectedHash, hashBuffer);

            var iterator = new RcHashIterator();
            HashIterator.InitializeIterator(iterator, "game.nds", image, imageSize);
            Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
            Assert.Equal(expectedHash, hashIterator);
            HashIterator.DestroyIterator(iterator);
            return;
        }

        MockFilereader.MockFile(0, "game.nds", image, imageSize);
        Assert.True(RcHash.GenerateFromFile(out var hashFile2, consoleId, "game.nds"));
        Assert.Equal(expectedHash, hashFile2);

        var fileIterator = new RcHashIterator();
        HashIterator.InitializeIterator(fileIterator, "game.nds", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator2, fileIterator) != 0);
        Assert.Equal(expectedHash, hashIterator2);
        HashIterator.DestroyIterator(fileIterator);
    }

    /// <summary>Tests hash nds.</summary>
    [Fact]
    public void TestHashNds()
    {
        TestHashNdsCore(supercard: false, buffered: false, dsi: false);
    }

    /// <summary>Tests hash nds supercard.</summary>
    [Fact]
    public void TestHashNdsSupercard()
    {
        TestHashNdsCore(supercard: true, buffered: false, dsi: false);
    }

    /// <summary>Tests hash nds buffered.</summary>
    [Fact]
    public void TestHashNdsBuffered()
    {
        TestHashNdsCore(supercard: false, buffered: true, dsi: false);
    }

    /// <summary>Tests hashing of a Nintendo DSi image.</summary>
    [Fact]
    public void TestHashDsi()
    {
        TestHashNdsCore(supercard: false, buffered: false, dsi: true);
    }

    /// <summary>Tests hashing of a Nintendo DSi image.</summary>
    [Fact]
    public void TestHashDsiBuffered()
    {
        TestHashNdsCore(supercard: false, buffered: true, dsi: true);
    }

    /* ========================================================================= */
    /* Super Cassette Vision                                                      */

    /// <summary>========================================================================= Super Cassette Vision</summary>
    [Fact]
    public void TestHashScvCart()
    {
        const int imageSize = 32768 + 32;
        var image = TestDataGen.GenerateGenericFile(imageSize);
        const string expectedMd5 = "4309c9844b44f9ff8256dfc04687b8fd";

        var header = "EmuSCV....CART.............................."u8.ToArray();
        Array.Copy(header, image, 32);

        MockFilereader.MockFile(0, "game.cart", image, imageSize);
        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleSuperCassettevision, "game.cart"));
        Assert.Equal(expectedMd5, hashFile);

        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.cart", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    /* ========================================================================= */
    /* Arduboy (Intel HEX text hash)                                              */

    private static void TestHashArduboyHex(string hexInput, string expectedMd5)
    {
        MockFilereader.MockFileText(0, "game.hex", hexInput);

        Assert.True(RcHash.GenerateFromFile(out var hashFile, ConsoleIds.RcConsoleArduboy, "game.hex"));
        Assert.Equal(expectedMd5, hashFile);

        var iterator = new RcHashIterator();
        HashIterator.InitializeIterator(iterator, "game.hex", null, 0);
        Assert.True(HashIterator.Iterate(out var hashIterator, iterator) != 0);
        Assert.Equal(expectedMd5, hashIterator);
        HashIterator.DestroyIterator(iterator);
    }

    /// <summary>Tests hashing of a Arduboy image.</summary>
    [Fact]
    public void TestHashArduboy()
    {
        TestHashArduboyHex(
            ":100000000C94690D0C94910D0C94910D0C94910D20\n" +
            ":100010000C94910D0C94910D0C94910D0C94910DE8\n" +
            ":100020000C94910D0C94910D0C94C32A0C94352BC7\n" +
            ":00000001FF\n",
            "67b64633285a7f965064ba29dab45148");
    }

    /// <summary>Tests hashing of a Arduboy image.</summary>
    [Fact]
    public void TestHashArduboyCrlf()
    {
        TestHashArduboyHex(
            ":100000000C94690D0C94910D0C94910D0C94910D20\r\n" +
            ":100010000C94910D0C94910D0C94910D0C94910DE8\r\n" +
            ":100020000C94910D0C94910D0C94C32A0C94352BC7\r\n" +
            ":00000001FF\r\n",
            "67b64633285a7f965064ba29dab45148");
    }

    /// <summary>Tests hashing of a Arduboy image.</summary>
    [Fact]
    public void TestHashArduboyNoFinalLf()
    {
        TestHashArduboyHex(
            ":100000000C94690D0C94910D0C94910D0C94910D20\n" +
            ":100010000C94910D0C94910D0C94910D0C94910DE8\n" +
            ":100020000C94910D0C94910D0C94C32A0C94352BC7\n" +
            ":00000001FF",
            "67b64633285a7f965064ba29dab45148");
    }

    /* ========================================================================= */
    /* SNES / PCE / SCV full-file vectors (header-strip consoles)                 */

    /// <summary>========================================================================= SNES / PCE / SCV full-file vectors (header-strip consoles)</summary>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="filename">the filename parameter</param>
    /// <param name="size">the size</param>
    /// <param name="expectedMd5">the expected md5 parameter</param>
    [Theory]
    [InlineData((uint)3, "test.smc", 524288, "68f0f13b598e0b66461bc578375c3888")] /* SNES */
    [InlineData((uint)3, "test.smc", 524288 + 512, "258c93ebaca1c3f488ab48218e5e8d38")]
    [InlineData((uint)8, "test.pce", 524288, "68f0f13b598e0b66461bc578375c3888")] /* PC Engine */
    [InlineData((uint)8, "test.pce", 524288 + 512, "258c93ebaca1c3f488ab48218e5e8d38")]
    [InlineData((uint)8, "test.pce", 491520 + 512, "ebb565a7f964ccdfaecdce0d6ed540af")]
    [InlineData((uint)55, "test.bin", 32768, "6a2305a2b6675a97ff792709be1ca857")] /* Super Cassette Vision */
    public void TestHashFullFileVectors(uint consoleId, string filename, int size, string expectedMd5)
    {
        TestHashFullFile(consoleId, filename, size, expectedMd5);
    }
}
