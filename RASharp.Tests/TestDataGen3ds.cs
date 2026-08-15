// TestDataGen3ds — synthetic 3DS fixtures for Phase 6.
// The encryption side mirrors the C's decryption choreography exactly
// (same keys, same continuous CTR counter, same CBC chain), so that
// RAHasher 1.8.3 (the oracle) can decrypt and hash them; the expected
// hashes embedded in TestHash3Ds.cs come from that oracle.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace RASharp.Tests;

/// <summary>TestDataGen3ds — synthetic 3DS fixtures for Phase 6. The encryption side mirrors the C's decryption choreography exactly (same keys, same continuous CTR counter</summary>
internal static class TestDataGen3Ds
{
    /* chosen key material for the synthetic aes_keys.txt (all first bytes non-zero,
     * as the C's "have" flags use key[0]) */
    public const string Slot2CKeyX = "0102030405060708090A0B0C0D0E0F10";
    public const string Slot25KeyX = "1112131415161718191A1B1C1D1E1F20";
    public const string Slot18KeyX = "2122232425262728292A2B2C2D2E2F30";
    public const string Slot1BKeyX = "3132333435363738393A3B3C3D3E3F40";
    public const string Slot3DKeyX = "4142434445464748494A4B4C4D4E4F50";
    public const string Common0 = "5152535455565758595A5B5C5D5E5F60";
    public const string Common1 = "6162636465666768696A6B6C6D6E6F70";
    public const string Common2 = "7172737475767778797A7B7C7D7E7F80";
    public const string Common3 = "8182838485868788898A8B8C8D8E8F90";
    public const string Common4 = "9192939495969798999A9B9C9D9E9FA0";
    public const string Common5 = "A1A2A3A4A5A6A7A8A9AAABACADAEAFB0";

    /// <summary>aes keys txt.</summary>
    /// <returns>the generated value</returns>
    public static string AesKeysTxt()
    {
        return string.Join("\n",
            "slot0x2CKeyX=" + Slot2CKeyX,
            "slot0x25KeyX=" + Slot25KeyX,
            "slot0x18KeyX=" + Slot18KeyX,
            "slot0x1BKeyX=" + Slot1BKeyX,
            "slot0x3DKeyX=" + Slot3DKeyX,
            "common0=" + Common0,
            "common1=" + Common1,
            "common2=" + Common2,
            "common3=" + Common3,
            "common4=" + Common4,
            "common5=" + Common5) + "\n";
    }

    /// <summary>generate seed db bin.</summary>
    /// <param name="programId">the program id parameter</param>
    /// <param name="seed">the seed parameter</param>
    /// <returns>the result</returns>
    public static byte[] GenerateSeedDbBin(byte[] programId, byte[] seed)
    {
        var result = new byte[4 + 12 + 8 + 16 + 8];
        result[0] = 1;
        Array.Copy(programId, 0, result, 4 + 12, 8);
        Array.Copy(seed, 0, result, 4 + 12 + 8, 16);
        return result;
    }

    private static byte[] HexToBytes(string hex)
    {
        var result = new byte[hex.Length / 2];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return result;
    }

    /* the key-normalization used by the hasher (mirror of Hash3DS.NormalizeKeys) */
    /// <summary>the key-normalization used by the hasher (mirror of Hash3DS.NormalizeKeys)</summary>
    /// <param name="keyX">the key x parameter</param>
    /// <param name="keyY">the key y parameter</param>
    /// <returns>the result</returns>
    public static byte[] NormalizeKeys(byte[] keyX, byte[] keyY)
    {
        var keyN = (byte[])keyX.Clone();
        Rol(keyN, 2);
        for (var i = 0; i < 16; i++)
        {
            keyN[i] ^= keyY[i];
        }

        byte[] gen = [0x1F, 0xF9, 0xE9, 0xAA, 0xC5, 0xFE, 0x04, 0x08, 0x02, 0x45, 0x91, 0xDC, 0x5D, 0x52, 0x76, 0x8A];
        ushort carry = 0;
        for (var i = 15; i >= 0; i--)
        {
            carry += (ushort)(keyN[i] + gen[i]);
            keyN[i] = (byte)(carry & 0xFF);
            carry >>= 8;
        }

        Rol(keyN, 87);
        return keyN;
    }

    private static void Rol(byte[] key, int amount)
    {
        var copy = (byte[])key.Clone();
        var offset = amount / 8;
        var shift = amount % 8;
        for (var i = 0; i < 16; i++)
        {
            key[i] = (byte)(copy[offset] << shift);
            offset = (offset + 1) % 16;
            key[i] |= (byte)(copy[offset] >> (8 - shift));
        }
    }

