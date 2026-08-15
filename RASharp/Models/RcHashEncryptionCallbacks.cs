// Ported from rcheevos (MIT) — include/rc_hash.h
// struct rc_hash_encryption_callbacks (nested in rc_hash_callbacks_t) —
// the 3DS key functions.

namespace RASharp.Models;

/* struct rc_hash_encryption_callbacks (nested in rc_hash_callbacks_t) */
/// <summary>struct rc_hash_encryption_callbacks (nested in rc_hash_callbacks_t)</summary>
public class RcHashEncryptionCallbacks
{
    public RcHash3DsGetCiaNormalKeyFunc? Get3DsCiaNormalKey;
    public RcHash3DsGetNcchNormalKeysFunc? Get3DsNcchNormalKeys;
}
