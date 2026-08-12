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

### RASharp extensions: `scan`

`RASharp scan` (not present in RAHasher 1.8.3 — the legacy positional
interface above is unchanged) hashes a whole ROM library with per-file
console auto-detection and emits one manifest row per file:

```
RASharp scan [options] <path>...
  -f, --format <text|csv|json>  output format (default: text)
  -s <systempath>               supplementary files directory (3DS keys)
      --match <db.json>         RetroAchievements database snapshot
                                (RetroAchievements.json); rows whose hash
                                belongs to a game are annotated with it
      --move <dir>              move matched files into <dir>/<console-key>/
                                <filename> (requires --match); existing
                                files are renamed with a (1), (2) suffix
      --dry-run                 preview --move without moving anything
                                (requires --move)
      --no-recursive            do not descend into subdirectories
  -h, --help                    show help
```

Each file is auto-detected the same way the `?` system key works for a
single file. Text rows look like `<hash> <console-key> <path>`; with
`--match`, matched rows append `=> <Title> (ID <id>)` (csv gains
`game_id,game_title` columns, json gains a `games` array). A file that
fails every candidate console gets the `????` marker and a `?` console.
Hidden, system, and reparse-point files are skipped; hidden
subdirectories are skipped too. The manifest goes to stdout, the summary
(`Scanned N file(s): X hashed, Y failed`) to stderr; exit code `0` when
every file hashed, `1` when any failed. Examples:

```
RASharp scan C:\ROMs
RASharp scan --format json C:\ROMs > manifest.json
RASharp scan --format csv --no-recursive "C:\ROMs\NES" > nes.csv
RASharp scan --match RetroAchievements.json C:\ROMs > matched.txt
RASharp scan --match RetroAchievements.json --move "C:\ROMs\Compatible Games" C:\ROMs
RASharp scan --match RetroAchievements.json --move "C:\ROMs\Compatible Games" --dry-run C:\ROMs
```

`--match` accepts the `RetroAchievements.json` snapshot produced by the
`RetroAchievements.DataFetcher` tool (a JSON array of games with a
`Hashes[]` list each). `--move` relocates only the matched files, grouped
by detected console key (`Compatible Games\NES\...`, `GB\...`, `PS1\...`),
never overwriting — colliding names get a `(1)`, `(2)` suffix. `--dry-run`
prints the exact move plan (collision suffixes included) to stderr without
touching any file. Note: `.zip`
files hash by filename (Arcade) during auto-detection, so they rarely
match the database — extract them first.

### RASharp extensions: `consoles`

`RASharp consoles` dumps the console metadata table (id, key, group, name)
that the usage banner shows, in a machine-readable form for scripts:

```
RASharp consoles [--format text|csv|json]
```

```
RASharp consoles                      # the familiar table
RASharp consoles --format csv > consoles.csv
RASharp consoles -f json              # [{"id": 7, "key": "NES", "group": "Nintendo", "name": "NES/Famicom"}, ...]
```

NULL-group consoles (`3DS`, `Oric`, `DOS`, ...) keep a blank `group` in
text/csv and `null` in json, matching how the usage banner marks consoles
"not supported by RA". Exit code `0` on success, `1` on usage errors.

### RASharp extensions: `checkkeys`, `identify`, `fetch-db`

**`RASharp checkkeys [-s <systempath>]`** — validates the 3DS key files in a
system directory (`aes_keys.txt` must carry `slot0x2CKeyX`, `slot0x3DKeyX`,
and at least one `common<slot>=` key; `seeddb.bin` is optional and only
warned about when missing). Uses the same "key present" semantics as the
3DS engine. Exit `0` when the keys are usable, `1` otherwise.

**`RASharp identify <system> <file> [options]`** — hashes a single file
with an explicit console (the same flow as the legacy CLI, including zip
content hashing and 3DS keys — `?` auto-detects) and resolves the hash to
a game with achievements:

```
RASharp identify NES game.nes --db RetroAchievements.json
RASharp identify GB game.zip --db RetroAchievements.json   # zip content hash
RASharp identify ? unknown.bin --db RetroAchievements.json
RASharp identify PS1 disc.cue --user myname --api-key <key>   # live API lookup
```

Without credentials the hash is looked up in a local
`RetroAchievements.json` snapshot (`--db`, default file in the current
directory). With `--user`/`--api-key` (or `RASHARP_RA_USER` /
`RASHARP_RA_API_KEY`) it fetches `API_GetGameList` for the file's console
from retroachievements.org and looks the hash up live — the public API has
no hash-to-game endpoint, so the live path downloads the same game list
the DataFetcher snapshot contains. `-f json` emits machine-readable rows.
Exit `0` when at least one hash resolves to a game, `1` otherwise.

**`RASharp fetch-db <url-or-path> [--out <file>]`** — downloads (or copies)
a RetroAchievements database snapshot, validates it with the same parser
`scan --match` uses, and saves it atomically (temp file + rename, so a
failed download never clobbers a good snapshot). Default output
`RetroAchievements.json` in the current directory:

```
RASharp fetch-db "https://example.com/RetroAchievements.json"
RASharp fetch-db "C:\Downloads\RetroAchievements.json" --out RetroAchievements.json
```

The snapshot must contain at least one game with hashes; a malformed or
empty result is refused. Exit `0` on success, `1` on failure.

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
