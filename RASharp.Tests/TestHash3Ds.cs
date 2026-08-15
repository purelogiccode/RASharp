// Phase 6 — 3DS encrypted hashing tests.
// All fixture files are generated at test time from known key material; the
// expected hashes are the outputs of RAHasher 1.8.3 (HAVE_CHD + 3DS keys)
// on the exact same fixtures, captured during development:
// all crypto variants of the same content hash identically (the C clears the
// crypto flags), so the expected hashes are:
//   plain/encrypted/fixed/encrypted.cia/plain.cia -> eb334fea757807e4a4b81ee99905437c
//   encrypted_v1.ncch  552ef040edf82bffada8b7615b8b2faa
//   seed.ncch          29b0b5a9e83ac39e635c792a5142f5e4
//   unaligned.ncch     3e2d3dfe1808dd0498ecf6c77e36ea46  (padding/partial-block branch)
//   homebrew.3dsx      ca7161a502db8be8089d16a8b2280970

using RASharp;

namespace RASharp.Tests;

/// <summary>Phase 6 — 3DS encrypted hashing tests. All fixture files are generated at test time from known key material; the expected hashes are the outputs of RAHasher 1.8</summary>
public class TestHash3Ds : IDisposable
{
    private readonly string _dir;

    public TestHash3Ds()
    {
        _dir = Path.Combine(Path.GetTempPath(), "rasharp_3ds_test");
        Directory.CreateDirectory(_dir);

        File.WriteAllText(Path.Combine(_dir, "aes_keys.txt"), TestDataGen3Ds.AesKeysTxt());

        var plain = TestDataGen3Ds.GenerateNcch(false, false, false, 0x01, 0, null, null, out _, out _);
        File.WriteAllBytes(Path.Combine(_dir, "plain.ncch"), plain);

        var enc = TestDataGen3Ds.GenerateNcch(true, false, false, 0x01, 0, null, null, out _, out _);
        File.WriteAllBytes(Path.Combine(_dir, "encrypted.ncch"), enc);

        File.WriteAllBytes(Path.Combine(_dir, "fixed.ncch"),
            TestDataGen3Ds.GenerateNcch(true, true, false, 0x01, 0, null, null, out _, out _));

        File.WriteAllBytes(Path.Combine(_dir, "encrypted_v1.ncch"),
            TestDataGen3Ds.GenerateNcch(true, false, false, 0x01, 1, null, null, out _, out _));

        /* unaligned .code size exercises the padding/partial-block branch */
        File.WriteAllBytes(Path.Combine(_dir, "unaligned.ncch"),
            TestDataGen3Ds.GenerateNcch(true, false, false, 0x01, 0, null, null, out _, out _, 0x641));

        byte[] programId = [0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88];
        byte[] seed = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];
        File.WriteAllBytes(Path.Combine(_dir, "seeddb.bin"), TestDataGen3Ds.GenerateSeedDbBin(programId, seed));
        File.WriteAllBytes(Path.Combine(_dir, "seed.ncch"),
            TestDataGen3Ds.GenerateNcch(true, false, true, 0x0B, 0, programId, seed, out _, out _));

