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

using RASharp.Core;
using Xunit;

namespace RASharp.Tests;

public class TestHash3Ds : IDisposable
{
    private readonly string _dir;

    public TestHash3Ds()
    {
        _dir = Path.Combine(Path.GetTempPath(), "rasharp_3ds_test");
        Directory.CreateDirectory(_dir);

        File.WriteAllText(Path.Combine(_dir, "aes_keys.txt"), TestDataGen3ds.AesKeysTxt());

        byte[] plain = TestDataGen3ds.GenerateNcch(false, false, false, 0x01, 0, null, null, out _, out _);
        File.WriteAllBytes(Path.Combine(_dir, "plain.ncch"), plain);

        byte[] enc = TestDataGen3ds.GenerateNcch(true, false, false, 0x01, 0, null, null, out _, out _);
        File.WriteAllBytes(Path.Combine(_dir, "encrypted.ncch"), enc);

        File.WriteAllBytes(Path.Combine(_dir, "fixed.ncch"),
            TestDataGen3ds.GenerateNcch(true, true, false, 0x01, 0, null, null, out _, out _));

        File.WriteAllBytes(Path.Combine(_dir, "encrypted_v1.ncch"),
            TestDataGen3ds.GenerateNcch(true, false, false, 0x01, 1, null, null, out _, out _));

        /* unaligned .code size exercises the padding/partial-block branch */
        File.WriteAllBytes(Path.Combine(_dir, "unaligned.ncch"),
            TestDataGen3ds.GenerateNcch(true, false, false, 0x01, 0, null, null, out _, out _, 0x641));

        byte[] programId = { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };
        byte[] seed = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        File.WriteAllBytes(Path.Combine(_dir, "seeddb.bin"), TestDataGen3ds.GenerateSeedDbBin(programId, seed));
        File.WriteAllBytes(Path.Combine(_dir, "seed.ncch"),
            TestDataGen3ds.GenerateNcch(true, false, true, 0x0B, 0, programId, seed, out _, out _));

        byte[] titleKey = { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00 };
        byte[] titleId = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
        File.WriteAllBytes(Path.Combine(_dir, "encrypted.cia"),
            TestDataGen3ds.GenerateCia(enc, titleId, 0, titleKey, true));
        File.WriteAllBytes(Path.Combine(_dir, "plain.cia"),
            TestDataGen3ds.GenerateCia(plain, titleId, 0, titleKey, false));

        File.WriteAllBytes(Path.Combine(_dir, "homebrew.3dsx"), TestDataGen3ds.Generate3Dsx());
        File.WriteAllBytes(Path.Combine(_dir, "junk.bin"), new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });
    }

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
        Hash3DS.InitHash3DS(_dir);
        return RcHash.GenerateFromFile(out hash, ConsoleIds.RC_CONSOLE_NINTENDO_3DS, Path.Combine(_dir, file));
    }

    [Fact]
    public void TestHash3DsPlainNcch()
    {
        Assert.True(Generate(out string hash, "plain.ncch"));
        Assert.Equal("eb334fea757807e4a4b81ee99905437c", hash);
    }

    [Fact]
    public void TestHash3DsEncryptedNcch()
    {
        Assert.True(Generate(out string hash, "encrypted.ncch"));
        Assert.Equal("eb334fea757807e4a4b81ee99905437c", hash);
    }

    [Fact]
    public void TestHash3DsFixedKeyNcch()
    {
        Assert.True(Generate(out string hash, "fixed.ncch"));
        Assert.Equal("eb334fea757807e4a4b81ee99905437c", hash);
    }

    [Fact]
    public void TestHash3DsEncryptedNcchV1()
    {
        Assert.True(Generate(out string hash, "encrypted_v1.ncch"));
        Assert.Equal("552ef040edf82bffada8b7615b8b2faa", hash);
    }

    [Fact]
    public void TestHash3DsSeedNcch()
    {
        Assert.True(Generate(out string hash, "seed.ncch"));
        Assert.Equal("29b0b5a9e83ac39e635c792a5142f5e4", hash);
    }

    [Fact]
    public void TestHash3DsEncryptedCia()
    {
        Assert.True(Generate(out string hash, "encrypted.cia"));
        Assert.Equal("eb334fea757807e4a4b81ee99905437c", hash);
    }

    [Fact]
    public void TestHash3DsPlainCia()
    {
        Assert.True(Generate(out string hash, "plain.cia"));
        Assert.Equal("eb334fea757807e4a4b81ee99905437c", hash);
    }

    [Fact]
    public void TestHash3DsUnalignedNcch()
    {
        /* .code size 0x641: the section end is neither media- nor block-aligned,
         * exercising the padding + partial-block ("evil IV reuse") branch */
        Assert.True(Generate(out string hash, "unaligned.ncch"));
        Assert.Equal("3e2d3dfe1808dd0498ecf6c77e36ea46", hash);
    }

    [Fact]
    public void TestHash3DsCryptoVariantsAgree()
    {
        /* the C clears the crypto flags before hashing, so decrypted and
         * encrypted content must hash identically */
        Assert.True(Generate(out string plain, "plain.ncch"));
        Assert.True(Generate(out string enc, "encrypted.ncch"));
        Assert.True(Generate(out string cia, "encrypted.cia"));
        Assert.Equal(plain, enc);
        Assert.Equal(plain, cia);
    }

    [Fact]
    public void TestHash3Ds3Dsx()
    {
        Assert.True(Generate(out string hash, "homebrew.3dsx"));
        Assert.Equal("ca7161a502db8be8089d16a8b2280970", hash);
    }

    [Fact]
    public void TestHash3DsErrorNoKeys()
    {
        string emptyDir = Path.Combine(Path.GetTempPath(), "rasharp_3ds_nokeys");
        Directory.CreateDirectory(emptyDir);

        HashEngine.InitCustomFilereader(null);
        Hash3DS.InitHash3DS(emptyDir);
        Assert.False(RcHash.GenerateFromFile(out string hash, ConsoleIds.RC_CONSOLE_NINTENDO_3DS, Path.Combine(_dir, "encrypted.ncch")));
        Assert.Equal("", hash);
    }

    [Fact]
    public void TestHash3DsErrorJunkFile()
    {
        Assert.False(Generate(out string hash, "junk.bin"));
        Assert.Equal("", hash);
    }

    /* NIST SP 800-38A AES-128 known-answer checks for the BCL-backed
     * primitives (the C's aes.c is standard AES, so this is transitive) */
    [Fact]
    public void TestAesHelperKat()
    {
        byte[] key = Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");
        byte[] iv = Convert.FromHexString("000102030405060708090a0b0c0d0e0f");
        byte[] plaintext = Convert.FromHexString("6bc1bee22e409f96e93d7e117393172a");

        /* CBC decrypt */
        byte[] cbc = Convert.FromHexString("7649abac8119b246cee98e9b12e9197d"); /* F.2.1 CBC-AES128.Decrypt C1 */
        AesHelper.AesCbcDecrypt(cbc, 0, 16, key, iv);
        Assert.Equal(plaintext, cbc);

        /* CTR */
        byte[] ctrCipher = Convert.FromHexString("874d6191b620e3261bef6864990db6ce");
        byte[] counter = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");
        AesHelper.AesCtrXcrypt(ctrCipher, 0, 16, key, counter);
        Assert.Equal(plaintext, ctrCipher);

        /* CTR over two blocks: the counter must carry across the boundary */
        byte[] multi = Convert.FromHexString("874d6191b620e3261bef6864990db6ce" + "9806f66b7970fdff8617187bb9fffdff");
        byte[] counter2 = Convert.FromHexString("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff");
        byte[] multiPlain = Convert.FromHexString("6bc1bee22e409f96e93d7e117393172a" + "ae2d8a571e03ac9c9eb76fac45af8e51");
        AesHelper.AesCtrXcrypt(multi, 0, 32, key, counter2);
        Assert.Equal(multiPlain, multi);
    }
}
