# Porting guide

How the C engine becomes C# — the conventions that keep the port faithful,
maintainable, and reviewable.

## Golden rules

1. **Translate 1:1.** Same control flow, same constants, same special
   cases, same error strings. If you are tempted to "improve" behavior,
   stop — parity is the requirement.
2. **Keep the C file open next to the C# file.** Every function carries a
   comment naming its origin, e.g. `// Ported from rcheevos (MIT) —
   src/rhash/hash_rom.c`.
3. **Never copy GPL text.** `RAHasher.cpp`, `Util.cpp`, `Hash3DS.cpp`,
   `HashCHD.cpp` are GPL-3.0 reference-only. Port their *observable
   behavior*; new files carry a
   `// New implementation, behavior parity with RALibretro RAHasher
   (GPL-3.0, used as reference only)` header.
4. **Vectors before merges.** A module is not done until its upstream
   vectors pass; the parity harness is the final arbiter.
5. **Mismatch = bug.** The parity suite never "accepts" a difference — a
   failing case is a defect in the port (or, rarely, a corpus-format issue
   that must be fixed and re-verified against the oracle).

## C → C# mapping

| C idiom | C# idiom in this repo |
|---|---|
| `memcmp(buffer + off, "TEXT", n)` | `MemEquals(buffer, off, "TEXT")` (byte-wise, includes NULs where the C does) |
| `rc_hash_iterator_error(iter, "...")` | `HashEngine.IteratorError(iterator, "...")` |
| `rc_hash_iterator_verbose_formatted(...)` | `HashEngine.IteratorVerboseFormatted(iterator, "{0} ...", ...)` (same text, `{0}` placeholders) |
| `md5_init / md5_append / rc_hash_finalize` | `new HashMd5()` / `md5.Append(...)` / `HashEngine.Finalize(iterator, md5, out hash)` |
| `file_reader->open/seek/tell/read/close` | `HashEngine.FileOpen/FileSeek/FileTell/FileRead/FileClose` |
| `rc_cd_read_sector(...)` | `CdReadSector(iterator, trackHandle, sector, buffer, count)` |
| `sizeof(buffer)` bounds | explicit `len`/`pos` in the port (no pointer arithmetic) |
| `atoi`, `isspace`, `strncasecmp` | local helpers (`Atoi`, `IsSpace`, case-insensitive compares) with the C's exact semantics |
| octal escapes (`"NEO\1"`) | `"NEO\x01"` (C# has no octal escapes) |
| `%u`/`%s` printf | `{0}`/`{1}` with explicit `(uint)` casts to match the C's types |

## Where the traps live

- **64 MiB cap** — `MAX_BUFFER_SIZE` applies in *every* place the C applies
  it (whole-file, buffered, `.neo`, disc file reads). A dedicated test
  covers the > 64 MiB path.
- **Endianness** — N64 byteswap, Jaguar CD byteswap, ISO9660 little-endian
  fields, BCD MSF.
- **Track math** — MSF→LBA (`− 150`), pregap, `file_first_sector`
  accumulation across cue FILE blocks, the sector-16 probe.
- **Callback plumbing** — `MergeCallbacks` (the 12.2.1 bug!), the global
  vs. iterator-local cdreader/filereader duality.
- **Text fidelity** — error and verbose strings are part of the parity
  contract (the harness compares raw stdout/stderr bytes).

## Verifying a new module

1. Port the algorithm 1:1 with an origin header.
2. Port the upstream vectors for it; add the module's tests.
3. Add corpus cases (success + error paths) in `TestParity`.
4. `dotnet test` — all green, then optionally run the parity cases against
   both oracles (`RASHARP_ORACLE`).
5. Update `ConversionPlan.md` and this documentation.

## Review checklist

- [ ] origin header present and accurate;
- [ ] no GPL text copied (behavior only);
- [ ] constants/error strings match the C byte-for-byte;
- [ ] 64 MiB cap applied where the C applies it;
- [ ] vectors green, parity green;
- [ ] warnings-as-errors clean.
