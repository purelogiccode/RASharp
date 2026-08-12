# Cartridge hashing (`HashRom.cs` — port of `hash_rom.c`)

`HashRom` implements the cartridge algorithms. Each function is a 1:1
translation; the buffer helpers (`IteratorBuffer`, `UnheaderedIteratorBuffer`)
mirror `rc_hash_iterator_buffer` / `rc_hash_unheadered_iterator_buffer`.

## Algorithms

### Atari 7800 — `RcHash7800`
Whole-file MD5, but if the buffer starts with the `ATARI7800` header
(offset 1, `buffer_size > 128` guard from 12.4.0), the 128-byte header is
stripped first. Verbose: `Ignoring 7800 header`.

### Arcade — `RcHashArcade` / `RcHashNeogeoCart`
- `RcHashArcade`: **filename hash** — MD5 of the file name without
  extension (FBNeo cores are strict about ROM data; the filename is the
  identifier). FBNeo subsystem folders are included in the hash.
- `RcHashNeogeoCart` (12.4.0): **content hash** for Geolith `.neo` carts —
  requires `NEO\1` magic at offset 0, skips the 4096-byte header, and hashes
  the remaining ROM data (64 KiB chunks, 64 MiB cap). Header text fields
  differ between conversion tools, so they must not affect the hash.
  Errors: `Not a valid .neo file`.

### Arduboy — `RcHashArduboy`
`.arduboy` files are zips: `HashZip.RcHashArduboyFx` filters to `.hex` /
`.bin` entries. Also supports raw Intel-HEX text hashing.

### Atari Lynx — `RcHashLynx`
Whole-file MD5 with the 64-byte `LYNX` header stripped
(`buffer_size > 64` guard).

### NES / Famicom Disk System — `RcHashNes`
Strips the 16-byte iNES header (`NES\x1a`) or FDS header (`FDS\x1a`) when
present (`buffer_size > 16` guards), then hashes the ROM. Copier headers are
handled by the same path.

### Nintendo 64 — `RcHashN64`
Hashes the first 1 MiB after a 16-bit byte-swap. Handles the `.v64` /
`.n64` / `.z64` / `.ndd` variants (swap + header strip rules per variant).

### Nintendo DS / DSi — `RcHashNintendoDs`
Header + first-N-KB rules: the ARM9/ARM7/icon blocks, with the SuperCard
512-byte header variant detected by magic.

### PC Engine — `RcHashPce`
Hashes the header + first N bytes of the HuCard image (whole-file MD5 of
the first 256 KiB region, exactly as the C).

### Super Cassette Vision — `RcHashScv`
Whole-file MD5 with the 32-byte `EmuSCV` header stripped (with the 12.4.0
`buffer_size > 32` guard).

### SNES — (whole-file path)
LoROM/HiROM header selection lives in the buffer logic; SNES hashes
whole-file (the header-strip behavior is inherited from the C's
`rc_hash_iterator_buffer` semantics).

## Guards added in 12.4.0 (Part II)

The 12.4.0 tree added `buffer_size > N` guards before header `memcmp`s so a
tiny buffer can never be "header-stripped" into a negative/empty region:

| Console | Guard |
|---|---|
| 7800 | `buffer_size > 128` |
| Lynx | `buffer_size > 64` |
| NES/FDS | `buffer_size > 16` |

These are behavior-preserving for valid ROMs and are covered by unit tests.

## `?`-iterate notes

The extension table maps `.neo` → Arcade (content hash) and `.sms` → Master
System — both additions from 12.4.0. Without the `sms` entry, `.sms` files
fell through to the generic whole-file fallback; the mapping makes the
console resolution explicit (same hash, correct console).
