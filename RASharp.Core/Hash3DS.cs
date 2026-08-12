// Hash3DS — 3DS key retrieval for the encryption callbacks.
// Behavior parity with RALibretro RAHasher's Hash3DS.cpp (GPL, used as
// reference only — this is a new implementation): aes_keys.txt parsing
// (keyX/keyY slots), key normalization (ROL/XOR/ADD with the generator
// constant), seeddb.bin lookups, and SHA-256 seed mixing.

using System.Security.Cryptography;
using Serilog;

namespace RASharp.Core;

public static class Hash3DS
{
    private static string g_systemDir = ".";

    /* the "have" flags in Hash3DS.cpp are the first byte of each key */
    private static bool KeyIsPresent(byte[] key)
    {
        return key[0] != 0;
    }

    /* rhash_read_128bit_hex — 32 hex chars -> 16 bytes (strtol per pair;
     * the C reads past short lines, we just treat missing chars as 0) */
    private static void Read128BitHex(string hex, byte[] key)
    {
        if (hex.Length < 32)
            hex = hex + new string('0', 32 - hex.Length);

        for (int index = 0; index < 16; ++index)
        {
            string pair = hex.Substring(index * 2, 2);
            key[index] = (byte)(int.TryParse(pair, System.Globalization.NumberStyles.HexNumber, null, out int value) ? value : 0);
        }
    }

    /* rhash_rol_128bit */
    private static void Rol128Bit(byte[] key, int amount)
    {
        byte[] copy = (byte[])key.Clone();
        int offset = amount / 8;
        int shift = amount % 8;

        for (int index = 0; index < 16; ++index)
        {
            key[index] = (byte)(copy[offset] << shift);
            offset = (offset + 1) % 16;
            key[index] |= (byte)(copy[offset] >> (8 - shift));
        }
    }

    /* rhash_xor_128bit */
    private static void Xor128Bit(byte[] key, byte[] value)
    {
        for (int index = 0; index < 16; ++index)
            key[index] ^= value[index];
    }

    /* rhash_add_128bit */
    private static void Add128Bit(byte[] key, byte[] value)
    {
        ushort carry = 0;
        for (int index = 15; index >= 0; --index)
        {
            carry += (ushort)(key[index] + value[index]);
            key[index] = (byte)(carry & 0xFF);
            carry >>= 8;
        }
    }

    /* rhash_3ds_normalize_keys */
    private static int NormalizeKeys(byte[] keyX, byte[] keyY, byte[] keyN)
    {
        if (KeyIsPresent(keyX) && KeyIsPresent(keyY))
        {
            byte[] generatorConstant =
            {
                0x1F, 0xF9, 0xE9, 0xAA, 0xC5, 0xFE, 0x04, 0x08,
                0x02, 0x45, 0x91, 0xDC, 0x5D, 0x52, 0x76, 0x8A
            };

            Array.Copy(keyX, 0, keyN, 0, 16);
            Rol128Bit(keyN, 2);
            Xor128Bit(keyN, keyY);
            Add128Bit(keyN, generatorConstant);
            Rol128Bit(keyN, 87);
            return 1;
        }

        return 0;
    }

    /* rhash_3ds_lookup_cia_normal_key */
    private static int LookupCiaNormalKey(byte index, byte[] key)
    {
        string[] lines;
        string scan = "common" + index + "=";
        byte[] keyX = new byte[16];
        byte[] keyY = new byte[16];

        try
        {
            lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(g_systemDir, "aes_keys.txt"));
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not open aes_keys.txt in {SystemDir}", g_systemDir);
            CallError("Could not open aes_keys.txt");
            return 0;
        }

        foreach (string line in lines)
        {
            if (line.StartsWith(scan, StringComparison.Ordinal))
            {
                Read128BitHex(line.Substring(scan.Length), keyY);
                if (KeyIsPresent(keyX))
                    break;
            }
            else if (line.StartsWith("slot0x3DKeyX=", StringComparison.Ordinal))
            {
                Read128BitHex(line.Substring(13), keyX);
                if (KeyIsPresent(keyY))
                    break;
            }
        }