    private static byte[] SlotKeyX(byte cryptoMethod)
    {
        return cryptoMethod switch
        {
            0x00 => HexToBytes(Slot2CKeyX),
            0x01 => HexToBytes(Slot25KeyX),
            0x0A => HexToBytes(Slot18KeyX),
            0x0B => HexToBytes(Slot1BKeyX),
            _ => throw new ArgumentException("Unsupported crypto method", nameof(cryptoMethod))
        };
    }

    /// <summary>generate ncch.</summary>
    /// <param name="encrypted">the encrypted parameter</param>
    /// <param name="fixedKey">the fixed key parameter</param>
    /// <param name="seedCrypto">the seed crypto parameter</param>
    /// <param name="cryptoMethod">the crypto method parameter</param>
    /// <param name="version">the version parameter</param>
    /// <param name="programId">the program id parameter</param>
    /// <param name="seed">the seed parameter</param>
    /// <param name="primaryKey">the primary key parameter</param>
    /// <param name="secondaryKey">the secondary key parameter</param>
    /// <param name="codeSize">the code size parameter</param>
    /// <returns>the result</returns>
    public static byte[] GenerateNcch(
        bool encrypted, bool fixedKey, bool seedCrypto, byte cryptoMethod, ushort version,
        byte[]? programId, byte[]? seed, out byte[] primaryKey, out byte[] secondaryKey,
        int codeSize = 0x600)
    {
        const int exefsOffset = 0x400; /* 2 media units after the header */
        const int iconSize = 0x400;

        var header = new byte[0x200];
        byte[] partitionId = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];

        /* the first 16 bytes of the header are the "primary key Y"; they must
         * be non-zero (the C's key-presence checks use the first byte) */
        byte[] signature = [0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98];
        Array.Copy(signature, 0, header, 0, 16);

        /* "NCCH" */
        header[0x100] = (byte)'N';
        header[0x101] = (byte)'C';
        header[0x102] = (byte)'C';
        header[0x103] = (byte)'H';

        /* partition id */
        Array.Copy(partitionId, 0, header, 0x108, 8);

        /* version */
        header[0x112] = (byte)(version & 0xFF);
        header[0x113] = (byte)(version >> 8);

        /* program id (only used for seed crypto) */
        if (programId != null)
            Array.Copy(programId, 0, header, 0x118, 8);

        /* crypto flags */
        header[0x188 + 3] = cryptoMethod;
        if (encrypted && fixedKey)
        {
            header[0x188 + 7] |= 0x01;
        }

        if (seedCrypto)
        {
            header[0x188 + 7] |= 0x20;
        }

        if (!encrypted)
        {
            header[0x188 + 7] |= 0x04;
        }

        /* exefs offset / size in media units */
        /* the ExeFS spans whole media units: the section padding is included */
        var exefsSize = 0x200 + (uint)iconSize + (uint)((codeSize + 0x1FF) & ~0x1FF);
        header[0x1A0] = exefsOffset / 0x200;
        header[0x1A4] = (byte)(exefsSize / 0x200);

        /* build the ExeFS (section table + icon + .code); section offsets are
         * relative to the end of the section table (the engine adds 0x200) */
        var exefs = new byte[exefsSize];
        WriteSection(exefs, 0, "icon", 0x000, iconSize);
        WriteSection(exefs, 1, ".code", iconSize, (uint)codeSize);
        Fill(exefs, 0x200, iconSize, 0x11);
        Fill(exefs, 0x200 + iconSize, codeSize, 0x22);

        var ncch = new byte[exefsOffset + exefsSize];
        Array.Copy(header, 0, ncch, 0, 0x200);
        Array.Copy(exefs, 0, ncch, exefsOffset, exefsSize);

        primaryKey = new byte[16];
        secondaryKey = new byte[16];

