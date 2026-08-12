# RASharp

A native C# port of the **RAHasher 1.8.3** CLI (`rcheevos` commit
`40d916de00fe757bab40fb4db41a7912193a48e3`) that produces **100% identical
hashes** to the original for every supported console. MIT-licensed class
library + CLI, using `CHDSharp` for CHD reading and
`VideoGameFileSystemParser` for the alternative filesystem backend.

See `ConversionPlan.md` for the full porting plan, module-by-module mapping,
and phase status.

## Building

```
dotnet build RASharp.sln -c Release
```

Produces `RASharp.Cli\bin\Release\net10.0-windows\RASharp.exe`.

Requires the .NET 10 SDK. All projects target the portable `net10.0` TFM
(`CHDSharp` and `VideoGameFileSystemParser` 1.2.0 both ship portable libs),
so the same code runs on Windows, Linux, x64, and arm64. NuGet
dependencies: `CHDSharp` 1.2.0 and `VideoGameFileSystemParser` 1.2.0 (both
MIT).

## Publishing

Self-contained single-file executables (no .NET runtime needed on the target):

```
dotnet publish RASharp.Cli -c Release -r win-x64    --self-contained true -p:PublishSingleFile=true -o artifacts/win-x64
dotnet publish RASharp.Cli -c Release -r win-arm64  --self-contained true -p:PublishSingleFile=true -o artifacts/win-arm64
dotnet publish RASharp.Cli -c Release -r linux-x64  --self-contained true -p:PublishSingleFile=true -o artifacts/linux-x64
dotnet publish RASharp.Cli -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -o artifacts/linux-arm64
```

Produces `RASharp.exe` (Windows) / `RASharp` (Linux) in `artifacts\<rid>\`.
(The parity suite's oracle is a Windows PE, so Tier-2 parity runs on Windows;
on Linux the parity cases skip and the ported vectors still run.)

## Usage

Identical to RAHasher 1.8.3:

```
RASharp [-v] [-s systempath] system filepath...
```

- `-v` — verbose messages for debugging
- `-s systempath` — directory with `aes_keys.txt` / `seeddb.bin` (3DS)
- `system` — console key (case-insensitive) or numeric id; `?` auto-detects
  by trying every console
- `filepath` — file(s) to hash; may contain wildcards in the filename; multiple
  files hash each in turn

Examples:

```
RASharp NES game.nes
RASharp PS1 disc.cue
RASharp -s C:\RetroArch\system 3DS game.cia
RASharp ? unknown.bin
RASharp GB *.gb
```

Supported input formats: raw ROMs, `.zip` (pre-loaded ROM / Arduboy FX /
DOSZ), `.m3u` playlists, `.cue/.bin/.iso/.gdi` discs, `.chd` discs, 3DS
`.cia`/`.3ds`/`.3dsx` (keys required via `-s`), and `.neo` Neo Geo carts
(Geolith format, hashed by ROM content). Exit codes: `0` success,
`1` any failure.

> Quirk faithfully reproduced from the original: console keys are only
> accepted for consoles with a non-empty RA group; NULL-group consoles
> (`Oric`, `TI83`, `TIC-80`, `ESCV`, `DOS`, `3DS`, …) must be addressed by
> numeric id — e.g. `RASharp 62 game.cia`. (The C's `find_console_id` falls
> back to `atoi`, so the key `3DS` would silently resolve to console 3!)

## Testing

```
dotnet test RASharp.sln
```

The suite has two tiers:

### Tier 1 — ported rcheevos vectors

All of rcheevos' own `test/rhash/*` vectors ported to xUnit (cartridge,
disc, cdreader, zip, m3u, handler-order), plus synthetic 3DS fixtures with
known key material. Deterministic and offline.

### Tier 2 — parity harness vs. the original binary

`RASharp.Tests\Parity\` runs both executables with identical arguments and
asserts **byte-identical stdout/stderr and equal exit codes** on an 90-case
generated corpus: 29 whole-file console vectors, cartridge algorithms
(NES/FDS/7800/NDS/SCV), all disc consoles (PSX ± homebrew, PS2, PSP, Sega CD,
Saturn, 3DO, Jaguar CD, PCE-CD, PC-FX, Dreamcast, Neo Geo CD, GameCube),
CHD (PSX/PSP/pregap/multi-track), zip (Arduboy FX, DOSZ/Zip64/DOSC, parent
chains), 3DS (all crypto variants + error paths), m3u, and CLI arg modes
(`?` auto-detect, wildcards, multi-file, `-v`, usage, unknown key/flag,
missing file). Every hash is additionally pinned to the ported vector value
where real-file behavior matches the mock semantics.

The corpus is generated in a unique `%TEMP%\rasharp_parity_corpus_<id>` directory from the same deterministic generators the unit vectors use — no ROM files needed.

**Oracle resolution** (in order): `RASHARP_ORACLE` env var →
`References\rcheevos-12.4.0\bin64\RAHasher.exe` (built from rcheevos **12.4.0**,
the current source of truth — Part II of ConversionPlan.md) →
`References\RAHasher-1.8.3\bin64\RAHasher.exe` (pinned 1.8.3 sources) →
`References\RAHasher.exe` (any other 1.8.3 binary). The harness probes the
oracle's capabilities and falls back to numeric console ids / skips `?` cases
for legacy builds that lack them. If no oracle is found, parity tests skip
with a note.

### Real-ROM parity

The harness covers all engines with synthetic discs (XA/MODE1/AUDIO layouts
reproducing real-file conventions). For real dumps, point `RASHARP_ORACLE` at
a 1.8.3 binary and drop ROMs into the corpus — or run both CLIs yourself:

```
RAHasher.exe PS1 game.cue   vs.   RASharp.exe PS1 game.cue
```

3DS parity requires user-supplied `aes_keys.txt`/`seeddb.bin` (not
redistributable); pass them via `-s` on both sides.

## Parity evidence

Phase 8 (Tier 2) status: **90/90 parity cases green; full suite 326/326** —
byte-identical output between `RASharp.exe` and the source-built
RAHasher 1.8.3 on every case, including verbose mode, error paths, and exit
codes. The harness also caught and fixed three real CLI port bugs (arg-count
guard crash, wildcard path construction, usage banner blank line).

Part II (rcheevos 12.4.0 sync): engine evolved to 12.4.0 — `.neo` Neo Geo
cart hashing, `sms`/`neo` iterate mapping, `merge_callbacks` fix, buffer-size
and GDI/zip guards — verified byte-for-byte against a 12.4.0-built oracle
(90/90 parity, 326/326 suite).

## License

MIT — see `LICENSE` and `THIRD-PARTY-NOTICES.md`. The rcheevos engine is
ported 1:1 (MIT); `Program.cs`, `FileUtil.cs`, `Hash3DS.cs`, and
`ChdCdReader.cs` are new implementations written for behavior parity with the
GPL-3.0 RALibretro RAHasher (used as read-only reference, never copied; the
GPL binary and sources live in `References\` only and are not shipped).
