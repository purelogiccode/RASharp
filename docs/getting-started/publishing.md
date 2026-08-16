# Publishing builds

RetroAchievementsSharp ships two artifacts:

1. **The `RetroAchievementsSharp` NuGet library** (net8.0;net9.0;net10.0) — for
   consumers; see [Packaging the NuGet library](#packaging-the-nuget-library).
2. **Self-contained single-file CLI executables** — no .NET runtime needs to
   be installed on the target machine. The same multi-targeted
   (`net8.0`/`net9.0`/`net10.0`) codebase produces binaries for Windows
   x64/arm64 and Linux x64/arm64.

## The four CLI targets

```bash
dotnet publish RetroAchievementsSharp.Cli -c Release -r win-x64     --self-contained true -p:PublishSingleFile=true -o artifacts/win-x64
dotnet publish RetroAchievementsSharp.Cli -c Release -r win-arm64   --self-contained true -p:PublishSingleFile=true -o artifacts/win-arm64
dotnet publish RetroAchievementsSharp.Cli -c Release -r linux-x64   --self-contained true -p:PublishSingleFile=true -o artifacts/linux-x64
dotnet publish RetroAchievementsSharp.Cli -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -o artifacts/linux-arm64
```

Cross-publishing works from any host: the SDK downloads the matching runtime
pack from NuGet, so e.g. Linux-arm64 can be produced on Windows.

## Output

| RID | File | Notes |
|---|---|---|
| `win-x64` | `artifacts/win-x64/RetroAchievementsSharp.Cli.exe` | PE |
| `win-arm64` | `artifacts/win-arm64/RetroAchievementsSharp.Cli.exe` | PE |
| `linux-x64` | `artifacts/linux-x64/RetroAchievementsSharp` | ELF |
| `linux-arm64` | `artifacts/linux-arm64/RetroAchievementsSharp` | ELF |

## Release archives

For GitHub releases, zip each publish output with a consistent name.
**GPL compliance: each archive must ship `LICENSE`,
`THIRD-PARTY-NOTICES.md`, and `README.md` (the source link) alongside the
binary** — never release a bare executable:

```text
retroachievementssharp_v<version>_<rid>.zip
```

e.g. for version `1.0.0`:

```text
retroachievementssharp_v1.0.0_win-x64.zip
retroachievementssharp_v1.0.0_win-arm64.zip
retroachievementssharp_v1.0.0_linux-x64.zip
retroachievementssharp_v1.0.0_linux-arm64.zip
```

!!! note "Linux permissions"
    Files cross-published from Windows carry no POSIX execute bit — run
    `chmod +x RetroAchievementsSharp` after copying to a Linux host (or set
    the bit when creating the archive on a Linux machine).

The `artifacts/` directory is git-ignored; sizes vary with the .NET version.

To publish all four targets in one go:

```bash
for rid in win-x64 win-arm64 linux-x64 linux-arm64; do
  dotnet publish RetroAchievementsSharp.Cli -c Release -r $rid \
    --self-contained true -p:PublishSingleFile=true -o artifacts/$rid
done
```

## Framework-dependent alternative

If the target machine already has a .NET 8/9/10 runtime, drop
`--self-contained true` for a ~200 KB launcher that uses the installed
runtime:

```bash
dotnet publish RetroAchievementsSharp.Cli -c Release -r linux-x64 -p:PublishSingleFile=true -o artifacts/fd-linux-x64
```

## What is inside the binary

The single file bundles the .NET runtime, `RetroAchievementsSharp` (engine),
`RetroAchievementsSharp.Cli`, `CHDSharp`, `RVZSharp` and `VideoGameFileSystemParser`. There
are no native libraries — trimming is intentionally disabled to keep the
reflection-free engine's behavior identical to the tested build.

## Packaging the library

`RetroAchievementsSharp` is packable (`IsPackable=true`) and carries complete NuGet
metadata: `net8.0;net9.0;net10.0` assemblies, XML docs on every public
member (CS1591 is an error), the GPL-2.0-or-later license and third-party
notices, a package icon, SourceLink (with symbols), repository metadata, and
a `PackageReleaseNotes` link to CHANGELOG.md. Package validation
(`EnablePackageValidation`) runs on every pack and fails the build if any
public API change would break consumers.

```bash
dotnet pack RetroAchievementsSharp -c Release -o artifacts
```

produces `artifacts/RetroAchievementsSharp.<version>.nupkg` and
`artifacts/RetroAchievementsSharp.<version>.snupkg`.

### Local smoke test of the package

Consume the just-packed nupkg from a scratch project:

```bash
mkdir /tmp/smoke && cd /tmp/smoke && dotnet new console --framework net8.0
dotnet add package RetroAchievementsSharp --source <repo>\release
dotnet run
```

### Pushing to NuGet.org

```bash
dotnet nuget push artifacts/RetroAchievementsSharp.1.0.0.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

Steps before the first push:

1. Bump `<Version>` in `RetroAchievementsSharp/RetroAchievementsSharp.csproj` and add a
   `CHANGELOG.md` entry (Keep a Changelog format).
2. Run the fast suite on all three TFMs
   (`dotnet test RetroAchievementsSharp.sln -c Release`), then the slow suite
   (`dotnet test RetroAchievementsSharp.Slow.Tests -c Release`).
3. Push a `v<version>` git tag (the release notes link to it).
4. Use an API key scoped to `RetroAchievementsSharp`; never commit the key.

The first release should be `1.0.0`: the engine behavior is pinned to
rcheevos 12.4.0 and the verifiable surface (415 fast + 172 slow tests per
TFM, parity corpus, RVZ-vs-ISO, real-ROM and published-hash spot checks) is
stable. After that,
follow <https://learn.microsoft.com/nuget/guides/api/package-versioning>
for SemVer-compliant bumps (behavior changes to the *ported* engine are
backwards-incompatible by design and warrant a major bump).