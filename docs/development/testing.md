# Testing

The test suite is three-tiered. Run everything with:

```bash
dotnet test RASharp.sln -c Release
```

**Current state: 326/326 green (Debug and Release), parity 90/90 vs the
rcheevos 12.4.0-built oracle.**

## Tier 1 — ported rcheevos vectors (offline, deterministic)

Every upstream vector from `test/rhash/` is ported to xUnit with the
expected MD5s copied verbatim:

| Test file | Ports |
|---|---|
| `TestHashRomGeneric.cs` | whole-file console vectors (`test_hash_rom.c`) |
| `TestHashRomCartridge.cs` | NES/FDS/7800/NDS/SCV/arcade/… (`test_hash_rom.c`) |
| `TestHashNeo.cs` | `.neo` vectors + ext-table mapping (12.4.0) |
| `TestHashDisc.cs` | all disc consoles (`test_hash_disc.c`) |
| `TestCdreader.cs` | cue/gdi track-open semantics (`test_cdreader.c`) |
| `TestHashZip.cs` | zip/DOSZ/ArduboyFX vectors (`test_hash_zip.c`) |
| `TestHash.cs` | m3u + handler-table order (`test_hash.c`) |
| `TestHash3Ds.cs` | synthetic 3DS fixtures (own tooling, keyed) |
| `TestChdCdReader.cs` | CHD reading + both-backends agreement |

The generators (`TestDataGen.cs`, `TestDataGenDisc.cs`,
`TestDataGen3ds.cs`) are ports of `test/rhash/data.c` and are deterministic:
`GenerateGenericFile(n)` produces the same bytes as the C's
`generate_generic_file(n)`.

## Tier 2 — parity harness vs. the C oracle

See [The parity harness](../architecture/parity-harness.md). Run just the
parity cases:

```bash
dotnet test --filter FullyQualifiedName~Parity
```

On non-Windows hosts (or without an oracle binary) these skip with a note.

## Tier 3 — real-world spot checks

Cross-check hashes against values published on retroachievements.org game
pages and the
[game-identification docs](https://docs.retroachievements.org/developer-docs/game-identification.html).
This tier needs real ROMs/ISOs (user-supplied; not part of the repo). The
harness supports an external corpus — point `RASHARP_ORACLE` at a C-built
binary and run both CLIs over any file:

```bash
RAHasher.exe PS1 game.cue   vs.   RASharp.exe PS1 game.cue
```

## Filters

| Purpose | Command |
|---|---|
| parity only | `--filter FullyQualifiedName~Parity` |
| one module | `--filter FullyQualifiedName~TestHashDisc` |
| one test | `--filter "FullyQualifiedName~TestHashNes"` |
| everything | `dotnet test RASharp.sln` |

## Corpus

The parity corpus is generated into a unique
`%TEMP%\rasharp_parity_corpus_<id>` directory per run: whole-file files,
cartridge images, XA/MODE1/AUDIO disc images with real cues, vendored CHDs
(`RASharp.Tests/TestData/*.chd`), mock-built zips, synthetic 3DS keys +
fixtures, m3u playlists, and wildcard directories. Nothing is downloaded and
nothing user-specific is required.

## Full verification

```bash
dotnet build RASharp.sln -c Release     # must be warning-free (TreatWarningsAsErrors)
dotnet test  RASharp.sln -c Release     # 326/326
dotnet test  RASharp.sln -c Debug       # 326/326
```
