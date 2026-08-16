# Release Notes — RetroAchievementsSharp 1.0.0

**A native C# port of the RAHasher 1.8.3 hashing engine (rcheevos 12.4.0)
that produces 100% identical RetroAchievements hashes** — the same hashes
the [RetroAchievements](https://retroachievements.org) website and its
clients use to identify ROMs and disc images.

## Highlights

- **Byte-exact parity with the C engine** — 415/415 fast tests and 172/172
  slow tests green on each of `net8.0`, `net9.0`, `net10.0`, including a
  Tier-2 parity harness that runs the ported CLI and the original C oracles
  with identical arguments and asserts byte-identical stdout/stderr and
  equal exit codes (90/90 corpus cases against the rcheevos 12.4.0-built
  oracle).
- **`RetroAchievementsSharp` NuGet library** — hash any supported file with
  two lines of code; auto-detection, buffer hashing, disc and 3DS support
  included.
- **`RetroAchievementsSharp.Cli`** — byte-identical to RAHasher 1.8.3, plus
  convenience subcommands (`scan`, `identify`, `consoles`, `checkkeys`,
  `fetch-db`).
- **Live disc hashing** — CHD (via CHDSharp) and GameCube/Wii RVZ/WIA (via
  RVZSharp, decode-on-read, no rvz→iso conversion) with no native
  dependencies.
- **Cross-platform** — Windows x64/arm64 and Linux x64/arm64
  self-contained single-file binaries (no .NET runtime needed on the target).

## Requirements

- **.NET 8, 9, or 10** for the library (older runtimes are **not**
  supported).
- No native dependencies — 100% managed.

## Supported formats

Raw ROMs (all cartridge consoles), `.zip` (pre-loaded ROM, Arduboy FX,
DOSZ/Zip64/DOSC), `.m3u` playlists, discs (`.cue`/`.bin`/`.iso`/`.gdi`/`.chd`,
GameCube/Wii `.rvz`/`.wia`), 3DS `.cia`/`.3ds`/`.3dsx` (keys via `-s` /
`Hash3Ds.InitHash3Ds`), and Neo Geo `.neo` carts.

Console list: NES/Famicom, SNES/SFC, N64, GB/GBC/GBA, Master System, Mega
Drive/Genesis, Game Gear, 32X, SG-1000, PCE/TG-16, PCE-CD, Saturn, Sega CD,
Dreamcast, PS1, PS2, PSP, 3DO, PC-FX, Jaguar(+CD), Neo Geo Pocket(+Color),
Neo Geo CD, NDS/DSi/3DS — the full rcheevos console table (~59 consoles),
including classic microcomputers.

## Downloads

| Asset | Platform |
|---|---|
| `RetroAchievementsSharp.1.0.0.nupkg` | NuGet library (net8.0 / net9.0 / net10.0) — `dotnet add package RetroAchievementsSharp` |
| `retroachievementssharp_v1.0.0_win-x64.zip` | Windows x64 (self-contained, single-file) |
| `retroachievementssharp_v1.0.0_win-arm64.zip` | Windows arm64 (self-contained, single-file) |
| `retroachievementssharp_v1.0.0_linux-x64.zip` | Linux x64 (self-contained, single-file) |
| `retroachievementssharp_v1.0.0_linux-arm64.zip` | Linux arm64 (self-contained, single-file) |

Each CLI archive includes `LICENSE`, `THIRD-PARTY-NOTICES.md`, and
`README.md` alongside the binary.

## CLI quick start

```bash
RetroAchievementsSharp NES game.nes
RetroAchievementsSharp PS1 disc.cue
RetroAchievementsSharp -s C:\RetroArch\system 3DS game.cia
```

## Library quick start

```csharp
using RetroAchievementsSharp;

if (RcHash.GenerateFromFile(out string hash, ConsoleIds.RcConsoleNintendo, "game.nes"))
    Console.WriteLine(hash); // 32 hex chars — matches the published RA hash
```

## Credits

- **[LeXofLeviafan](https://github.com/LeXofLeviafan/)** — author of the
  RALibretro RAHasher CLI this project is behaviorally compatible with and
  that we use as the reference oracle in our parity test suite.
- **[RetroAchievements](https://retroachievements.org)** / [rcheevos](https://github.com/RetroAchievements/rcheevos)
  — the hashing engine, ported 1:1 (MIT).

## Links

- Repository: <https://github.com/purelogiccode/RetroAchievementsSharp>
- Docs: <https://purelogiccode.github.io/RetroAchievementsSharp/>
- NuGet: <https://www.nuget.org/packages/RetroAchievementsSharp>
- License: GPL-2.0-or-later (see `LICENSE` and `THIRD-PARTY-NOTICES.md`)

## Changelog

See [CHANGELOG.md](https://github.com/purelogiccode/RetroAchievementsSharp/blob/master/CHANGELOG.md).