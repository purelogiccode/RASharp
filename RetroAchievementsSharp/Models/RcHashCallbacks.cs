// Ported from rcheevos (MIT) — include/rc_hash.h
// struct rc_hash_callbacks_t — the callback bag attached to every iterator.

namespace RetroAchievementsSharp.Models;

/* struct rc_hash_callbacks_t */
/// <summary>struct rc_hash_callbacks_t</summary>
public class RcHashCallbacks
{
    /// <summary>Callback invoked for verbose messages (parity: same text as the C engine).</summary>
    public RcHashMessageCallback? VerboseMessage;

    /// <summary>Callback invoked for error messages.</summary>
    public RcHashMessageCallback? ErrorMessage;

    /// <summary>The file reader callbacks used for hashing files.</summary>
    public RcHashFilereader Filereader = new();

    /// <summary>The CD reader callbacks used for disc hashing.</summary>
    public RcHashCdreader Cdreader = new();

    /// <summary>The 3DS key callbacks used for encrypted formats.</summary>
    public RcHashEncryptionCallbacks Encryption = new();
}