        byte[] titleKey = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00];
        byte[] titleId = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];
        File.WriteAllBytes(Path.Combine(_dir, "encrypted.cia"),
            TestDataGen3Ds.GenerateCia(enc, titleId, 0, titleKey, true));
        File.WriteAllBytes(Path.Combine(_dir, "plain.cia"),
            TestDataGen3Ds.GenerateCia(plain, titleId, 0, titleKey, false));

        File.WriteAllBytes(Path.Combine(_dir, "homebrew.3dsx"), TestDataGen3Ds.Generate3Dsx());
        File.WriteAllBytes(Path.Combine(_dir, "junk.bin"), [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]);
    }

    /// <summary>Releases the mounted filesystem.</summary>
    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, true);
            Directory.Delete(Path.Combine(Path.GetTempPath(), "rasharp_3ds_nokeys"), true);
        }
        catch
        {
            /* best effort */
        }
    }

    private bool Generate(out string hash, string file)
    {
        /* deterministic: real filereader + the synthetic key files */
        HashEngine.InitCustomFilereader(null);
        Hash3Ds.InitHash3Ds(_dir);
        return RcHash.GenerateFromFile(out hash, ConsoleIds.RcConsoleNintendo3Ds, Path.Combine(_dir, file));
    }

    /// <summary>Tests hash3 ds plain ncch.</summary>
    [Fact]
    public void TestHash3DsPlainNcch()
    {
        Assert.True(Generate(out var hash, "plain.ncch"));
        Assert.Equal("eb334fea757807e4a4b81ee99905437c", hash);
    }

    /// <summary>Tests hash3 ds encrypted ncch.</summary>
    [Fact]
    public void TestHash3DsEncryptedNcch()
    {
        Assert.True(Generate(out var hash, "encrypted.ncch"));
        Assert.Equal("eb334fea757807e4a4b81ee99905437c", hash);
    }

    /// <summary>Tests hash3 ds fixed key ncch.</summary>
    [Fact]
    public void TestHash3DsFixedKeyNcch()
    {
        Assert.True(Generate(out var hash, "fixed.ncch"));
        Assert.Equal("eb334fea757807e4a4b81ee99905437c", hash);
    }

    /// <summary>Tests hash3 ds encrypted ncch v1.</summary>
    [Fact]
    public void TestHash3DsEncryptedNcchV1()
    {
        Assert.True(Generate(out var hash, "encrypted_v1.ncch"));
        Assert.Equal("552ef040edf82bffada8b7615b8b2faa", hash);
    }

    /// <summary>Tests hash3 ds seed ncch.</summary>
    [Fact]
    public void TestHash3DsSeedNcch()
    {
        Assert.True(Generate(out var hash, "seed.ncch"));
        Assert.Equal("29b0b5a9e83ac39e635c792a5142f5e4", hash);
    }

    /// <summary>Tests hash3 ds encrypted cia.</summary>
    [Fact]
    public void TestHash3DsEncryptedCia()
    {
        Assert.True(Generate(out var hash, "encrypted.cia"));
        Assert.Equal("eb334fea757807e4a4b81ee99905437c", hash);
    }

    /// <summary>Tests hash3 ds plain cia.</summary>
    [Fact]
    public void TestHash3DsPlainCia()
    {
        Assert.True(Generate(out var hash, "plain.cia"));
        Assert.Equal("eb334fea757807e4a4b81ee99905437c", hash);
    }

    /// <summary>Tests hash3 ds unaligned ncch.</summary>
    [Fact]
    public void TestHash3DsUnalignedNcch()
    {
        /* .code size 0x641: the section end is neither media- nor block-aligned,
         * exercising the padding + partial-block ("evil IV reuse") branch */
        Assert.True(Generate(out var hash, "unaligned.ncch"));
        Assert.Equal("3e2d3dfe1808dd0498ecf6c77e36ea46", hash);
    }

    /// <summary>Tests hash3 ds crypto variants agree.</summary>
    [Fact]
    public void TestHash3DsCryptoVariantsAgree()
    {
        /* the C clears the crypto flags before hashing, so decrypted and
         * encrypted content must hash identically */
        Assert.True(Generate(out var plain, "plain.ncch"));
        Assert.True(Generate(out var enc, "encrypted.ncch"));
        Assert.True(Generate(out var cia, "encrypted.cia"));
        Assert.Equal(plain, enc);
        Assert.Equal(plain, cia);
    }

    /// <summary>Tests hash3 ds3 dsx.</summary>
    [Fact]
    public void TestHash3Ds3Dsx()
    {
        Assert.True(Generate(out var hash, "homebrew.3dsx"));
        Assert.Equal("ca7161a502db8be8089d16a8b2280970", hash);
    }

    /// <summary>Tests hash3 ds error no keys.</summary>
    [Fact]
    public void TestHash3DsErrorNoKeys()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), "rasharp_3ds_nokeys");
        Directory.CreateDirectory(emptyDir);

        HashEngine.InitCustomFilereader(null);
        Hash3Ds.InitHash3Ds(emptyDir);
        Assert.False(RcHash.GenerateFromFile(out var hash, ConsoleIds.RcConsoleNintendo3Ds, Path.Combine(_dir, "encrypted.ncch")));
        Assert.Equal("", hash);
    }

    /// <summary>Tests hash3 ds error junk file.</summary>
    [Fact]
    public void TestHash3DsErrorJunkFile()
    {
        Assert.False(Generate(out var hash, "junk.bin"));
        Assert.Equal("", hash);
    }

    /* NIST SP 800-38A AES-128 known-answer checks for the BCL-backed
     * primitives (the C's aes.c is standard AES, so this is transitive) */
    /// <summary>NIST SP 800-38A AES-128 known-answer checks for the BCL-backed primitives (the C's aes.c is standard AES, so this is transitive)</summary>
    [Fact]
    public void TestAesHelperKat()
    {
        var key = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");
        var iv = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
        var plaintext = Convert.FromHexString("6bc1bee22e409f96e93d7e117393172a");

        /* CBC decrypt */
        var cbc = Convert.FromHexString("7649abac8119b246cee98e9b12e9197d"); /* F.2.1 CBC-AES128.Decrypt C1 */
        AesHelper.AesCbcDecrypt(cbc, 0, 16, key, iv);
        Assert.Equal(plaintext, cbc);

        /* CTR */
        var ctrCipher = Convert.FromHexString("874d6191b620e3261bef6864990db6ce");
        var counter = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");
        AesHelper.AesCtrXcrypt(ctrCipher, 0, 16, key, counter);
        Assert.Equal(plaintext, ctrCipher);

        /* CTR over two blocks: the counter must carry across the boundary */
        var multi = Convert.FromHexString("874d6191b620e3261bef6864990db6ce" + "9806f66b7970fdff8617187bb9fffdff");
        var counter2 = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");
        var multiPlain = Convert.FromHexString("6bc1bee22e409f96e93d7e117393172a" + "ae2d8a571e03ac9c9eb76fac45af8e51");
        AesHelper.AesCtrXcrypt(multi, 0, 32, key, counter2);
        Assert.Equal(multiPlain, multi);
    }
}
