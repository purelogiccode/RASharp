// Ported from rcheevos (MIT) — include/rc_hash.h
// struct rc_hash_filereader_t — mirrors the C function-pointer semantics:
// a null delegate means "handler not registered", exactly like a NULL
// function pointer in C.

namespace RASharp.Core.Models;

/* struct rc_hash_filereader_t */
public class RcHashFilereader
{
    public Func<string, object?>? Open;
    public Action<object, long, int>? Seek;
    public Func<object, long>? Tell;
    public Func<object, byte[], int, int>? Read;
    public Action<object>? Close;
}
