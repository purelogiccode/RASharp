# Installation

## Prerequisites

- **.NET 8 SDK or newer** — all three projects multi-target the portable
  `net8.0;net9.0;net10.0` TFMs (any one SDK ≥ 8.0 builds all of them;
  .NET 10 SDK is required only to test the `net10.0` target). Install from
  <https://dotnet.microsoft.com/download> or your package manager
  (`apt install dotnet-sdk-10.0`, `winget install Microsoft.DotNet.SDK.10`, …).
- **No native dependencies** — the engine is 100 % managed. `CHDSharp` and
  `VideoGameFileSystemParser` are pure managed NuGet packages.
- **Windows, Linux, macOS** all work for building; the CLI publishes for
  Windows x64/arm64 and Linux x64/arm64 (see [Publishing](publishing.md)).
- The **parity test suite additionally needs a C-built oracle binary on
  Windows** — see [Building the oracles](../development/oracles.md). Without
  one, the Tier-2 parity cases skip (the ported vectors still run).

`RASharp` is also available as a **NuGet package**
(`dotnet add package RASharp`) for use from any net8.0+ application —
see [Packaging the library](publishing.md#packaging-the-library).

## Clone & build

```bash
git clone https://github.com/purelogiccode/RASharp.git
cd RASharp
dotnet build RASharp.sln -c Release
```

The solution contains three projects (plus a manual-only slow test project
kept out of the solution):

| Project | Kind | Purpose |
|---|---|---|
| `RASharp` | class library | the hashing engine (public API mirror of `include/rc_hash.h`) |
| `RASharp.Cli` | console app | the RAHasher-compatible command line (`RASharp.Cli.exe`) |
| `RASharp.Tests` | xUnit (fast) | ported rcheevos vectors + engine/CLI unit tests — in the solution |
| `RASharp.Slow.Tests` | xUnit (slow) | parity harness vs. C oracles, real-ROM, RVZ, published-DB — run manually, not in the solution |

NuGet dependencies:

| Package | Version | License | Role |
|---|---|---|---|
| `CHDSharp` | 1.2.0 | MIT | CHD V1–V5 reading (`ChdFile`, `Tracks`, metadata) |
| `RVZSharp` | 1.0.0 | GPL-2.0-or-later | GameCube/Wii RVZ/WIA live hashing (`RvzFilereader`) |
| `VideoGameFileSystemParser` | 1.2.0 | MIT | alternative ISO9660/UDF backend behind `FileSystemResolver` |
| `Serilog` | 4.4.0 | Apache-2.0 | logging |

## Run the test suite

```bash
dotnet test RASharp.sln -c Release            # fast suite
dotnet test RASharp.Slow.Tests -c Release     # slow suite (parity; manual)
```

Expected result: **415 passed, 0 failed per TFM on the fast suite**
(net8.0, net9.0, net10.0), **172 passed on the slow suite**. The suites
have several tiers — see [Testing](../development/testing.md) for details
and how to filter them.

## Verify the build

```bash
RASharp.Cli/bin/Release/net10.0/RASharp.Cli.exe GB <any-file>
```

prints the 32-character MD5 of the file (the same hash RAHasher 1.8.3
prints). No-args prints the console table and usage:

```bash
RASharp.Cli/bin/Release/net10.0/RASharp.Cli.exe
```

## Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `error NETSDK1005: Assets file ... not found` | run `dotnet restore` once |
| parity tests report `SKIPPED` | no usable oracle found on a Windows host — build one (see [oracles](../development/oracles.md)) or set `RASHARP_ORACLE` |
| build fails on a warning | `TreatWarningsAsErrors` is on by design; fix the warning rather than suppressing it |
| `RASharp.Cli.exe` not found under `bin/Release/net10.0-windows` | the projects target portable `net8.0;net9.0;net10.0` — look under `bin/Release/<tfm>/` (any of the three works) |
