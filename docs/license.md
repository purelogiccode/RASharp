# License

RetroAchievementsSharp is licensed under the **GNU General Public License, version 2 or
later** (GPL-2.0-or-later). It is a translation of the MIT-licensed rcheevos
`rc_hash` engine that links the GPL-2.0-or-later RVZSharp library (Dolphin
RVZ/WIA, used for live GameCube/Wii disc hashing); its CLI behavior is
derived (observably, not textually) from the GPL-3.0 RAHasher reference
implementation — which is **never shipped**.

## GPL-2.0-or-later (this project)

The project is distributed under GPL-2.0-or-later (see `LICENSE`):
Copyright (c) 2026 Peterson Fernandes and Pure Logic Code. The ported engine
retains the upstream rcheevos MIT copyright notice, reproduced in
`THIRD-PARTY-NOTICES.md`.

## Third-party components

| Component | License | Role |
|---|---|---|
| rcheevos `rc_hash` (`40d916d` → 12.4.0) | MIT — Copyright (c) 2018 RetroAchievements.org | the ported engine + test vectors |
| CHDSharp 1.2.0 | MIT | CHD reading |
| RVZSharp 1.0.0 | **GPL-2.0-or-later** (Dolphin-derived) | GameCube/Wii RVZ/WIA live hashing |
| VideoGameFileSystemParser 1.2.0 | MIT (per package metadata) | alternative ISO9660/UDF backend |
| Serilog 4.4.0 | Apache-2.0 | logging |
| RAHasher 1.8.3 (RALibretro lineage, [LeXofLeviafan](https://github.com/LeXofLeviafan/) fork) | **GPL-3.0** | **reference only** — CLI behavior (`RAHasher.cpp`, `Util.cpp`, `Hash3DS.cpp`, `HashCHD.cpp`) is re-implemented fresh, and LeXofLeviafan's RAHasher binaries are the **reference oracle for the parity test suite**; the GPL sources and the C oracle binaries live in `References/` and are never part of the shipped sources, binaries, or packages |

## What "reference only" means in practice

- `Program.cs`, `FileUtil.cs`, `Hash3DS.cs`, `ChdCdReader.cs` are new C#
  implementations written to match observable behavior — no GPL text is
  copied; each file carries an origin header stating this.
- The C oracle binaries under `References/` are built from GPL material for
  local test purposes only (git-ignored, never distributed).
- The console table (ids, keys, names) is factual metadata, safe to reuse.

## Usage in your own projects

RetroAchievementsSharp is GPL-2.0-or-later: you can link it, use it, and modify it under
the GPL-2.0-or-later terms (see `LICENSE`), including (at your option) any
later version of the GPL. If you ship binaries or a derived work, keep a
copy of `LICENSE` and `THIRD-PARTY-NOTICES.md` alongside them and make your
sources available under the same terms.