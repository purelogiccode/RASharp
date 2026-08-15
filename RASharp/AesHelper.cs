// AesHelper — AES-128 primitives matching the call patterns of rcheevos'
// aes.c (MIT reference; BCL-backed). The engine uses 128-bit keys only
// (AES_KEYLEN == 16), CBC with no padding, and CTR whose counter is the
// full 128-bit big-endian block counter incremented after every full block
// (the C mutates the IV across calls, so the counter is passed by reference).

using System.Security.Cryptography;

namespace RASharp;

/// <summary>AesHelper — AES-128 primitives matching the call patterns of rcheevos' aes.c (MIT reference; BCL-backed). The engine uses 128-bit keys only (AES_KEYLEN == 16), </summary>
public static class AesHelper
{
    /// <summary>The AES-128 key length in bytes.</summary>
    public const int KeyLen = 16;

    /// <summary>The AES block length in bytes.</summary>
    public const int BlockLen = 16;

    /* AES_CBC_decrypt_buffer */
    /// <summary>AES_CBC_decrypt_buffer</summary>
    /// <param name="data">the data</param>
    /// <param name="offset">the byte offset</param>
    /// <param name="length">the length parameter</param>
    /// <param name="key">the console key or numeric id</param>
    /// <param name="iv">the iv parameter</param>
    public static void AesCbcDecrypt(byte[] data, int offset, int length, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.KeySize = 128;
        aes.Key = key;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        decryptor.TransformBlock(data, offset, length, data, offset);
    }

    /* AES_CBC_encrypt_buffer (test-fixture helper; the engine only decrypts) */
    /// <summary>AES_CBC_encrypt_buffer (test-fixture helper; the engine only decrypts)</summary>
    /// <param name="data">the data</param>
    /// <param name="offset">the byte offset</param>
    /// <param name="length">the length parameter</param>
    /// <param name="key">the console key or numeric id</param>
    /// <param name="iv">the iv parameter</param>
    public static void AesCbcEncrypt(byte[] data, int offset, int length, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.KeySize = 128;
        aes.Key = key;
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor();
        encryptor.TransformBlock(data, offset, length, data, offset);
    }

    /* AES_CTR_xcrypt_buffer — XORs `length` bytes with the keystream derived
     * from `counter` (mutated: incremented after every full 16-byte block,
     * exactly like the C's Iv member) */
    /// <summary>AES_CTR_xcrypt_buffer — XORs `length` bytes with the keystream derived from `counter` (mutated: incremented after every full 16-byte block, exactly like the C's</summary>
    /// <param name="data">the data</param>
    /// <param name="offset">the byte offset</param>
    /// <param name="length">the length parameter</param>
    /// <param name="key">the console key or numeric id</param>
    /// <param name="counter">the counter parameter</param>
    public static void AesCtrXcrypt(byte[] data, int offset, int length, byte[] key, byte[] counter)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.KeySize = 128;
        aes.Key = key;

        using var encryptor = aes.CreateEncryptor();

        /* encrypt the counter stream in 64 KiB chunks: each chunk holds
         * consecutive 128-bit big-endian counters */
        const int chunkCounters = 4096; /* 4096 * 16 = 64 KiB */
        var counterStream = new byte[chunkCounters * BlockLen];

        var pos = 0;
        while (pos < length)
        {
            /* fill the chunk with consecutive counters */
            var counters = Math.Min(chunkCounters, (length - pos + BlockLen - 1) / BlockLen);
            for (var c = 0; c < counters; c++)
            {
                Array.Copy(counter, 0, counterStream, c * BlockLen, BlockLen);
                IncrementCounter(counter);
            }

            encryptor.TransformBlock(counterStream, 0, counters * BlockLen, counterStream, 0);

            var count = Math.Min(counters * BlockLen, length - pos);
            for (var i = 0; i < count; i++)
            {
                data[offset + pos + i] ^= counterStream[i];
            }

            pos += count;
        }
    }

    /* full 128-bit big-endian increment (the C's byte-wise carry loop) */
    private static void IncrementCounter(byte[] counter)
    {
        for (var i = BlockLen - 1; i >= 0; i--)
        {
            if (counter[i] == 255)
            {
                counter[i] = 0;
                continue;
            }

            counter[i] += 1;
            break;
        }
    }
}
