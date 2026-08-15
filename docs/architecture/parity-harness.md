# The parity harness (`RetroAchievementsSharp.Tests/Parity/`)

The Tier-2 harness is the project's core proof: it runs **both executables**
(the ported CLI and a C-built oracle) with identical arguments and requires
**byte-identical stdout/stderr and equal exit codes** for every case.

## Components

| File | Role |
|---|---|
| `ParityHarness.cs` | process runner (raw-byte capture, 180 s timeout), oracle/CLI discovery, oracle capability probes |
| `TestParity.cs` | the 90-case corpus: generator code, `ParityCase` records, the `[Theory]` runner |

## How a case runs

1. The corpus is built once per test run into a unique
   `%TEMP%\rasharp_parity_corpus_<id>` directory (synthetic fixtures from
   the same deterministic generators the unit vectors use — **no ROM files
   needed**).
2. For each `ParityCase`, both executables are spawned with identical
   arguments (`ProcessStartInfo.ArgumentList` — no shell quoting) and the
   same working directory.
3. Assertions:
   - stdout byte-identical, stderr byte-identical, exit codes equal;
   - `ExpectSuccess` — exit code 0 required where the vector proves success;
   - `ExpectedHash` — the trimmed stdout equals the ported vector value
     (pins both binaries to upstream truth).
4. Usage/name differences are normalized (`RAHasher` → `RetroAchievementsSharp`) only for
   the usage cases; everything else compares raw bytes (including `\r\n`).

## Oracle resolution and probes

The oracle is resolved in order:

1. `RASHARP_ORACLE` environment variable;
2. `References\rcheevos-12.4.0\bin64\RAHasher.exe` (built from rcheevos
   **12.4.0** — the Part II source of truth);
3. `References\RAHasher-1.8.3\bin64\RAHasher.exe` (pinned 1.8.3 sources);
4. `References\RAHasher.exe` (any other 1.8.3 binary the user provides).

The harness probes the oracle once (`nes.nes`): if the build doesn't accept
console keys it falls back to numeric ids; if it lacks `?` mode those cases
skip. No oracle on a Windows host (or any non-Windows host — the oracles are
Windows PEs) ⇒ parity cases skip with a note; the ported vectors still run.

## The 90-case corpus

| Group | Cases | Coverage |
|---|---|---|
| `whole/*` | 29 | whole-file consoles, sizes/ids from the upstream vector table |
| `cart/*` | 9 | NES, FDS, 7800, NDS, SCV, SNES, N64, Lynx, `.neo` (valid/variant/bad-magic) |
| `disc/*` | 17 | PSX ± homebrew, PS2, PSP, Sega CD, Saturn, 3DO, Jaguar CD, PCE-CD, PC-FX, Dreamcast (gdi), Neo Geo CD, GameCube, malformed GDI ×2 |
| `chd/*` | 4 | psx/psp/pregap/multi-track (vendored fixtures) |
| `zip/*` | 7 | Arduboy FX, DOSZ/Zip64/DOSC, parent chains |
| `3ds/*` | 8 | all crypto variants + error paths |
| `m3u/*` | 1 | MD playlist |
| `args/*` | 11 | `?` iterate ×4, wildcards ×2, multi-file, `-v`, usage, unknown key/flag, missing file |

Synthetic disc fixtures reproduce the real-file conventions the C's
cdreader depends on:

- **XA layout** for MODE2/2352 (data at +24, real MSF: LBA 0 = `00:02:00`);
- **MODE1/2048** cues for ISO9660 content;
- **AUDIO tracks** are read headerless (raw 2352 stream), so the bin is the
  generator output stream-chunked to 2352 bytes;
- empty sibling tracks so the cue's `file_first_sector` math stays at 0.

## Adding a case

1. Write the fixture in `BuildCorpus` (or reuse a generator).
2. Add a `ParityCase` in `BuildCases` — `Add(...)` for a success with a
   pinned hash, `ExpectSuccess: false` for error parity.
3. Run the slow suite: `dotnet test RetroAchievementsSharp.Slow.Tests --filter FullyQualifiedName~TestParity`.
4. Any mismatch is a **port bug** — investigate, never "accept" a
   difference.
