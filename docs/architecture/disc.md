# Disc hashing (`HashDisc.cs` + `CdReader.cs` — port of `hash_disc.c` + `cdreader.c`)

Disc hashing reads files *inside* an optical disc image: track selection,
sector math, ISO9660 lookups, and console-specific boot-file rules. The
default CD reader (`CdReader.cs`) provides the `.cue`/`.gdi`/`.bin` parsing
and the track abstraction.

## Track selection

The engine opens tracks by logical selection, mirroring `RC_HASH_CDTRACK_*`:

| Selection | Meaning |
|---|---|
| `FIRST_DATA` | first track whose mode is `MODE*` |
| `LAST` | last track in the table |
| `LARGEST` | largest data track (by file size) |
| `FIRST_OF_SECOND_SESSION` | first track of session 2 (Jaguar CD) |

## Sector math (the part most likely to surprise)

- **MSF ↔ LBA**: the C computes `msf_to_lba = (m*60+s)*75 + f − 150`
  (LBA 0 = MSF `00:02:00`). Raw 2352-byte images must carry real MSF in the
  sector headers — a synthetic image whose sector 0 says `00:00:00` yields
  `track_first_sector = −150`, which breaks absolute-LBA reads.
- **Cue `INDEX 01 00:00:00`** parses to sector offset 0 (the cue parser does
  *not* subtract 150); real-world cues use `00:02:00`.
- **Sector sizes**: `MODE1/2048` (2048, header 0), `MODE2/2336` (2336,
  header 8), `MODE1/2352` (2352, header 16), `MODE2/2352` (2352, header 24
  — includes the 8-byte XA subheader), `AUDIO` (2352, header 0, raw stream).
- **Sector-size probe** (`cdreader_determine_sector_size`): reads 32 bytes
  at `(16 + pregap) · 2352` looking for the 12-byte sync pattern, then
  `CD001` at +25 (XA) / +17 / +1, falling back to file-size heuristics.

## ISO9660 mini-parser

`HashDisc.CdFindFileSector` is the port of `rc_cd_find_file_sector`:

- reads the PVD at LBA 16 (256 bytes);
- root directory extent + size from the PVD record;
- walks directory records (name, version `;1` handling, case-insensitive
  compare), recursing through subdirectories;
- returns the file's **sector** and **size** for `rc_hash_cd_file`.

The walker is deliberately tolerant (it does not require spec-perfect
directory records) — this is the *default* backend. The alternative backend
is `FileSystemResolver` (VideoGameFileSystemParser), which is spec-strict
and CHD-bound (see the [porting guide](../development/porting-guide.md)).

## Per-console rules

| Console | Algorithm |
|---|---|
| **PSX** | first data track; read `SYSTEM.CNF`, parse `BOOT=cdrom:\...`, MD5 of the boot file **with the exe name prefixed**; fallback to `PSX.EXE` |
| **PS2** | `BOOT2 = cdrom0:\...` via ISO9660; MD5 of the ELF (with `ELF` marker check) |
| **PSP** | `PSP_GAME\PARAM.SFO` + `PSP_GAME\SYSDIR\EBOOT.BIN` (MD5 of both); `.pbp` = whole-file |
| **Saturn / Sega CD** | first 512 bytes of sector 0, magic-checked (`SEGA SEGASATURN` / `SEGADISCSYSTEM`) |
| **PCE-CD** | first data track, header check `PC Engine CD-ROM SYSTEM` |
| **PC-FX** | largest data track, sector-0 marker check |
| **3DO** | OperaFS: `LAUNCHME` lookup (case-insensitive), volume header included in the hash |
| **Jaguar CD** | second-session first track; `ATARI APPROVED DATA HEADER` scan, byteswap variants, homebrew KART-track logic |
| **Neo Geo CD** | `IPL.TXT` contents lookup (lowercase variant), hash the listed executables |
| **Dreamcast** | OperaFS `IP.BIN` + largest track rules |
| **GameCube** | `0xC2339F3D` magic at `0x1C`, then disc-partition reading |
| **Wii** | disc partition path vs WiiWare TMD/content |

## `rc_hash_cd_file`

The shared helper streams a file region (sector + size) into the MD5 across
sector boundaries, handling 2352/2048 conversions and the 64 MiB cap — ported
1:1.

## 12.4.0 changes ported in Part II

- **3DO short-read guard**: the OperaFS magic is only checked when the
  sector read returned ≥ 132 bytes (`rc_hash_3do`).
- **GDI filename bounds**: an unterminated quoted filename errors with
  `Quoted string without closing quote`; filenames ≥ 256 bytes error with
  `Cannot copy %u byte filename into %u byte buffer` (previously an
  unbounded copy).
- Wii/WiiWare malloc-failure checks are N/A in managed code (noted in
  comments).
