# RetroAchievementsSharp

[![NuGet](https://img.shields.io/nuget/v/RetroAchievementsSharp)](https://www.nuget.org/packages/RetroAchievementsSharp)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4)](https://dotnet.microsoft.com/download)
[![License: GPL-2.0-or-later](https://img.shields.io/badge/license-GPL--2.0--or--later-blue.svg)](LICENSE)
[![Repo](https://img.shields.io/badge/github-purelogiccode%2FRetroAchievementsSharp-181717?logo=github)](https://github.com/purelogiccode/RetroAchievementsSharp)

A native C# port of the **RAHasher 1.8.3** hashing engine (`rcheevos`
`40d916d` → 12.4.0) that produces **100% identical hashes** to the original
for every supported console — the same hashes the
[RetroAchievements](https://retroachievements.org) website and its clients
use to identify ROMs and disc images.

- **Library** — `RetroAchievementsSharp` ([NuGet](https://www.nuget.org/packages/RetroAchievementsSharp)),
  targets `net8.0`, `net9.0`, and `net10.0`; GPL-2.0-or-later.
- **CLI** — `RetroAchievementsSharp.Cli.exe`, byte-identical in behavior to `RAHasher 1.8.3`,
  plus convenience subcommands (`scan`, `identify`, `consoles`, `checkkeys`,
  `fetch-db`).
- **Verified** — 415/415 fast tests + 172/172 slow (parity, real-ROM, RVZ,
  published-DB) green on **each** supported TFM, including a parity harness
  against source-built C oracles, RVZ-vs-ISO equality on real GameCube/Wii
  discs, and spot checks against the published RetroAchievements game
  database.

## Installation

```
dotnet add package RetroAchievementsSharp
```

Works with the .NET 8, 9, and 10 SDKs/runtimes on Windows, Linux, macOS,
x64 and arm64.

## Quick start

```csharp
using RetroAchievementsSharp;

// Hash a ROM exactly the way RetroAchievements does (60+ consoles; whole-file,
// cartridge, disc, zip, CHD, RVZ, and 3DS algorithms are included):
if (RcHash.GenerateFromFile(out string hash, ConsoleIds.RcConsoleNintendo, "game.nes"))
    Console.WriteLine(hash); // 32 hex chars — matches the published RA hash
```

More examples:

```csharp
// Auto-detect the console by extension (the engine tries every candidate):
var iterator = new RcHashIterator();
HashIterator.InitializeIterator(iterator, "Super Mario (USA).sfc", null, 0);
while (HashIterator.Iterate(out var hash, iterator) != 0)
    Console.WriteLine(hash); // the first hash that matched a console
HashIterator.DestroyIterator(iterator);

// Hash an in-memory buffer (e.g. the first entry of a zip):
if (RcHash.GenerateFromBuffer(out hash, ConsoleIds.RcConsoleMegaDrive, data, data.Length))
    Console.WriteLine(hash);

// GameCube / Wii discs hash RVZ and WIA images live (decode-on-read,
// no rvz→iso conversion) via RVZSharp; CHD via CHDSharp:
if (RcHash.GenerateFromFile(out hash, ConsoleIds.RcConsoleGamecube, "game.rvz"))
    Console.WriteLine(hash);
if (RcHash.GenerateFromFile(out hash, ConsoleIds.RcConsoleWii, "game.chd"))
    Console.WriteLine(hash);

// 3DS .cia/.3ds files need key files before hashing:
Hash3Ds.InitHash3Ds(@"C:\RetroArch\system"); // dir with aes_keys.txt/seeddb.bin
if (RcHash.GenerateFromFile(out hash, ConsoleIds.RcConsoleNintendo3Ds, "game.cia"))
    Console.WriteLine(hash);
```

- `RcHash.GenerateFromFile(out hash, consoleId, path)` — hash a file.
- `RcHash.GenerateFromBuffer(out hash, consoleId, buffer, bufferSize)` —
  hash in-memory data (e.g. the first entry of a zip).
- `?(...)` per-console entry points: `HashEngine.FromFile`, `HashEngine.FromBuffer`,
  `HashRom.RcHashNes`, `HashDisc.RcHashPsx`, and the iterate API
  `HashIterator`/`RcHashIterator` for auto-detection (`?` mode).
- 3DS `.cia`/`.3ds` files need key files: call `Hash3Ds.InitHash3Ds(systemDir)`
  with a directory containing `aes_keys.txt` (and optionally `seeddb.bin`).
- CHD discs are supported out of the box via
  [CHDSharp](https://www.nuget.org/packages/CHDSharp).
- GameCube/Wii `​.rvz`/`.wia` discs are hashed live (decode-on-read, no
  conversion) via [RVZSharp](https://www.nuget.org/packages/RVZSharp).

Full API reference and docs: <https://purelogiccode.github.io/RetroAchievementsSharp/>.
For exact engine behavior (64 MiB cap, header-stripping rules, track
selection, verbose messages), see the [documentation](docs/index.md) and
[known quirks](docs/reference/known-quirks.md).

## Supported formats

Raw ROMs (all cartridge consoles), `.zip` (pre-loaded ROM, Arduboy FX,
DOSZ/Zip64/DOSC), `.m3u` playlists, discs (`.cue`/`.bin`/`.iso`/`.gdi`/`.chd`,
GameCube/Wii `.rvz`/`.wia`),
3DS `.cia`/`.3ds`/`.3dsx` (keys via `-s` / `Hash3Ds.InitHash3Ds`), and
Neo Geo `.neo` carts. Console list: NES/Famicom, SNES/SFC, N64, GB/GBC/GBA,
Master System, Mega Drive/Genesis, Game Gear, 32X, SG-1000, PCE/TG-16,
PCE-CD, Saturn, Sega CD, Dreamcast, PS1, PS2, PSP, 3DO, PC-FX, Jaguar(+CD),
Neo Geo Pocket(+Color), Neo Geo CD, NDS/DSi/3DS — the full rcheevos console
table (~59 consoles), including classic microcomputers.

## CLI usage

Identical to RAHasher 1.8.3:

```
RetroAchievementsSharp [-v] [-s systempath] system filepath...
```

- `-v` — verbose messages for debugging
- `-s systempath` — directory with `aes_keys.txt` / `seeddb.bin` (3DS)
- `system` — console key (case-insensitive) or numeric id; `?` auto-detects
  by trying every console
- `filepath` — file(s) to hash; may contain wildcards in the filename; multiple
  files hash each in turn

Examples:

```
RetroAchievementsSharp NES game.nes
RetroAchievementsSharp PS1 disc.cue
RetroAchievementsSharp -s C:\RetroArch\system 3DS game.cia
```

Supported input formats: raw ROMs, `.zip` (pre-loaded ROM / Arduboy FX /
DOSZ), `.m3u` playlists, `.cue/.bin/.iso/.gdi` discs, `.chd` discs, 3DS
`.cia`/`.3ds`/`.3dsx` (keys required via `-s`), and `.neo` Neo Geo carts
(Geolith format, hashed by ROM content). Exit codes: `0` success,
`1` any failure.

> Quirk faithfully reproduced from the original: console keys are only
> accepted for consoles with a non-empty RA group; NULL-group consoles
> (`Oric`, `TI83`, `TIC-80`, `ESCV`, `DOS`, `3DS`, …) must be addressed by
> numeric id — e.g. `RetroAchievementsSharp 62 game.cia`. (The C's `find_console_id` falls
> back to `atoi`, so the key `3DS` would silently resolve to console 3!)

### `RetroAchievementsSharp scan` — hash a whole ROM library

(not present in RAHasher 1.8.3 — the legacy positional interface above is
unchanged) hashes each file with per-file console auto-detection and emits
one manifest row per file:

```
RetroAchievementsSharp scan [options] <path>...
  -f, --format <text|csv|json>  output format (default: text)
  -s <systempath>               supplementary files directory (3DS keys)
      --match <db.json>         RetroAchievements database snapshot
      --move <dir>              move matched files into <dir>/<console-key>/
      --dry-run                 preview --move without moving anything
      --no-recursive            do not descend into subdirectories
  -h, --help                    show help
```

Each file is auto-detected the same way the `?` system key works for a
single file; matched rows append `=> <Title> (ID <id>)`. The manifest goes
to stdout, the summary (`Scanned N file(s): X hashed, Y failed`) to stderr;
exit code `0` when every file hashed, `1` when any failed.

### Other subcommands

- `RetroAchievementsSharp consoles [--format text|csv|json]` — dump the console table.
- `RetroAchievementsSharp checkkeys [-s <systempath>]` — validate 3DS key files.
- `RetroAchievementsSharp identify <system> <file> [--db <RetroAchievements.json> | --user <u> --api-key <k>]`
  — hash one file and resolve it to a game with achievements (local
  snapshot or live API).
- `RetroAchievementsSharp fetch-db <url-or-path> [--out <file>]` — download a database
  snapshot, validate it, save atomically.

Full details for every subcommand: [docs/getting-started/usage.md](docs/getting-started/usage.md).

## Building

```sh
dotnet build RetroAchievementsSharp.sln -c Release
```

Requires any .NET 8+ SDK. All three projects multi-target `net8.0;net9.0;net10.0`
(CHDSharp, VideoGameFileSystemParser 1.2.0 and Serilog 4.4.0 all ship
portable libs for these TFMs). Build a single target with
`-f net8.0` (faster).

## Publishing the CLI

Self-contained single-file executables (no runtime needed on the target):

```sh
dotnet publish RetroAchievementsSharp.Cli -c Release -r win-x64    --self-contained true -p:PublishSingleFile=true -o artifacts/win-x64
dotnet publish RetroAchievementsSharp.Cli -c Release -r linux-x64  --self-contained true -p:PublishSingleFile=true -o artifacts/linux-x64
```

Produces `RetroAchievementsSharp.Cli.exe` (Windows) / `RetroAchievementsSharp` (Linux) in `artifacts\<rid>\`.
(The parity suite's oracle is a Windows PE, so Tier-2 parity runs on Windows;
on Linux the parity cases skip and the ported vectors still run.)

## Packaging the NuGet library

```sh
dotnet pack RetroAchievementsSharp -c Release
# publishes RetroAchievementsSharp.1.0.0.nupkg + .snupkg (default: bin/Release); make artifacts/:
dotnet pack RetroAchievementsSharp -c Release -o artifacts
```

The package includes the net8/9/10 assemblies, XML docs, the GPL-2.0-or-later
license and third-party notices, SourceLink/symbols, and runs NuGet package
validation on every pack. Publishing to NuGet.org:

```bash
dotnet nuget push artifacts/RetroAchievementsSharp.1.0.0.nupkg --api-key <key> --source https://api.nuget.org/v3/index.json
```

See [publishing.md](docs/getting-started/publishing.md) for details.

## Testing

Tests are split into two projects:

```bash
dotnet test RetroAchievementsSharp.sln -c Release          # fast suite, all TFMs (net8, net9, net10)
dotnet test RetroAchievementsSharp.sln -f net10.0          # fast suite, one TFM
dotnet test RetroAchievementsSharp.Slow.Tests -c Release   # slow suite (manual — not in the sln)
```

The **fast suite** (`RetroAchievementsSharp.Tests`, in the solution — 415 tests, seconds)
runs every ported rcheevos `test/rhash` vector (cartridge, disc, cdreader,
zip, m3u, handler order), synthetic 3DS/CHD fixtures, and CLI/engine unit
tests. The **slow suite** (`RetroAchievementsSharp.Slow.Tests`, kept out of the solution —
172 tests, minutes) is run manually and covers:

- **Tier-2 parity harness** vs. a source-built C oracle (rcheevos 12.4.0,
  falling back to the pinned 1.8.3 build): 90 generated cases + all CLI arg
  modes; stdout/stderr + exit codes byte-identical.
- **RVZ validation** — real GameCube/Wii RVZ images hashed live through
  RVZSharp must equal the DolphinTool-converted ISO hash (6 files;
  skips when the libraries or DolphinTool are absent).
- **Real-ROM parity** — first files of 60 user library directories vs. the
  pinned 1.8.3 binary (skipped-with-note when the libraries or oracle are
  absent).
- **Published-hash spot checks** — real ROM samples hashed and looked up in
  a RetroAchievements game-database snapshot, asserting official matches per
  library (skips when the snapshot isn't present).

See [parity-evidence.md](docs/reference/parity-evidence.md) for the coverage
table and [known-quirks.md](docs/reference/known-quirks.md) for the
unsupported-format behavior (ROM encoding not part of rcheevos).

## Parity evidence

Current: **415/415 fast + 172/172 slow green on net8.0, net9.0, net10.0** —
byte-identical CLI output between `RetroAchievementsSharp.Cli.exe` and the C oracles,
including verbose mode, error paths, and exit codes; RVZ-vs-ISO equality
6/6 (GameCube and Wii); real-ROM parity 61/61; published-hash spot-check
15/15 library/console pairs. The harness has caught and fixed real port bugs
(arg-count guard crash, wildcard path construction, usage banner blank line,
silent `merge_callbacks` bug) — parity is never "accepted" as a difference;
it's asserted.

## Documentation

Full docs (mkdocs site): [docs](docs/index.md) — architecture, engine
deep-dives, testing, known quirks, and the release-sync playbook for
absorbing future rcheevos releases.

## License

GPL-2.0-or-later — see `LICENSE` and `THIRD-PARTY-NOTICES.md`. Copyright (c)
2026 Peterson Fernandes and Pure Logic Code. The rcheevos engine is ported
1:1 (MIT, credited in the notices); RVZ/WIA hashing links RVZSharp which is
GPL-2.0-or-later (Dolphin-derived). `Program.cs`, `FileUtil.cs`, `Hash3DS.cs`,
and `ChdCdReader.cs` are new implementations written for behavior parity with
the GPL-3.0 RALibretro RAHasher (used as read-only reference, never copied;
the GPL binary and sources live in `References\` only and are not shipped).

## Changelog

See [CHANGELOG.md](CHANGELOG.md).