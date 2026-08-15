# Public API

`RASharp` mirrors `include/rc_hash.h`. The primary entry points are on
the static `RcHash` class.

## Hashing

```csharp
public static bool GenerateFromFile(out string hash, uint consoleId, string path)
public static bool GenerateFromBuffer(out string hash, uint consoleId, byte[] buffer, int bufferSize)
```

- `hash` — 32 lowercase hex characters on success.
- Returns `false` on failure (the error callback receives the message).

## Initialization callbacks

```csharp
public static void InitErrorMessageCallback(RcHashMessageCallbackDeprecated? callback)
public static void InitVerboseMessageCallback(RcHashMessageCallbackDeprecated? callback)
public static void InitCustomFilereader(RcHashFilereader? reader)
public static void GetDefaultCdreader(RcHashCdreader cdreader)
public static void InitDefaultCdreader()
public static void InitCustomCdreader(RcHashCdreader? reader)
public static void Init3DsGetCiaNormalKeyFunc(RcHash3DsGetCiaNormalKeyFunc func)
public static void Init3DsGetNcchNormalKeysFunc(RcHash3DsGetNcchNormalKeysFunc func)
```

These mirror `rc_hash_init_*` and `rc_hash_get_default_cdreader`. The 3DS
key funcs wire the decryption key providers (see
[3DS encryption](../architecture/encrypted.md)).

## Iterator API

```csharp
// namespace RASharp.Models
public sealed class RcHashIterator { ... }        // rc_hash_iterator_t
public sealed record ExtHandlerEntry(string Ext, Action<RcHashIterator, int> Handler, int Data);

// namespace RASharp
public static class HashIterator
{
    public static void InitializeIterator(RcHashIterator iterator, string? path, byte[]? buffer, int bufferSize)
    public static int  Iterate(out string hash, RcHashIterator iterator)   // 0 = no match
    public static void DestroyIterator(RcHashIterator iterator)
    public static ExtHandlerEntry[] GetIteratorExtHandlers(out int numHandlers)
}
```

`Iterate` drives the `?` auto-detect: it walks the handler table in the C's
exact order and returns the first console whose algorithm accepts the file.

## Delegate structs (`RASharp.Models`)

```csharp
public sealed class RcHashFilereader      // rc_hash_filereader: Open/Seek/Tell/Read/Close
public sealed class RcHashCdreader        // rc_hash_cdreader: OpenTrack/ReadSector/CloseTrack/
                                          //   FirstTrackSector/OpenTrackIterator
public sealed class RcHashEncryptionCallbacks  // 3DS key funcs
public sealed class RcHashCallbacks            // VerboseMessage/ErrorMessage + Filereader/Cdreader/Encryption
public sealed class CdromTrack                 // rc_hash_cdrom_track_t (sector math state)
public delegate void RcHashMessageCallbackDeprecated(string message)   // RASharp (Callbacks.cs)
```

## Constants

```csharp
public static class ConsoleIds
{
    // RC_CONSOLE_* — e.g. RC_CONSOLE_NINTENDO = 7, RC_CONSOLE_ARCADE = 27,
    //                RC_CONSOLE_NINTENDO_3DS = 62, ...
    public const uint RC_CONSOLE_MAX = 90;
    public const uint RC_HASH_CDTRACK_FIRST_DATA = 0xFFFFFFFF;  // -1
    public const uint RC_HASH_CDTRACK_LAST = 0xFFFFFFFE;        // -2
    public const uint RC_HASH_CDTRACK_LARGEST = 0xFFFFFFFD;     // -3
    public const uint RC_HASH_CDTRACK_FIRST_OF_SECOND_SESSION = 0xFFFFFFFC; // -4
}
```

See the [console table](console-table.md) for all ids.

## Library internals (used by tests)

`HashEngine` exposes the dispatch and primitives (`WholeFile`,
`BufferedFile`, `FromBuffer`, `FileOpen/Seek/Tell/Read/Close`,
`IteratorError(Formatted)`, `IteratorVerbose(Formatted)`, `Finalize`,
`MAX_BUFFER_SIZE`). `HashRom` / `HashDisc` / `HashZip` / `HashEncrypted` /
`CdReader` / `ChdCdReader` are `public static` for testability — they are
the module ports described in [Architecture](../architecture/overview.md).

## CLI

`RASharp.Cli` exposes `Consoles.All` (`ConsoleInfo[]`: id, key, group,
name) — the factual metadata table used by the usage output.
