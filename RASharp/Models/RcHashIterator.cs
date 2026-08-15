// Ported from rcheevos (MIT) — src/rhash/hash.c
// rc_hash_iterator_t — the iterator state carried through hashing
// (path or buffer, callback bags, and the console list for '?' iterate).

namespace RASharp.Models;

/* struct rc_hash_iterator_t */
/// <summary>struct rc_hash_iterator_t</summary>
public class RcHashIterator
{
    public byte[]? Buffer;
    public int BufferSize;
    public uint[] Consoles = new uint[12];
    public int Index;
    public string? Path;
    public object? Userdata;
    public RcHashCallbacks Callbacks = new();
}
