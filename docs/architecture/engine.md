# The engine (`HashEngine.cs` — port of `hash.c`)

The engine is the dispatcher: it decides *which* algorithm runs for a given
console and file, and provides the shared hashing primitives (whole-file,
buffered, m3u, zip pre-load).

## Dispatch tables

There are two switches, mirroring `rc_hash_from_file` and
`rc_hash_from_buffer`:

- **File dispatch** (`GenerateFromFile` → `FromFile`) — the primary path used
  by the CLI. Handles `.m3u` playlists for the playlist-capable consoles,
  `.zip` pre-loading and `.chd`/`.rvz` reader selection (CLI level), and
  routes every `RC_CONSOLE_*` to its handler.
- **Buffer dispatch** (`FromBuffer`) — used by the CLI zip pre-load path and
  by `RcHash.GenerateFromBuffer`. Whole-file consoles hash the buffer
  directly; algorithm consoles re-run their header detection on the buffer.

!!! note "Arcade routing (12.4.0)"
    `RC_CONSOLE_ARCADE` is special: `.neo` files (Geolith Neo Geo carts)
    contain actual ROM data and are **content-hashed**
    (`RcHashNeogeoCart`); everything else (`.zip`/`.7z` romsets) is
    **filename-hashed** (`RcHashArcade`). The buffer dispatch always uses
    the content hash.

## Whole-file and buffered hashing

- `WholeFile` — streams the file into MD5 in 64 KiB chunks, capped at
  **`MAX_BUFFER_SIZE = 64 MiB`** (the C's `MAX_BUFFER_SIZE`). Verbose output
  distinguishes capped vs. full hashing, byte-identical to the C.
- `BufferedFile` — reads ≤ 64 MiB into memory, then re-dispatches to the
  buffer path (used by the cartridge consoles that need random access).

## m3u playlists

`GenerateFromPlaylist` parses `.m3u` files with the C's exact semantics:

- lines starting with `#` are comments; blank lines skipped;
- CRLF/LF handled; paths resolved **relative to the playlist's directory**;
- the first entry is hashed with the same console;
- extension matching and error text match `hash.c` byte-for-byte.

## The iterator and the extension table

`RcHashIterator` (port of `rc_hash_iterator_t`) carries the path, optional
buffer, and the filereader/cdreader callbacks. `HashIterator` provides:

- `InitializeIterator(path/buffer)` + `Iterate()` — the `?` auto-detect
  mode: each console's handler is tried in the C's exact table order until
  one succeeds;
- the **extension→console table** (`ExtHandlers`, mirror of the C's
  `rc_hash_ext_handlers` bsearch table) — e.g. `nes`→Nintendo, `neo`→Arcade,
  `sms`→Master System, `cue`/`gdi`/`chd`→disc readers, `rvz`→GameCube/Wii
  (RVZSharp path). Table order is sorted for binary search, exactly like the
  C.

## Callbacks

`Callbacks.cs` mirrors the `rc_hash_callbacks_t` struct: error/verbose
message callbacks, filereader (open/seek/tell/read/close), cdreader
(open_track/read_sector/close_track/first_track_sector/open_track_iterator)
and the 3DS key funcs. Every hash run starts from `HashEngine.ResetIterator`
(the port of `rc_hash_reset_iterator`), which merges the **global**
callbacks (verbose/error messages, custom filereader, cdreader, 3DS key
funcs) into the iterator's callback bag — **including the 12.4.0 bugfix**
(the 12.2.1 original assigned `error_message` into `verbose_message`; the
port inherited the bug and fixed it in Part II).

## Key constants

| Constant | Value | Where |
|---|---|---|
| `MAX_BUFFER_SIZE` | `64 * 1024 * 1024` | whole-file/buffered/`.neo`/disc-file caps |
| `RC_CONSOLE_MAX` | `90` | CLI `?` handling |
| `RC_HASH_CDTRACK_FIRST_DATA` | `-1` | track selection |
| `RC_HASH_CDTRACK_LAST` | `-2` | track selection |
| `RC_HASH_CDTRACK_LARGEST` | `-3` | track selection |
| `RC_HASH_CDTRACK_FIRST_OF_SECOND_SESSION` | `-4` | Jaguar CD |

## Console coverage

The dispatch covers every console in the 1.8.3/12.2.1 table (81 entries in
the CLI). Consoles without a dedicated algorithm (GB, GBA, GG, SMS, MD, 2600,
WS, NGP, …) hash whole-file. See the [console table](../reference/console-table.md)
and [hashing semantics](../reference/hashing-semantics.md).
