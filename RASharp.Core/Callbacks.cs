// Ported from rcheevos (MIT) — include/rc_hash.h
// Callback delegates. The rc_hash_callbacks_t / rc_hash_filereader_t /
// rc_hash_cdreader_t structs live in Models/. Mirror the C function-pointer
// semantics: a null delegate means "handler not registered", exactly like a
// NULL function pointer in C.

using RASharp.Core.Models;

namespace RASharp.Core;

/* new-style message callback: (message, iterator) */
public delegate void RcHashMessageCallback(string message, RcHashIterator? iterator);

/* deprecated message callback: (message) */
public delegate void RcHashMessageCallbackDeprecated(string message);

/* 3DS key functions (used from Phase 6 on) */
public delegate int RcHash3DsGetCiaNormalKeyFunc(byte commonKeyIndex, byte[] outNormalKey);
public delegate int RcHash3DsGetNcchNormalKeysFunc(byte[] primaryKeyY, byte secondaryKeyXSlot, byte[]? optionalProgramId, byte[] outPrimaryKey, byte[] outSecondaryKey);
