// Ported from rcheevos (MIT) — include/rc_hash.h
// struct rc_hash_encryption_callbacks (nested in rc_hash_callbacks_t) —
// the 3DS key functions.

namespace RASharp.Models;

/* struct rc_hash_encryption_callbacks (nested in rc_hash_callbacks_t) */
/// <summary>struct rc_hash_encryption_callbacks (nested in rc_hash_callbacks_t)</summary>
public class RcHashEncryptionCallbacks
{
    /// <summary>Provides the 3DS normal key for a CIA normal-key index (0–5).</summary>
    public RcHash3DsGetCiaNormalKeyFunc? Get3DsCiaNormalKey;

    /// <summary>Provides the 3DS normal keys for an NCCH header's key indices.</summary>
    public RcHash3DsGetNcchNormalKeysFunc? Get3DsNcchNormalKeys;
}