        if (encrypted)
        {
            if (fixedKey)
            {
                /* all-zero keys */
            }
            else
            {
                /* primary key y is the first 16 bytes of the NCCH header */
                var primaryKeyY = new byte[16];
                Array.Copy(header, 0, primaryKeyY, 0, 16);

                primaryKey = NormalizeKeys(HexToBytes(Slot2CKeyX), primaryKeyY);

                byte[] secondaryKeyY;
                if (seedCrypto)
                {
                    var digest = SHA256.HashData(Concat(primaryKeyY, seed!));
                    secondaryKeyY = new byte[16];
                    Array.Copy(digest, 0, secondaryKeyY, 0, 16);
                }
                else
                {
                    secondaryKeyY = primaryKeyY;
                }

                secondaryKey = NormalizeKeys(SlotKeyX(cryptoMethod), secondaryKeyY);
            }

            /* the CTR counter (continuous across the whole ExeFS) */
            var counter = new byte[16];
            if (version is 0 or 2)
            {
                for (var i = 0; i < 8; i++)
                {
                    counter[7 - i] = header[0x108 + i];
                }

                counter[8] = 2;
            }
            else
            {
                for (var i = 0; i < 8; i++)
                {
                    counter[i] = header[0x108 + i];
                }

                counter[12] = 0; /* (exefsOffset >> 24) & 0xFF — 0x400 < 2^24 */
                counter[13] = 0; /* (exefsOffset >> 16) & 0xFF — 0x400 < 2^16 */
                counter[14] = (exefsOffset >> 8) & 0xFF; /* 0x400 >> 8 = 4 */
                counter[15] = 0; /* exefsOffset & 0xFF — 0x400 is 0x100-aligned */
            }

            /* section table (first 0x200 bytes) and icon use the primary key */
            AesHelper.AesCtrXcrypt(ncch, exefsOffset, 0x200, primaryKey, counter);
            AesHelper.AesCtrXcrypt(ncch, exefsOffset + 0x200, iconSize, primaryKey, counter);
            /* .code uses the secondary key (the section's final partial block
             * and the following padding stay within this region) */
            AesHelper.AesCtrXcrypt(ncch, exefsOffset + 0x200 + iconSize, codeSize, secondaryKey, counter);
        }

