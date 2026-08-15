// Ported from rcheevos (MIT) — include/rc_hash.h
// struct rc_hash_cdreader_t — mirrors the C function-pointer semantics:
// a null delegate means "handler not registered", exactly like a NULL
// function pointer in C.

namespace RASharp.Models;

/* struct rc_hash_cdreader_t */
/// <summary>struct rc_hash_cdreader_t</summary>
public class RcHashCdreader
{
    /// <summary>Opens a track for hashing and returns a track handle (path and track number).</summary>
    public Func<string, uint, object?>? OpenTrack;

    /// <summary>Reads a sector from an open track handle; returns the number of bytes read.</summary>
    public Func<object, uint, byte[], int, int>? ReadSector;

    /// <summary>Closes an open track handle.</summary>
    public Action<object>? CloseTrack;

    /// <summary>Returns the absolute first sector number of an open track.</summary>
    public Func<object, uint>? FirstTrackSector;

    /// <summary>Opens a track for hashing with full iterator context (path, track, and iterator).</summary>
    public Func<string, uint, RcHashIterator, object?>? OpenTrackIterator;
}