        return NormalizeKeys(keyX, keyY, key);
    }

    /* rhash_3ds_lookup_ncch_normal_key */
    private static int LookupNcchNormalKey(byte[] primaryKeyY, byte secondaryKeyXSlot, byte[]? programId, byte[] primaryKeyOut, byte[] secondaryKeyOut)
    {
        string[] lines;
        string scan = string.Format("slot0x{0:X2}KeyX=", secondaryKeyXSlot);
        byte[] primaryKeyX = new byte[16];
        byte[] secondaryKeyX = new byte[16];
        byte[] secondaryKeyY = new byte[16];

        try
        {
            lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(g_systemDir, "aes_keys.txt"));
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not open aes_keys.txt in {SystemDir}", g_systemDir);
            CallError("Could not open aes_keys.txt");
            return 0;
        }

        foreach (string line in lines)
        {
            if (line.StartsWith("slot0x2CKeyX=", StringComparison.Ordinal))
            {
                Read128BitHex(line.Substring(13), primaryKeyX);
                if (KeyIsPresent(secondaryKeyX))
                    break;

                if (secondaryKeyXSlot == 0x2C)
                {
                    Array.Copy(primaryKeyX, 0, secondaryKeyX, 0, secondaryKeyX.Length);
                    break;
                }
            }
            else if (line.StartsWith(scan, StringComparison.Ordinal))
            {
                Read128BitHex(line.Substring(13), secondaryKeyX);
                if (KeyIsPresent(primaryKeyX))
                    break;
            }
        }

        if (NormalizeKeys(primaryKeyX, primaryKeyY, primaryKeyOut) == 0)
            return 0;

        if (programId == null)
        {
            Array.Copy(primaryKeyY, 0, secondaryKeyY, 0, secondaryKeyY.Length);
        }
        else
        {
            uint count;
            byte[] buffer = new byte[8];

            /* find the seed for the programId */
            byte[]? seeddb;
            try
            {
                seeddb = System.IO.File.ReadAllBytes(System.IO.Path.Combine(g_systemDir, "seeddb.bin"));
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Could not open seeddb.bin in {SystemDir}", g_systemDir);
                CallError("Could not open seeddb.bin");
                return 0;
            }

            /* seeddb.bin's layout is simply the first 4 bytes indicate the amount of seeds in the
             * file, followed by 12 bytes of padding. Then a collection of seeds in the format of
             * 8 bytes for the program id, then 16 bytes for the seed, then 8 bytes of padding */
            bool found = false;
            if (seeddb.Length >= 4 + 12)
            {
                count = (uint)(seeddb[0] | (seeddb[1] << 8) | (seeddb[2] << 16) | (seeddb[3] << 24));
                int pos = 4 + 12;

                for (; count > 0 && !found; count--)
                {
                    if (pos + 24 > seeddb.Length)
                        break;

                    Array.Copy(seeddb, pos, buffer, 0, 8);
                    if (MemEquals(buffer, programId, 8))
                    {
                        Array.Copy(seeddb, pos + 8, secondaryKeyY, 0, secondaryKeyY.Length);
                        found = true;
                        break;
                    }

                    pos += 16 + 8;
                }
            }

            if (!found) /* did not find programId in seeddb.bin */
                return 0;

            /* the actual secondaryKeyY used to generate the normalized key is the first 16 bytes
             * of the SHA256 of the primaryKeyY and the seed pulled from seeddb.bin */
            byte[] digest = SHA256.HashData(Concat(primaryKeyY, secondaryKeyY));
            Array.Copy(digest, 0, secondaryKeyY, 0, secondaryKeyY.Length);
        }

        if (NormalizeKeys(secondaryKeyX, secondaryKeyY, secondaryKeyOut) == 0)
            return 0;

        return 1;
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        byte[] result = new byte[a.Length + b.Length];
        Array.Copy(a, 0, result, 0, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        return result;
    }

    private static bool MemEquals(byte[] a, byte[] b, int length)
    {
        for (int i = 0; i < length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    private static void CallError(string message)
    {
        /* the C logs through the error-message callback (rhash_log_error_message) */
        HashEngine.CallErrorMessage(message);
    }

    /* initHash3DS */
    public static void InitHash3DS(string systemDir)
    {
        g_systemDir = systemDir;

        HashEngine.HashInit3DsGetCiaNormalKeyFunc(LookupCiaNormalKey);
        HashEngine.HashInit3DsGetNcchNormalKeysFunc(LookupNcchNormalKey);
    }
}
