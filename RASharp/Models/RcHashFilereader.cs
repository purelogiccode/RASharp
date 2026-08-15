// Ported from rcheevos (MIT) — include/rc_hash.h
// struct rc_hash_filereader_t — mirrors the C function-pointer semantics:
// a null delegate means "handler not registered", exactly like a NULL
// function pointer in C.

namespace RASharp.Models;

/* struct rc_hash_filereader_t */
/// <summary>struct rc_hash_filereader_t</summary>
public class RcHashFilereader
{
    /// <summary>Opens a file and returns a file handle, or null on failure.</summary>
    public Func<string, object?>? Open;

    /// <summary>Seeks to an offset in an open file (origin: <see cref="HashEngine.SeekSet"/>/<see cref="HashEngine.SeekCur"/>/<see cref="HashEngine.SeekEnd"/>).</summary>
    public Action<object, long, int>? Seek;

    /// <summary>Returns the current position of an open file.</summary>
    public Func<object, long>? Tell;

    /// <summary>Reads up to the requested number of bytes; returns the number of bytes read.</summary>
    public Func<object, byte[], int, int>? Read;

    /// <summary>Closes an open file handle.</summary>
    public Action<object>? Close;
}
