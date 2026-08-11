// Ported from rcheevos (MIT) — include/rc_hash.h
// Callback delegates and the rc_hash_callbacks_t / rc_hash_filereader_t /
// rc_hash_cdreader_t structs. Mirror the C function-pointer semantics: a
// null delegate means "handler not registered", exactly like a NULL
// function pointer in C.

namespace RASharp.Core;

/* new-style message callback: (message, iterator) */
public delegate void RcHashMessageCallback(string message, RcHashIterator? iterator);

/* deprecated message callback: (message) */
public delegate void RcHashMessageCallbackDeprecated(string message);

/* 3DS key functions (used from Phase 6 on) */
public delegate int RcHash3DsGetCiaNormalKeyFunc(byte commonKeyIndex, byte[] outNormalKey);
public delegate int RcHash3DsGetNcchNormalKeysFunc(byte[] primaryKeyY, byte secondaryKeyXSlot, byte[]? optionalProgramId, byte[] outPrimaryKey, byte[] outSecondaryKey);

/* struct rc_hash_filereader_t */
public class RcHashFilereader
{
    public Func<string, object?>? Open;
    public Action<object, long, int>? Seek;
    public Func<object, long>? Tell;
    public Func<object, byte[], int, int>? Read;
    public Action<object>? Close;
}

/* struct rc_hash_cdreader_t */
public class RcHashCdreader
{
    public Func<string, uint, object?>? OpenTrack;
    public Func<object, uint, byte[], int, int>? ReadSector;
    public Action<object>? CloseTrack;
    public Func<object, uint>? FirstTrackSector;
    public Func<string, uint, RcHashIterator, object?>? OpenTrackIterator;
}

/* struct rc_hash_encryption_callbacks (nested in rc_hash_callbacks_t) */
public class RcHashEncryptionCallbacks
{
    public RcHash3DsGetCiaNormalKeyFunc? Get3DsCiaNormalKey;
    public RcHash3DsGetNcchNormalKeysFunc? Get3DsNcchNormalKeys;
}

/* struct rc_hash_callbacks_t */
public class RcHashCallbacks
{
    public RcHashMessageCallback? VerboseMessage;
    public RcHashMessageCallback? ErrorMessage;
    public RcHashFilereader Filereader = new();
    public RcHashCdreader Cdreader = new();
    public RcHashEncryptionCallbacks Encryption = new();
}
