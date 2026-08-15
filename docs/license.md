# License

RASharp is **MIT licensed**. It is a translation of the MIT-licensed
rcheevos `rc_hash` engine, uses two MIT NuGet packages, and its CLI
behavior is derived (observably, not textually) from the GPL-3.0 RAHasher
reference implementation — which is **never shipped**.

## MIT (this project)

The project is distributed under the MIT License (see `LICENSE` in the repo
root). Copyright (c) 2026 Peterson Fernandes; the ported engine retains the
upstream rcheevos copyright notice in `THIRD-PARTY-NOTICES.md`.

## Third-party components

| Component | License | Role |
|---|---|---|
| rcheevos `rc_hash` (`40d916d` → 12.4.0) | MIT — Copyright (c) 2018 RetroAchievements.org | the ported engine + test vectors |
| CHDSharp 1.2.0 | MIT | CHD reading |
| VideoGameFileSystemParser 1.2.0 | MIT (per package metadata) | alternative ISO9660/UDF backend |
| RAHasher 1.8.3 (RALibretro lineage, LeXofLeviafan fork) | **GPL-3.0** | **reference only** — CLI behavior (`RAHasher.cpp`, `Util.cpp`, `Hash3DS.cpp`, `HashCHD.cpp`) is re-implemented fresh; the GPL sources and the C oracle binaries live in `References/` and are never part of the shipped sources, binaries, or packages |

## What "reference only" means in practice

- `Program.cs`, `FileUtil.cs`, `Hash3DS.cs`, `ChdCdReader.cs` are new C#
  implementations written to match observable behavior — no GPL text is
  copied; each file carries an origin header stating this.
- The C oracle binaries under `References/` are built from GPL material for
  local test purposes only (git-ignored, never distributed).
- The console table (ids, keys, names) is factual metadata, safe to reuse.

## Usage in your own projects

You can link `RASharp` under MIT terms, exactly as you would any other
MIT library. If you ship binaries, keep `THIRD-PARTY-NOTICES.md` alongside
them.

!!! warning "VideoGameFileSystemParser"
    The package metadata declares MIT. Confirm with the upstream author
    before any MIT-licensed distribution depends on it — see the note in
    `THIRD-PARTY-NOTICES.md`.
