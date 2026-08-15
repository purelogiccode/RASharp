// Ported from rcheevos (MIT) — src/rhash/hash.c
// rc_hash_iterator_t — the iterator state carried through hashing
// (path or buffer, callback bags, and the console list for '?' iterate).

namespace RetroAchievementsSharp.Models;

/* struct rc_hash_iterator_t */
/// <summary>struct rc_hash_iterator_t</summary>
public class RcHashIterator
{
    /// <summary>The in-memory data to hash, when hashing a buffer instead of a file.</summary>
    public byte[]? Buffer;

    /// <summary>The number of valid bytes in <see cref="Buffer"/>.</summary>
    public int BufferSize;

    /// <summary>The console candidates for '?' auto-detection (filled by the extension handlers).</summary>
    public uint[] Consoles = new uint[12];

    /// <summary>The index of the next console candidate to try in <see cref="Consoles"/>.</summary>
    public int Index;

    /// <summary>The file path to hash, or null when hashing a buffer.</summary>
    public string? Path;

    /// <summary>Arbitrary user data passed through the hash calls.</summary>
    public object? Userdata;

    /// <summary>The callback bag (filereader, cdreader, encryption and messages) for this iterator.</summary>
    public RcHashCallbacks Callbacks = new();
}
