# Third-Party Notices

RASharp is an MIT-licensed port of the hashing engine of RAHasher 1.8.3
(`rcheevos` commit `40d916d`). This file lists every third-party component
that contributes code, behavior, or test vectors to this project, with its
license and provenance.

## Components

| Component | Version/Pin | License | Provenance / Use |
|---|---|---|---|
| rcheevos (rc_hash engine) | commit `40d916de00fe757bab40fb4db41a7912193a48e3` | MIT — Copyright (c) 2018 RetroAchievements.org | Ported 1:1 into `RASharp` (see `src/rhash/*.c`); test vectors under `test/rhash/` ported into `RASharp.Tests` |
| CHDSharp | 1.2.0 (NuGet) | MIT — Copyright (c) 2026 Peterson Fernandes | CHD V1–V5 reading in `ChdCdReader` |
| RVZSharp | 1.0.0 (NuGet) | GPL-2.0-or-later — see note below | GameCube/Wii RVZ/WIA decoding on the fly in `RvzFilereader` (no rvz→iso conversion) |
| VideoGameFileSystemParser | 1.2.0 (NuGet) | MIT (per package metadata; see note below) | Optional ISO9660/UDF filesystem backend (`FileSystemResolver`) |

> **Note on RVZSharp licensing:** RVZSharp is copyright (c) Peterson Fernandes /
> Pure Logic Code and is licensed **GPL-2.0-or-later**, because its WIA/RVZ
> format logic is derived from Dolphin (GPL-2.0-or-later). Adding RVZSharp as a
> dependency of the MIT-licensed RASharp package means the combined NuGet
> package carries GPL-2.0-or-later code. Distributing RASharp with RVZSharp
> therefore requires the RVZ-enabled package to be treated as
> GPL-2.0-or-later (or offered as a separate package).

> **Note on VideoGameFileSystemParser licensing:** the package metadata
> declares `PackageLicenseExpression = MIT`. Confirm with the upstream
> author before any MIT-licensed distribution of RASharp depends on it.

## GPL reference material (NOT shipped)

The following are used **only as read-only behavioral references** while
writing the C# implementation, and are **not** included in the RASharp
sources, binaries, or NuGet packages:

- `RAHasher-1.8.3` — RAHasher CLI sources (`RAHasher.cpp`, `Util.cpp`,
  `Hash3DS.cpp`, `HashCHD.cpp`, `Logger.*`), GPL-3.0 (RALibretro lineage,
  LeXofLeviafan fork). RASharp's `Program.cs`, `FileUtil.cs`, `Hash3DS.cs`,
  and `ChdCdReader.cs` are new implementations written to match observable
  behavior only; no GPL text is copied.
- `RAHasher.exe` (test oracle for the parity harness) — built from the
  GPL-3.0 sources above; lives in `References\` / `tools\` only, never shipped.

## MIT License (applies to the components above)

MIT License

Copyright (c) 2018 RetroAchievements.org
Copyright (c) 2026 Peterson Fernandes

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
