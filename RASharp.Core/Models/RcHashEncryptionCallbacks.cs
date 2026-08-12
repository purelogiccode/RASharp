// Ported from rcheevos (MIT) — include/rc_hash.h
// struct rc_hash_encryption_callbacks (nested in rc_hash_callbacks_t) —
// the 3DS key functions.

using RASharp.Core;

namespace RASharp.Core.Models;

/* struct rc_hash_encryption_callbacks (nested in rc_hash_callbacks_t) */
public class RcHashEncryptionCallbacks
{
    public RcHash3DsGetCiaNormalKeyFunc? Get3DsCiaNormalKey;
    public RcHash3DsGetNcchNormalKeysFunc? Get3DsNcchNormalKeys;
}
