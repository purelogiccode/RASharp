// Ported from rcheevos (MIT) — src/rhash/md5.c (md5_init/append/final semantics)
// Implementation uses the BCL MD5 provider; the wrapper exists so the engine
// can be translated 1:1 without touching the digest code.

using System.Security.Cryptography;

namespace RASharp.Core;

public sealed class HashMd5
{
    private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);

    public void Append(byte[] buffer, int length)
    {
        _hash.AppendData(buffer, 0, length);
    }

    public void Append(byte[] buffer, int offset, int length)
    {
        _hash.AppendData(buffer, offset, length);
    }

    public byte[] Finish()
    {
        return _hash.GetHashAndReset();
    }
}
