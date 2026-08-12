// Ported from rcheevos (MIT) — include/rc_hash.h
// struct rc_hash_cdreader_t — mirrors the C function-pointer semantics:
// a null delegate means "handler not registered", exactly like a NULL
// function pointer in C.

namespace RASharp.Core.Models;

/* struct rc_hash_cdreader_t */
/// <summary>struct rc_hash_cdreader_t</summary>
public class RcHashCdreader
{
    public Func<string, uint, object?>? OpenTrack;
    public Func<object, uint, byte[], int, int>? ReadSector;
    public Action<object>? CloseTrack;
    public Func<object, uint>? FirstTrackSector;
    public Func<string, uint, RcHashIterator, object?>? OpenTrackIterator;
}