        return ncch;
    }

    /// <summary>generate3 dsx.</summary>
    /// <returns>the result</returns>
    [SuppressMessage("ReSharper", "ConvertToConstant.Local")]
    public static byte[] Generate3Dsx()
    {
        uint headerSize = 0x80;
        uint relocHeaderSize = 0x20;
        uint codeSize = 0x2000;
        var codeOffset = headerSize + relocHeaderSize * 3;

        var file = new byte[codeOffset + codeSize];
        file[0] = (byte)'3';
        file[1] = (byte)'D';
        file[2] = (byte)'S';
        file[3] = (byte)'X';
        file[4] = (byte)(headerSize & 0xFF);
        file[5] = (byte)(headerSize >> 8);
        file[6] = (byte)(relocHeaderSize & 0xFF);
        file[7] = (byte)(relocHeaderSize >> 8);
        file[0x10] = (byte)(codeSize & 0xFF);
        file[0x11] = (byte)((codeSize >> 8) & 0xFF);
        file[0x12] = (byte)((codeSize >> 16) & 0xFF);
        file[0x13] = (byte)((codeSize >> 24) & 0xFF);
        Fill(file, (int)codeOffset, (int)codeSize, 0x33);
        return file;
    }

    /// <summary>generate cia.</summary>
    /// <param name="ncch">the ncch parameter</param>
    /// <param name="titleId">the title id parameter</param>
    /// <param name="commonKeyIndex">the common key index parameter</param>
    /// <param name="titleKey">the title key parameter</param>
    /// <param name="encryptContent">the encrypt content parameter</param>
    /// <returns>the result</returns>
    [SuppressMessage("ReSharper", "ConvertToConstant.Local")]
    public static byte[] GenerateCia(byte[] ncch, byte[] titleId, byte commonKeyIndex, byte[] titleKey, bool encryptContent)
    {
        uint certSize = 0;
        uint tikSize = 0x400;
        uint tmdSize = 4 + 0x23C + 0x9C4 + 0x30;
        const uint ciaHeaderSize = 0x2020;
        const uint alignMask = 63;

        const uint certOffset = (ciaHeaderSize + alignMask) & ~alignMask;
        /* certSize is 0 (no certificate chain), so the ticket starts right at the aligned cert offset */
        var tikOffset = (certOffset + alignMask) & ~alignMask;
        var tmdOffset = (tikOffset + tikSize + alignMask) & ~alignMask;
        var contentOffset = (tmdOffset + tmdSize + alignMask) & ~alignMask;

        var cia = new byte[contentOffset + (uint)ncch.Length];
        /* header */
        cia[0] = 0x20;
        cia[1] = 0x20;
        cia[2] = 0x00;
        cia[3] = 0x00;
        cia[8] = (byte)(certSize & 0xFF);
        cia[9] = (byte)((certSize >> 8) & 0xFF);
        cia[10] = (byte)((certSize >> 16) & 0xFF);
        cia[11] = (byte)((certSize >> 24) & 0xFF);
        cia[12] = (byte)(tikSize & 0xFF);
        cia[13] = (byte)((tikSize >> 8) & 0xFF);
        cia[14] = (byte)((tikSize >> 16) & 0xFF);
        cia[15] = (byte)((tikSize >> 24) & 0xFF);
        cia[16] = (byte)(tmdSize & 0xFF);
        cia[17] = (byte)((tmdSize >> 8) & 0xFF);
        cia[18] = (byte)((tmdSize >> 16) & 0xFF);
        cia[19] = (byte)((tmdSize >> 24) & 0xFF);

        /* ticket: signature type 0x00010000 (RSA-2048, sig size 0x23C) */
        var tik = (int)tikOffset;
        cia[tik] = 0x00;
        cia[tik + 1] = 0x01;
        cia[tik + 2] = 0x00;
        cia[tik + 3] = 0x00;
        /* the engine reads the ticket data after the 4-byte signature type */
        var tikData = tik + 4 + 0x23C;

        /* encrypted title key: CBC(common normal key, iv = title id padded to 16) */
        var commonKey = NormalizeKeys(HexToBytes(Slot3DKeyX), HexToBytes(Common0));
        var titleKeyCopy = (byte[])titleKey.Clone();
        var titleKeyIv = new byte[16];
        Array.Copy(titleId, 0, titleKeyIv, 0, 8);
        AesHelper.AesCbcEncrypt(titleKeyCopy, 0, 16, commonKey, titleKeyIv);
        Array.Copy(titleKeyCopy, 0, cia, tikData + 0x7F, 16);
        Array.Copy(titleId, 0, cia, tikData + 0x9C, 8);
        cia[tikData + 0xB1] = commonKeyIndex;

        /* TMD: signature type 0x00010000, content count 1, main content chunk.
         * the engine's offsets (0x9E / 0x9C4) are relative to the end of the
         * 4-byte signature type */
        var tmd = (int)tmdOffset;
        cia[tmd] = 0x00;
        cia[tmd + 1] = 0x01;
        cia[tmd + 2] = 0x00;
        cia[tmd + 3] = 0x00;
        var tmdCount = tmd + 4 + 0x23C + 0x9E;
        cia[tmdCount] = 0x00;
        cia[tmdCount + 1] = 0x01; /* content count = 1 */
        var chunk = tmd + 4 + 0x23C + 0x9C4;
        cia[chunk + 4] = 0x00;
        cia[chunk + 5] = 0x00; /* content index 0 */
        cia[chunk + 7] = (byte)(encryptContent ? 0x01 : 0x00); /* flags: encrypted */
        var contentSize = (uint)ncch.Length;
        cia[chunk + 0xC] = (byte)(contentSize & 0xFF);
        cia[chunk + 0xD] = (byte)((contentSize >> 8) & 0xFF);
        cia[chunk + 0xE] = (byte)((contentSize >> 16) & 0xFF);
        cia[chunk + 0xF] = (byte)((contentSize >> 24) & 0xFF);

        /* content */
        var content = (byte[])ncch.Clone();
        if (encryptContent)
        {
            /* whole content is one CBC chain starting at the content offset with iv = 0
             * (content index 0) */
            AesHelper.AesCbcEncrypt(content, 0, content.Length, titleKey, new byte[16]);
        }

        Array.Copy(content, 0, cia, contentOffset, content.Length);
        return cia;
    }

    private static void WriteSection(byte[] exefs, int index, string name, uint offset, uint size)
    {
        var pos = index * 16;
        for (var i = 0; i < name.Length && i < 8; i++)
        {
            exefs[pos + i] = (byte)name[i];
        }

        exefs[pos + 8] = (byte)(offset & 0xFF);
        exefs[pos + 9] = (byte)((offset >> 8) & 0xFF);
        exefs[pos + 10] = (byte)((offset >> 16) & 0xFF);
        exefs[pos + 11] = (byte)((offset >> 24) & 0xFF);
        exefs[pos + 12] = (byte)(size & 0xFF);
        exefs[pos + 13] = (byte)((size >> 8) & 0xFF);
        exefs[pos + 14] = (byte)((size >> 16) & 0xFF);
        exefs[pos + 15] = (byte)((size >> 24) & 0xFF);
    }

    private static void Fill(byte[] buffer, int offset, int length, byte value)
    {
        for (var i = 0; i < length; i++)
        {
            buffer[offset + i] = value;
        }
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Array.Copy(a, 0, result, 0, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        return result;
    }
}
