# Changelog

All notable changes to **RASharp** are documented here (keep-a-changelog format).

The project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
For the port history and the evolution plan, see `ConversionPlan.md`.

## [Unreleased]

### Added
- NuGet packaging for `RASharp.Core` (net8.0;net9.0;net10.0, SourceLink, XML
  docs, symbols, package validation), package icon, and CHANGELOG.md.

### Changed
- All projects multi-target `net8.0;net9.0;net10.0` instead of `net10.0`
  only (`Convert.ToHexStringLower` and `System.Threading.Lock` are gated on
  `NET9_0_OR_GREATER` for net8 compatibility).
- Parity harness prefers the CLI built for the test assembly's own TFM.

## [1.0.0] - 2026-08-15

### Added
- Complete native C# port of the RAHasher 1.8.3 / rcheevos 12.4.0 hashing
  engine: cartridge (NES/FDS, SNES, N64, NDS/DSi, PCE, SCV, 7800, Lynx,
  Arduboy, Neo Geo `.neo`, Arcade), disc (PSX, PS2, PSP, Saturn, Sega CD,
  PCE-CD, PC-FX, 3DO, Jaguar CD, Neo Geo CD, Dreamcast, GameCube, Wii), CHD,
  zip (Arduboy FX, DOSZ/DOSC), m3u, and 3DS (CIA/NCCH/3DSX with
  `aes_keys.txt`/`seeddb.bin`).
- CLI with byte-identical behavior to RAHasher 1.8.3, plus the `scan`,
  `consoles`, `checkkeys`, `identify`, and `fetch-db` subcommands.
- Test suite: all ported rcheevos vectors, synthetic 3DS fixtures, a parity
  harness vs. source-built C oracles, and real-ROM parity — **581/581 green
  on net8.0, net9.0, and net10.0**.

### Fixed
- Three real CLI parity bugs caught by the harness (arg-count guard crash,
  wildcard path construction, usage banner blank line), and the inherited
  12.2.1 `merge_callbacks` bug synced to 12.4.0.

---

[1.0.0]: https://github.com/purelogiccode/RASharp/releases/tag/v1.0.0