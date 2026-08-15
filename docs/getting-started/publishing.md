# Publishing builds

RASharp ships two artifacts:

1. **The `RASharp` NuGet library** (net8.0;net9.0;net10.0) — for
   consumers; see [Packaging the NuGet library](#packaging-the-nuget-library).
2. **Self-contained single-file CLI executables** — no .NET runtime needs to
   be installed on the target machine. The same multi-targeted
   (`net8.0`/`net9.0`/`net10.0`) codebase produces binaries for Windows
   x64/arm64 and Linux x64/arm64.

## The four CLI targets

```bash
dotnet publish RASharp.Cli -c Release -r win-x64     --self-contained true -p:PublishSingleFile=true -o artifacts/win-x64
dotnet publish RASharp.Cli -c Release -r win-arm64   --self-contained true -p:PublishSingleFile=true -o artifacts/win-arm64
dotnet publish RASharp.Cli -c Release -r linux-x64   --self-contained true -p:PublishSingleFile=true -o artifacts/linux-x64
dotnet publish RASharp.Cli -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -o artifacts/linux-arm64
```

Cross-publishing works from any host: the SDK downloads the matching runtime
pack from NuGet, so e.g. Linux-arm64 can be produced on Windows.

## Output

| RID | File | Notes |
|---|---|---|
| `win-x64` | `artifacts/win-x64/RASharp.Cli.exe` | PE |
| `win-arm64` | `artifacts/win-arm64/RASharp.Cli.exe` | PE |
| `linux-x64` | `artifacts/linux-x64/RASharp` | ELF |
| `linux-arm64` | `artifacts/linux-arm64/RASharp` | ELF |

!!! note "Linux permissions"
    Files cross-published from Windows carry no POSIX execute bit — run
    `chmod +x RASharp` after copying to a Linux host.

The `artifacts/` directory is git-ignored; sizes vary with the .NET version.

To publish all four targets in one go:

```bash
for rid in win-x64 win-arm64 linux-x64 linux-arm64; do
  dotnet publish RASharp.Cli -c Release -r $rid \
    --self-contained true -p:PublishSingleFile=true -o artifacts/$rid
done
```

## Framework-dependent alternative

If the target machine already has a .NET 8/9/10 runtime, drop
`--self-contained true` for a ~200 KB launcher that uses the installed
runtime:

```bash
dotnet publish RASharp.Cli -c Release -r linux-x64 -p:PublishSingleFile=true -o artifacts/fd-linux-x64
```

## What is inside the binary

The single file bundles the .NET runtime, `RASharp` (engine),
`RASharp.Cli`, `CHDSharp` and `VideoGameFileSystemParser`. There are no
native libraries — trimming is intentionally disabled to keep the
reflection-free engine's behavior identical to the tested build.

## Packaging the library

`RASharp` is packable (`IsPackable=true`) and carries complete NuGet
metadata: `net8.0;net9.0;net10.0` assemblies, XML docs, the MIT license and
third-party notices, a package icon, SourceLink (with symbols), repository
metadata, and a `PackageReleaseNotes` link to CHANGELOG.md. Package
validation (`EnablePackageValidation`) runs on every pack and fails the
build if any public API change would break consumers.

```bash
dotnet pack RASharp -c Release -o artifacts
```

produces `artifacts/RASharp.<version>.nupkg` and
`artifacts/RASharp.<version>.snupkg`.

### Local smoke test of the package

Consume the just-packed nupkg from a scratch project:

```bash
mkdir /tmp/smoke && cd /tmp/smoke && dotnet new console --framework net8.0
dotnet add package RASharp --source C:\Sincronizar\source\repos\CSharp_RASharp\artifacts
dotnet run
```

### Pushing to NuGet.org

```bash
dotnet nuget push artifacts/RASharp.1.0.0.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

Steps before the first push:

1. Bump `<Version>` in `RASharp/RASharp.csproj` and add a
   `CHANGELOG.md` entry (Keep a Changelog format).
2. Run the full suite on all three TFMs (`dotnet test RASharp.sln -c Release`).
3. Push a `v<version>` git tag (the release notes link to it).
4. Use an API key scoped to `RASharp`; never commit the key.

The first release should be `1.0.0`: the engine behavior is pinned to
rcheevos 12.4.0 and the verifiable surface (581 tests per TFM, parity
corpus, real-ROM and published-hash spot checks) is stable. After that,
follow <https://learn.microsoft.com/nuget/guides/api/package-versioning>
for SemVer-compliant bumps (behavior changes to the *ported* engine are
backwards-incompatible by design and warrant a major bump).