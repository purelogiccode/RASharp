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

The solution contains three projects:

| Project | Kind | Purpose |
|---|---|---|
| `RASharp` | class library | the hashing engine (public API mirror of `include/rc_hash.h`) |
| `RASharp.Cli` | console app | the RAHasher-compatible command line (`RASharp.Cli.exe`) |
| `RASharp.Tests` | xUnit | ported rcheevos vectors + the Tier-2 parity harness |

NuGet dependencies (both MIT):

| Package | Version | Role |
|---|---|---|
| `CHDSharp` | 1.2.0 | CHD V1–V5 reading (`ChdFile`, `Tracks`, metadata) |
| `VideoGameFileSystemParser` | 1.2.0 | alternative ISO9660/UDF backend behind `FileSystemResolver` |

## Run the test suite

```bash
dotnet test RASharp.sln -c Release
```

Expected result: **581 passed, 0 failed per TFM** (net8.0, net9.0,
net10.0). The suite has three tiers — see [Testing](../development/testing.md)
for details and how to filter them.

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
