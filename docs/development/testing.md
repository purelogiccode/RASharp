# Testing

The test suite is split into **two xUnit projects**:

| Project | Purpose | Speed | In the solution? |
|---|---|---|---|
| `RetroAchievementsSharp.Tests` | deterministic unit/vector/CLI tests — runs on every change and on every TFM | seconds | **yes** |
| `RetroAchievementsSharp.Slow.Tests` | parity harness vs. C oracles, real-ROM libraries, RVZ/DolphinTool and published-DB spot checks | minutes | **no** (run manually) |

Run the fast suite (default) with:

```bash
dotnet test RetroAchievementsSharp.sln -c Release
```

Run the slow suite explicitly (it is not part of the solution):

```bash
dotnet test RetroAchievementsSharp.Slow.Tests -c Release
```

**Current state: 415/415 fast tests green, 172/172 slow tests green
(Debug + Release), parity 90/90 vs the rcheevos 12.4.0-built oracle,
RVZ-vs-ISO 6/6, 61/61 real-ROM cases vs the pinned 1.8.3 binary, and
published-hash spot checks (see
[Parity evidence](../reference/parity-evidence.md)).**

## RetroAchievementsSharp.Tests — fast, deterministic (in the solution)

Every upstream vector from `test/rhash/` is ported to xUnit with the
expected MD5s copied verbatim, plus CLI and engine unit tests:

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
| `TestScan.cs`, `TestConsoles.cs`, `TestCheckKeys.cs`, … | CLI subcommand dispatch + engine internals |

The generators (`TestDataGen.cs`, `TestDataGenDisc.cs`,
`TestDataGen3ds.cs`) are ports of `test/rhash/data.c` and are deterministic:
`GenerateGenericFile(n)` produces the same bytes as the C's
`generate_generic_file(n)`.

## RetroAchievementsSharp.Slow.Tests — parity and real-world suites (manual)

Kept out of the solution because it needs a C oracle binary, user ROM
libraries, DolphinTool, and/or the RA database snapshot. Skips with a note
when its prerequisites are absent, and references the fast test project for
the shared `ParityHarness` and generators. Run it explicitly:

```bash
dotnet build RetroAchievementsSharp.sln                 # build the fast project first
dotnet test  RetroAchievementsSharp.Slow.Tests -f net10.0   # one TFM: slow suites re-run the same engine, and DolphinTool conversions are serialized
```

!!! note "Run one TFM"
    The slow suite is environment-dependent and takes minutes — run a single
    TFM (`-f net10.0`). Running all three TFMs in parallel triples the time
    with no extra coverage; the RVZ cases serialize their DolphinTool
    conversions via a cross-process mutex, so concurrent runs no longer
    collide on Dolphin's temporary FST file.

| File | What it runs |
|---|---|
| `TestParity.cs` | the generated-corpus parity harness vs. the rcheevos 12.4.0-built oracle (90 cases; `RASHARP_ORACLE` to override) |
| `TestRealRomParity.cs` | first files of 60 user library directories vs. the pinned 1.8.3 binary |
| `TestRvzParity.cs` | real GameCube/Wii RVZ images — live RVZSharp hash must equal the DolphinTool-converted ISO hash (6 cases) |
| `TestPublishedHashMatch.cs` | real ROM samples hashed and looked up in the official RA game-database snapshot |

### Tier 2 — parity harness vs. the C oracle

See [The parity harness](../architecture/parity-harness.md). Run just the
parity cases:

```bash
dotnet test RetroAchievementsSharp.Slow.Tests --filter FullyQualifiedName~TestParity
```

On non-Windows hosts (or without an oracle binary) these skip with a note.

### Real-ROM parity (user libraries)

`TestRealRomParity` hashes the first files of each configured user library —
60 paths across 53 console ids (3DS needs the user keys at the repo root and
uses 5 files), including cartridge, zip, MAME romsets, disk images, and CD
formats (predominantly CHD) — against the pinned
`References\RAHasher-1.8.3\RAHasher.exe` binary, requiring byte-identical
stdout/stderr and exit codes. Cases skip with a note when the library path
or the oracle is absent, so the suite stays green on machines without the
ROM libraries. Full coverage table:
[Parity evidence](../reference/parity-evidence.md).

### RVZ validation (DolphinTool conversion vs live hash)

`TestRvzParity` converts real GameCube/Wii RVZ images to ISO with
`References\DolphinTool.exe`, hashes both the ISO and the RVZ
(RetroAchievementsSharp.Cli), and requires identical hashes — proving that the
decode-on-read RVZSharp path computes the same game hash as the converted
ISO without conversion. 6 cases (3 GameCube + 3 Wii): skips when the
libraries or DolphinTool are absent.

### Published-hash spot checks (official RA database)

`TestPublishedHashMatch` places RetroAchievementsSharp's hashes next to the official
RetroAchievements game database. RA does not publish the database on the
docs site, so the test consumes a snapshot generated by the
`RetroAchievements.DataFetcher` tool (`RetroAchievements.json`,
`ID/Title/ConsoleID/Hashes` schema). For each of 15 cartridge libraries the
first 50 files are hashed with `identify <console> <file> --db <snapshot>`
and the test requires **at least one official-hash match per library**
(misses are expected — the snapshot covers games with sets only). The
snapshot is located via the `RASHARP_RA_DB` environment variable, else the
DataFetcher publish output, else `References\RetroAchievements.json`, and
skips with a note when neither it nor the libraries are present.

```bash
# point the spot check at a fresh snapshot
RASHARP_RA_DB=C:\path\to\RetroAchievements.json dotnet test RetroAchievementsSharp.Slow.Tests --filter FullyQualifiedName~TestPublishedHashMatch
```

## Real-world spot checks (ad-hoc)

The harness also supports an ad-hoc external corpus — point `RASHARP_ORACLE`
at a C-built binary and run both CLIs over any file:

```bash
RAHasher.exe PS1 game.cue   vs.   RetroAchievementsSharp.Cli.exe PS1 game.cue
```

## Filters

| Purpose | Command |
|---|---|
| parity only | `dotnet test RetroAchievementsSharp.Slow.Tests --filter FullyQualifiedName~TestParity` |
| one module (fast) | `dotnet test RetroAchievementsSharp.sln --filter FullyQualifiedName~TestHashDisc` |
| one test (fast) | `dotnet test RetroAchievementsSharp.sln --filter "FullyQualifiedName~TestHashNes"` |
| everything fast | `dotnet test RetroAchievementsSharp.sln` |
| everything slow | `dotnet test RetroAchievementsSharp.Slow.Tests` |

## Corpus

The parity corpus is generated into a unique
`%TEMP%\rasharp_parity_corpus_<id>` directory per run: whole-file files,
cartridge images, XA/MODE1/AUDIO disc images with real cues, vendored CHDs
(`RetroAchievementsSharp./TestData/*.chd`), mock-built zips, synthetic 3DS keys +
fixtures, m3u playlists, and wildcard directories. Nothing is downloaded and
nothing user-specific is required.

## Full verification

```bash
dotnet build RetroAchievementsSharp.sln -c Release            # must be warning-free (TreatWarningsAsErrors)
dotnet test  RetroAchievementsSharp.sln -c Release            # 415/415 fast
dotnet test  RetroAchievementsSharp.sln -c Debug              # 415/415 fast
dotnet test  RetroAchievementsSharp.Slow.Tests -c Release     # 172/172 slow (manual)
```