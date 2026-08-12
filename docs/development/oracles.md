# Building the oracles

The parity suite compares against **C-built oracle binaries**. They live in
`References/` (git-ignored, GPL-3.0 — built from GPL reference material,
used only as local test oracles, never shipped).

## Which oracle is used

The harness resolves, in order:

1. `RASHARP_ORACLE` env var;
2. `References\rcheevos-12.4.0\bin64\RAHasher.exe` — **built from rcheevos
   12.4.0** (the Part II source of truth);
3. `References\RAHasher-1.8.3\bin64\RAHasher.exe` — built from the pinned
   1.8.3 sources;
4. `References\RAHasher.exe` — any other 1.8.3 binary (a legacy build that
   only accepts numeric console ids; the harness adapts).

## Prerequisites (Windows)

- **MSYS2** (`/c/msys64`) with `make`, `sh`, and the MinGW-w64 toolchain
  (`gcc`/`g++` 16.x).
- **miniz** and **libchdr** sources under `References/RAHasher-1.8.3/src/`
  (cloned; libchdr pinned to v0.3.0 with its vendored deps
  lzma-25.01/zstd-1.5.7/miniz-3.1.1).

### The TMP problem (why the wrapper scripts exist)

The MSYS2 `sh` clears `TMP`/`TEMP` for child processes, so MinGW gcc falls
back to `C:\WINDOWS` and fails with *"Cannot create temporary file"*. The
`ccw-gcc.sh` / `ccw-g++.sh` wrappers set `TMP=C:\msys64\tmp` before
`exec`-ing the real compiler:

```sh
#!/bin/sh
export TMP='C:\msys64\tmp'
export TEMP='C:\msys64\tmp'
exec /c/msys64/mingw64/bin/gcc.exe "$@"
```

## Building the 1.8.3 oracle

```bash
cd References/RAHasher-1.8.3
export PATH="/c/msys64/usr/bin:/c/msys64/mingw64/bin:$PATH"
make -f Makefile.RAHasher HAVE_CHD=1 CC=./ccw-gcc.sh CXX=./ccw-g++.sh
# → bin64/RAHasher.exe
```

Notes on the reference-tree adaptations (build-only, never shipped):

- `src/rcheevos/` is the 40d916d snapshot; `src/miniz` is 3.1.0 (master's
  API drifted — 3.1.0 restores `mz_alloc_func`/`tinfl_decompressor`);
  `src/Util.cpp` includes `<miniz.h>` before `<miniz_zip.h>` (the header
  split);
- `Makefile.common`'s `CHD_OBJS` were adapted to libchdr v0.3.0's dep
  layout (`lzma-25.01/src/LzmaDec.c`, `zstd-1.5.7/zstddeclib.c`, codec
  files split per-module) and `codec_zlib`'s miniz include is shimmed to the
  CLI's miniz to avoid duplicate symbols;
- `src/RA_BuildVer.h` is a handwritten `1.8.3` header (the git-generated
  one needs the RAInterface submodule).

## Building the 12.4.0 oracle

```bash
cd References/rcheevos-12.4.0
export PATH="/c/msys64/usr/bin:/c/msys64/mingw64/bin:$PATH"
make -f Makefile.oracle CC=./ccw-gcc.sh CXX=./ccw-g++.sh
# → bin64/RAHasher.exe
```

`Makefile.oracle` compiles the RAHasher 1.8.3 CLI stack
(`RAHasher.cpp`, `Util.cpp`, `Hash3DS.cpp`, `HashCHD.cpp`, `Logger.cpp`,
`sha256.c`) **against rcheevos 12.4.0's `src/rhash`**, reusing the same
miniz 3.1.0 and libchdr v0.3.0 trees for zip pre-load and CHD support.

## Why CHD needs libchdr

`rc_hash`'s default cdreader has no CHD support — the CHD track reader is
`HashCHD.cpp` (from the RAHasher tree, GPL reference), which plugs into
`rc_hash_cdreader_t` and reads hunks via libchdr. Both oracles therefore
link libchdr v0.3.0 + vendored deps (zstd single-file decoder, LZMA
decoder, miniz for the zlib codec).

## Verification

After building, smoke-test:

```bash
bin64/RAHasher.exe GB <any-file>        # hash + exit 0
bin64/RAHasher.exe '?' <any-file>       # iterate mode
bin64/RAHasher.exe PS1 <game>.cue       # disc
bin64/RAHasher.exe 12 <game>.chd        # CHD (numeric id also works)
bin64/RAHasher.exe -s <sysdir> 62 <file>.cia   # 3DS
```

Then run `dotnet test --filter FullyQualifiedName~Parity` — all 90 cases
must pass byte-identically.
