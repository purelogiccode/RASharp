// Ported from rcheevos (MIT) — src/rhash/hash.c
// Extension-handler table entry used by '?' auto-detect (rc_hash_iterate).
// The table entries and their order are copied verbatim.

namespace RetroAchievementsSharp.Models;

/// <summary>Ported from rcheevos (MIT) — src/rhash/hash.c Extension-handler table entry used by '?' auto-detect (rc_hash_iterate). The table entries and their order are cop</summary>
public sealed record ExtHandlerEntry(string Ext, Action<RcHashIterator, int> Handler, int Data);
