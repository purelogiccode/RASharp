// Ported from rcheevos (MIT) — src/rhash/md5.c (md5_init/append/final semantics)
// Implementation uses the BCL MD5 provider; the wrapper exists so the engine
// can be translated 1:1 without touching the digest code.

using System.Security.Cryptography;

namespace RASharp;

/// <summary>Ported from rcheevos (MIT) — src/rhash/md5.c (md5_init/append/final semantics) Implementation uses the BCL MD5 provider; the wrapper exists so the engine can be</summary>
public sealed class HashMd5
{
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);

    /// <summary>Appends bytes to the MD5 state.</summary>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="length">the length parameter</param>
    public void Append(byte[] buffer, int length)
    {
        _hash.AppendData(buffer, 0, length);
    }

    /// <summary>Appends bytes to the MD5 state.</summary>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="offset">the byte offset</param>
    /// <param name="length">the length parameter</param>
    public void Append(byte[] buffer, int offset, int length)
    {
        _hash.AppendData(buffer, offset, length);
    }

    /// <summary>finish.</summary>
    /// <returns>the result</returns>
    public byte[] Finish()
    {
        return _hash.GetHashAndReset();
    }
}
