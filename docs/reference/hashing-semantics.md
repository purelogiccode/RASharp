# Hashing semantics

How each console's hash is computed. All algorithms are 1:1 translations of
rcheevos `40d916d` (12.2.1) + the 12.4.0 deltas; details per module in the
[architecture](../architecture/overview.md) section.

## Universal rules

- **Digest**: MD5 (streamed, `System.Security.Cryptography.MD5`).
- **64 MiB cap**: whole-file, buffered, `.neo`, and disc-file hashes stop at
  `MAX_BUFFER_SIZE = 64 * 1024 * 1024` — identical to the C.
- **Case-insensitive** filename/record compares where the C uses
  `strncasecmp` (ISO9660, 3DO `LAUNCHME`, Neo Geo CD `IPL.TXT`, arcade
  folders).
- **Version numbers** (`;1`) are stripped in ISO9660 record compares.

## Cartridges

| Console | Hash input |
|---|---|
| NES/FDS | ROM after the 16-byte iNES/FDS header (header detection guarded by `buffer_size > 16`) |
| SNES | whole file |
| N64 | first 1 MiB, 16-bit byte-swapped (.v64/.n64/.z64/.ndd variants) |
| NDS/DSi | header + ARM9/ARM7/icon region; SuperCard 512-byte variant |
| GB/GBC/GBA, GG, SMS, MD, SG-1000, 32X, 2600, Jaguar, Lynx*, PCE, SCV, WS, NGP, MINI, VB, UZE, VECT, WASM-4, TIC-80, TI-83, MSX, CPC, A2, C64, Oric, PC-8800, ZXS, CV, INTV, MO2, VC4000, A2001, CHF, DUCK, WSV, ELEK, 80/88, 9800, PC-6000, Amiga, CD-i, X1, X68K, TO8, Pico, FMTowns, N-Gage, Zeebo, ECV, ESCV, ZX81, VIC-20, G&W, WiiU, Xbox, DOS (non-zip) | whole file |

\* Lynx strips the 64-byte `LYNX` header when present (`buffer_size > 64`).

| Console | Hash input |
|---|---|
| Atari 7800 | ROM after the 128-byte `ATARI7800` header (when present) |
| Arcade (romset zip) | **filename** (no extension) + FBNeo subsystem folder |
| Arcade (`.neo`) | ROM data after the 4096-byte `NEO\1` header (Geolith carts) |
| Arduboy | zip filtered to `interp_s2_ArduboyFX.hex` + `.bin` entries |
| MS-DOS (`.dosz`/`.dosc`) | zip record hashes + sibling `.dosc` + parent chain |

## Discs

| Console | Hash input |
|---|---|
| PSX | `SYSTEM.CNF` → `BOOT=cdrom:\...` executable (exe-name prefix + contents); fallback `PSX.EXE` |
| PS2 | `BOOT2 = cdrom0:\...` ELF via ISO9660 |
| PSP | `PSP_GAME\PARAM.SFO` + `PSP_GAME\SYSDIR\EBOOT.BIN`; `.pbp` whole-file |
| Saturn / Sega CD | first 512 bytes of sector 0 (magic-checked) |
| PCE-CD | first data track sector-0 header region |
| PC-FX | largest data track, sector-0 marker |
| 3DO | OperaFS volume header + `LAUNCHME` executable |
| Jaguar CD | second-session boot header (`ATARI APPROVED DATA HEADER`) + boot executable; byteswap variants; homebrew track-2 KART logic |
| Neo Geo CD | executables listed in `IPL.TXT` |
| Dreamcast | OperaFS `IP.BIN` + track-3 rules |
| GameCube | partition reading after `0xC2339F3D` magic at `0x1C` |
| Wii | disc partition path (or WiiWare TMD/content) |

## Encrypted (3DS)

| Format | Hash input |
|---|---|
| `.cia` | decrypted content (AES-128-CBC, CIA normal key) — equals the NCCH hash |
| `.3ds` / NCCH | decrypted ExeFS/RomFS (per-sector IVs) |
| `.3dsx` | whole file (plaintext homebrew) |

Keys from `aes_keys.txt` + `seeddb.bin` via `-s` (see
[3DS encryption](../architecture/encrypted.md)).

## Zip pre-load (CLI)

`.zip` + console ≤ 90 → the **first entry** is extracted and hashed via the
buffer API (`util::loadZippedFile` semantics).
