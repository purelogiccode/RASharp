// Ported from rcheevos (MIT) — include/rc_hash.h
// Callback delegates. The rc_hash_callbacks_t / rc_hash_filereader_t /
// rc_hash_cdreader_t structs live in Models/. Mirror the C function-pointer
// semantics: a null delegate means "handler not registered", exactly like a
// NULL function pointer in C.

using RetroAchievementsSharp.Models;

namespace RetroAchievementsSharp;

/* new-style message callback: (message, iterator) */
/// <summary>new-style message callback: (message, iterator)</summary>
public delegate void RcHashMessageCallback(string message, RcHashIterator? iterator);

/* deprecated message callback: (message) */
/// <summary>deprecated message callback: (message)</summary>
public delegate void RcHashMessageCallbackDeprecated(string message);

/* 3DS key functions (used from Phase 6 on) */
/// <summary>3DS key functions (used from Phase 6 on)</summary>
public delegate int RcHash3DsGetCiaNormalKeyFunc(byte commonKeyIndex, byte[] outNormalKey);

/// <summary>Ported from rcheevos (MIT) — include/rc_hash.h Callback delegates. The rc_hash_callbacks_t / rc_hash_filereader_t / rc_hash_cdreader_t structs live in Models/. </summary>
public delegate int RcHash3DsGetNcchNormalKeysFunc(byte[] primaryKeyY, byte secondaryKeyXSlot, byte[]? optionalProgramId, byte[] outPrimaryKey, byte[] outSecondaryKey);
