// Ported from rcheevos (MIT) — include/rc_hash.h
// struct rc_hash_callbacks_t — the callback bag attached to every iterator.

using RASharp.Core;

namespace RASharp.Core.Models;

/* struct rc_hash_callbacks_t */
/// <summary>struct rc_hash_callbacks_t</summary>
public class RcHashCallbacks
{
    public RcHashMessageCallback? VerboseMessage;
    public RcHashMessageCallback? ErrorMessage;
    public RcHashFilereader Filereader = new();
    public RcHashCdreader Cdreader = new();
    public RcHashEncryptionCallbacks Encryption = new();
}
