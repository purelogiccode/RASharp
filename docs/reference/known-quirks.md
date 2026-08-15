# Known quirks

The port deliberately reproduces the original's behavior — including its
quirks. These are **features for parity**, verified byte-for-byte by the
harness. Each entry says what the original does, what the port does, and
why it stays.

## Faithfully reproduced

### 1. NULL-group console keys resolve via `atoi`
`find_console_id` only matches keys whose console has a non-empty RA group.
Everything else falls back to `atoi` — so `RASharp 3DS file` silently
hashes as **console 3 (SNES)**, and `Oric`/`TI83`/`TIC-80`/`ESCV`/`DOS`
keys print usage + exit 1. Use numeric ids for NULL-group consoles.

### 2. 64 MiB whole-file cap
`MAX_BUFFER_SIZE` truncates whole-file/buffered/`.neo`/disc-file hashes.
Verbose output reports the truncated size — byte-identical to the C.

### 3. Wildcard path construction
On Windows the original scans the **full pattern** (`FindFirstFile` accepts
forward slashes) but builds each open path from the **backslash-split**
directory. Therefore `dir/*.bin` finds `dir\a.bin` yet tries to open
`.\a.bin`. The port reproduces this exactly (the parity case for it
expects both binaries to fail identically).

### 4. `.sms` had no extension mapping (until 12.4.0)
Pre-12.4.0, `.sms` files fell through to the generic fallback; 12.4.0 maps
them to Master System. The port follows 12.4.0 (same hash either way — the
mapping makes console resolution explicit).

### 5. CHD `LARGEST`-track stale offsets
The 1.8.3 `HashCHD.cpp` re-fetches metadata after computing offsets, so the
`LARGEST` track selection can use stale values — both binaries report
`Not a PC-FX CD` on the 3-track test CHD. Reproduced 1:1 (parity case
expects the identical failure).

### 6. CD images need real MSF
The cdreader derives `track_first_sector` from the sector-16 header MSF
(`msf_to_lba − 150`). Images whose sector headers say `00:00:00` produce
`−150` and break absolute-LBA reads. Real dumps encode `00:02:00` for LBA 0;
the corpus fixtures follow the real convention.

### 7. AUDIO tracks are headerless
AUDIO cue tracks read as a raw 2352-byte stream (no header skip) — the
corpus builds Jaguar CD bins accordingly.

### 8. `merge_callbacks` (12.2.1 bug)
The 12.2.1 C assigned `error_message` into `verbose_message`; the port
inherited the bug and **fixed it in Part II** with 12.4.0 (the C# bug was
found by re-auditing, and the fix is covered by the sync).

### 9. Zip entries >= 2 GiB (fixed in the port)
The C extracts a zip entry into a `malloc`'d buffer and can hold multi-GiB
dumps; a C# `byte[]` is capped below 2 GiB, so the original port failed on
2 GiB+ 3DS dumps (`new byte[entry.Length]` throws). Fixed with a disk-backed
fallback (`LoadZippedFileToTemp` + `GenerateHashes`) that hashes the
extracted temp file — same bytes, same hash. Verified against real 2 GiB
3DS dumps in the real-ROM suite.

### 10. RVZ/WIA (supported) vs. WUX/WUD (unsupported)
rcheevos defines no reader for Dolphin's RVZ, but RASharp adds one via
RVZSharp (`RvzFilereader`, GPL-2.0-or-later): GameCube/Wii `.rvz`/`.wia`
files are decoded on read and hashed exactly like the plain ISO — validated
against DolphinTool conversions (6/6). The CLI selects the RVZ reader per
file by extension (`?` auto-detect maps `.rvz` to GameCube + Wii). What the
binaries actually do:

- **RVZ/WIA**: fully supported — `RASharp 16 game.rvz`, `RASharp 19 game.wia`,
  or `RASharp ? game.rvz` hash the decoded disc image (same hash as the
  converted ISO).
- **WUX/WUD (Wii U)**: no reader exists in either engine. `RASharp 20 game.wux`
  hits the console with **no hasher at all** — id 20 (Wii U) exists in the
  console table but has no `FromFile` case
  (`Unsupported console for file hash: 20`).
- **`?` auto-detect for other unknown containers**: an unknown extension
  falls through to the generic whole-file MD5 fallback with a warning
  (`No console mapping specified for wux file extension - trying full file
  hash`). The result is the hash of the compressed container bytes, **not**
  a game hash — do not use it for identification.

The WUX/WUD behavior is byte-identical to the 1.8.3 oracle (verified). The
practical route for such files is conversion (WUX → raw image), then the
existing `iso` handler applies.

## Intentional differences (documented, not bugs)

| Difference | Why |
|---|---|
| `-s` with no value prints usage + exit 1 | the original **segfaults** (reads `argv[argc]`); crashing is not behavior worth replicating |
| directories matching a wildcard are skipped | the original emits a `????` line for them; noted edge case, kept for simplicity |
| Wii/WiiWare malloc-failure paths | managed code has no allocation failures; noted in comments |
| `RC_CCONV` calling convention | ABI detail, no native interop in the port |
| error/verbose callbacks route to stderr/stdout via `Console` | matches the C's `fprintf(stderr, ...)` / `printf(...)` streams |
