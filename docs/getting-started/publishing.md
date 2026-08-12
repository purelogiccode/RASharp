# Publishing builds

RASharp publishes **self-contained single-file executables** — no .NET
runtime needs to be installed on the target machine. The same portable
`net10.0` codebase produces binaries for Windows x64/arm64 and Linux
x64/arm64.

## The four targets

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
| `win-x64` | `artifacts/win-x64/RASharp.exe` | PE, ~74 MB |
| `win-arm64` | `artifacts/win-arm64/RASharp.exe` | PE, ~83 MB |
| `linux-x64` | `artifacts/linux-x64/RASharp` | ELF, ~75 MB |
| `linux-arm64` | `artifacts/linux-arm64/RASharp` | ELF, ~82 MB |

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

If the target machine already has the .NET 10 runtime, drop
`--self-contained true` for a ~200 KB launcher that uses the installed
runtime:

```bash
dotnet publish RASharp.Cli -c Release -r linux-x64 -p:PublishSingleFile=true -o artifacts/fd-linux-x64
```

## What is inside the binary

The single file bundles the .NET runtime, `RASharp.Core` (engine),
`RASharp.Cli`, `CHDSharp` and `VideoGameFileSystemParser`. There are no
native libraries — trimming is intentionally disabled to keep the
reflection-free engine's behavior identical to the tested build.
