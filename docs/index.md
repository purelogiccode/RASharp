# RASharp

**A native C# port of the RAHasher 1.8.3 CLI that produces 100 % identical
RetroAchievements hashes — now tracking the current rcheevos release
(12.4.0) as the single source of truth.**

RASharp is an MIT-licensed class library + CLI that hashes ROMs and disc
images exactly the way RetroAchievements identifies games: same algorithms,
same constants, same edge cases, byte-for-byte. The engine is a 1:1
translation of the `rc_hash` module of [rcheevos](https://github.com/RetroAchievements/rcheevos),
originally pinned to the RAHasher 1.8.3 submodule commit (`40d916d`,
rcheevos 12.2.1) and since evolved to rcheevos 12.4.0.

## Highlights

- **100 % parity, proven** — a Tier-2 harness runs `RASharp.exe` and the
  original C binaries with identical arguments and requires byte-identical
  stdout/stderr and equal exit codes: **90/90 corpus cases green** against
  the rcheevos 12.4.0-built oracle; **326/326 tests green** (Debug + Release).
- **81 consoles** in the CLI table — cartridge, disc, encrypted (3DS), CHD,
  zip-based (Arduboy FX, DOSZ/DOSC) and `.neo` Neo Geo cart formats.
- **Cross-platform** — portable `net10.0` TFM; publishes self-contained
  single-file executables for Windows x64/arm64 and Linux x64/arm64.
- **Honest engineering** — the port reproduces the original's quirks
  (64 MiB whole-file cap, `atoi` console-key fallback, wildcard path
  construction) *and* inherits its bugfixes (the 12.4.0 `merge_callbacks`
  fix was ported the day the C# port's copy of the bug was found).
- **MIT** — the ported engine is MIT (rcheevos); the GPL-3.0 RAHasher
  reference material is used only as a behavioral reference and is never
  shipped.

## Quick start

```bash
dotnet build RASharp.sln -c Release
dotnet test  RASharp.sln -c Release

RASharp.Cli/bin/Release/net10.0/RASharp.exe NES game.nes
RASharp.Cli/bin/Release/net10.0/RASharp.exe PS1 disc.cue
RASharp.Cli/bin/Release/net10.0/RASharp.exe '?' unknown.bin
```

## Project status

| Part | Scope | Status |
|---|---|---|
| Part I (phases 0–8) | Port of RAHasher 1.8.3 / rcheevos `40d916d` (12.2.1) | ✅ Complete — 82/82 parity vs the source-built 1.8.3 oracle |
| Part II (phases E0–E6) | Evolution to rcheevos 12.4.0 (single source of truth) | ◑ E0–E4 done (.neo, bugfixes, guards, vectors, corpus); E5 real-world spot checks and E6 release-sync playbook open |

## Documentation map

- **Getting Started** — [installation](getting-started/installation.md), [CLI usage](getting-started/usage.md), [publishing builds](getting-started/publishing.md)
- **Architecture** — [overview](architecture/overview.md) and deep dives into the
  [engine](architecture/engine.md), [cartridges](architecture/cartridge.md),
  [disc hashing](architecture/disc.md), [zip](architecture/zip.md),
  [3DS encryption](architecture/encrypted.md), the [CLI](architecture/cli.md)
  and the [parity harness](architecture/parity-harness.md)
- **Development** — [testing](development/testing.md), [building the oracles](development/oracles.md),
  the [porting guide](development/porting-guide.md) and the
  [release-sync playbook](development/sync-rcheevos.md)
- **Reference** — [console table](reference/console-table.md),
  [hashing semantics](reference/hashing-semantics.md), [public API](reference/public-api.md),
  [known quirks](reference/known-quirks.md), [parity evidence](reference/parity-evidence.md)

## Repository layout

```
RASharp.sln                 solution (Core + Cli + Tests)
Directory.Build.props       net10.0, nullable, warnings-as-errors
mkdocs.yml                  this documentation site
docs/                       these pages
RASharp.Core/               the hashing engine (class library)
RASharp.Cli/                the RAHasher-compatible command line
RASharp.Tests/              ported rcheevos vectors + the Tier-2 parity harness
References/                 read-only reference material (never shipped):
                            rcheevos-12.4.0, rcheevos-40d916d, RAHasher-1.8.3,
                            C oracle binaries (GPL, local test oracles only)
```

## License

MIT — see [license.md](license.md) and `THIRD-PARTY-NOTICES.md` in the repo.
