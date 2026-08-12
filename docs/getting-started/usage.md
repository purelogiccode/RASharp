# CLI usage

RASharp replicates the RAHasher 1.8.3 command line exactly — same arguments,
same output format, same exit codes.

## Synopsis

```text
RASharp [-v] [-s systempath] system filepath...
```

| Argument | Meaning |
|---|---|
| `-v` | enable verbose messages for debugging |
| `-s systempath` | directory containing `aes_keys.txt` / `seeddb.bin` (3DS only) |
| `system` | console **key** (case-insensitive), **numeric id**, or `?` for auto-detection |
| `filepath` | the game file; wildcards allowed in the filename; multiple files hash each in turn |

## Examples

```bash
# cartridge
RASharp NES game.nes
RASharp 7 game.nes                # same, numeric id

# disc image (cue references the bin)
RASharp PS1 disc.cue
RASharp SAT saturn.cue
RASharp DC game.gdi
RASharp GC game.iso

# CHD
RASharp PS1 game.chd

# 3DS (needs key files in the system dir)
RASharp -s C:\RetroArch\system 3DS game.cia
RASharp -s C:\RetroArch\system 62 game.3ds   # numeric id form

# zip pre-load / arcade
RASharp ARC romset.zip
RASharp ARD game.arduboy
RASharp 26 game.dosz

# Neo Geo cart (Geolith .neo) — content-hashed
RASharp ARC game.neo

# auto-detect by extension
RASharp '?' unknown.bin

# wildcards and multiple files
RASharp GB *.gb
RASharp PS1 game1.cue game2.cue
```

## Console keys and ids

The console table (81 entries) is printed by running RASharp with no
arguments. Keys are matched case-insensitively; any console can also be
addressed by its numeric id (see the [console table](../reference/console-table.md)).

!!! warning "NULL-group consoles"
    Faithfully reproduced from the original: console keys are only accepted
    for consoles with a non-empty RA group. NULL-group consoles (`Oric`,
    `TI83`, `TIC-80`, `ESCV`, `DOS`, `3DS`, …) **must** be addressed by
    numeric id — the C's `find_console_id` falls back to `atoi`, so the key
    `3DS` would silently resolve to console **3 (SNES)** and hash the file as
    a SNES ROM!

## Output

- Single-file mode prints the 32-character hash followed by a newline.
- Failure prints the error to **stderr** (via the error callback) and exits
  with code `1`.
- `?` auto-detect iterates the handler table; on failure it prints
  `????????????????????????????????` and exits `1`.
- Multi-file / wildcard mode prints `hash filename` per file, suppresses
  verbose output, and prints `No matches found` when nothing matched.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | success (at least one hash produced) |
| `1` | any failure (bad args, unknown console, unreadable file, unsupported format, no wildcard matches) |

## Supported formats

| Format | Notes |
|---|---|
| raw ROM files | whole-file or algorithm-specific per console |
| `.zip` | pre-loaded first entry (console ≤ 90), Arduboy FX, DOSZ/DOSC |
| `.m3u` | playlist — first entry is hashed, paths resolved against the playlist |
| `.cue` / `.bin` / `.iso` | disc images (2352/2336/2048 sector layouts auto-detected) |
| `.gdi` | Dreamcast track table |
| `.chd` | via CHDSharp (read-only, V1–V5) |
| `.cia` / `.3ds` / `.3dsx` | 3DS encrypted formats (keys via `-s`) |
| `.neo` | Geolith Neo Geo carts — content-hashed (ROM data after the 4096-byte header) |

## Behavior notes

- **Wildcards** follow the original's Windows semantics: the pattern is
  scanned with `FindFirstFile`-style matching, but the per-file open path is
  built from the backslash-split directory. This reproduces the original
  exactly, including its `dir/*.bin` quirk (see
  [known quirks](../reference/known-quirks.md)).
- **Verbose output** is byte-identical to the original, including the
  `Hashing … (N bytes)` / `Buffering …` / `Generated hash …` lines.
- **64 MiB cap** applies to whole-file and buffered hashes, exactly like
  `MAX_BUFFER_SIZE` in the C engine.
