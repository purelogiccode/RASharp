# The CLI (`Program.cs`, `Consoles.cs`)

`Program.cs` is a **new implementation** written to match the observable
behavior of `RAHasher.cpp` (GPL-3.0, used as reference only — no text
copied). `Consoles.cs` contains the factual console metadata table (81
entries: id, key, group, name) and is safe to reuse as data.

## Argument processing

```
RetroAchievementsSharp [-v] [-s systempath] system filepath...
```

1. Flags loop: `-v` enables verbose, `-s` takes the next argument as the
   3DS system directory. *(Hardened: `-s` at end-of-args prints usage and
   exits 1 — the original segfaults by reading past `argv`.)*
2. Arg-count guard: `argi + 2 > args.Length` → usage + exit 1 (the C's
   `argi + 2 > argc`; the port originally had an off-by-one that crashed —
   caught by the parity harness).
3. Console resolution:
   - `?` → `RC_CONSOLE_MAX + 1` (iterate mode);
   - key lookup (case-insensitive, **group != NULL only**) → id;
   - else `atoi` fallback (so `"3DS"` becomes **3**, and `"0"`/unknown → 0
     → usage + exit 1). See [known quirks](../reference/known-quirks.md).
4. Multi-file / wildcard mode detection; verbose is disabled for
   multi-file, exactly like the C.

## Wildcards

Windows `FindFirstFile`-style semantics, reproduced exactly:

- the pattern is scanned with the full pattern (forward slashes accepted);
- the per-file open path is built from the **backslash-split** directory —
  so `dir/*.bin` finds `dir\a.bin` but opens `.\a.bin` (the C's quirk);
- directories named like the pattern are matched by the C but skipped here
  (documented edge case).

## Zip pre-load

`.zip` + console ≤ 90 → the first entry is extracted and hashed via
`GenerateFromBuffer` — exactly `util::loadZippedFile` +
`rc_hash_generate_from_buffer` semantics (the C uses the buffer API, not the
file path, for zips).

## Output and exit codes

| Mode | Output |
|---|---|
| single file, success | `hash\n` |
| single file, failure | error on **stderr**, `\n` on stdout, exit 1 |
| iterate (`?`), failure | `????????????????????????????????` then exit 1 |
| multi-file / wildcard | `hash filename\n` per file; `No matches found` if none |
| usage / bad args | full console table + usage, exit 1 |

The usage banner matches the C byte-for-byte (modulo the executable name):
`RetroAchievementsSharp 1.8.3` + `====================` + usage + the `ID Key Group Name`
table with blank lines between groups.

## Console table quirks

- NULL-group consoles print with an empty group column.
- `HIDE_UNSUPPORTED_CONSOLES` is not defined in the build, so all rows are
  shown (matching the reference build).
- The table data comes from `Consoles.cs`, which mirrors
  `RAHasher.cpp`'s `CONSOLES[]` exactly.

## Unicode-safe file I/O

`FileUtil.cs` provides the unicode-safe open/read used by the engine's
default filereader (the C registers a custom filereader for the same reason
on Windows) plus `FullPath`/`Extension`/`Directory`/
`FileNameWithExtension` helpers mirroring the used subset of `Util.cpp`.
