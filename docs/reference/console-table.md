# Console table

The CLI console table (81 entries) — id, key, RA group, display name. Keys
are case-insensitive; **NULL-group consoles accept numeric ids only** (the
`find_console_id` fallback is `atoi` — see [known quirks](known-quirks.md)).

| ID | Key | Group | Name |
|---|---|---|---|
| 7 | `NES` | NINTENDO | NES/Famicom |
| 81 | `FDS` | NINTENDO | Famicom Disk System |
| 3 | `SNES` | NINTENDO | SNES/Super Famicom |
| 2 | `N64` | NINTENDO | Nintendo 64 |
| 16 | `GC` | NINTENDO | GameCube |
| 19 | `Wii` | NINTENDO | Wii |
| 4 | `GB` | NINTENDO | Game Boy |
| 6 | `GBC` | NINTENDO | Game Boy Color |
| 5 | `GBA` | NINTENDO | Game Boy Advance |
| 18 | `DS` | NINTENDO | Nintendo DS |
| 78 | `DSi` | NINTENDO | Nintendo DSi |
| 24 | `MINI` | NINTENDO | Pokemon Mini |
| 28 | `VB` | NINTENDO | Virtual Boy |
| 60 | `G&W` |  | Game & Watch |
| 62 | `3DS` |  | Nintendo 3DS |
| 20 | `WiiU` |  | Wii U |
| 12 | `PS1` | SONY | PlayStation |
| 21 | `PS2` | SONY | PlayStation 2 |
| 41 | `PSP` | SONY | PlayStation Portable |
| 25 | `2600` | ATARI | Atari 2600 |
| 51 | `7800` | ATARI | Atari 7800 |
| 17 | `JAG` | ATARI | Atari Jaguar |
| 77 | `JCD` | ATARI | Atari Jaguar CD |
| 13 | `Lynx` | ATARI | Atari Lynx |
| 50 | `5200` |  | Atari 5200 |
| 36 | `AST` |  | Atari ST |
| 33 | `SG1K` | SEGA | SG-1000 |
| 11 | `SMS` | SEGA | Master System |
| 1 | `MD` | SEGA | Genesis/Mega Drive |
| 9 | `SCD` | SEGA | Sega CD |
| 10 | `32X` | SEGA | 32X |
| 39 | `SAT` | SEGA | Saturn |
| 40 | `DC` | SEGA | Dreamcast |
| 15 | `GG` | SEGA | Game Gear |
| 68 | `Pico` |  | Sega Pico |
| 47 | `80/88` | NEC | PC-8000/8800 |
| 8 | `PCE` | NEC | PC Engine/TurboGrafx-16 |
| 76 | `PCCD` | NEC | PC Engine CD/TurboGrafx-CD |
| 49 | `PC-FX` | NEC | PC-FX |
| 67 | `PC-6000` |  | PC-6000 |
| 48 | `9800` |  | PC-9800 |
| 56 | `NGCD` | SNK | Neo Geo CD |
| 14 | `NGP` | SNK | Neo Geo Pocket |
| 43 | `3DO` | OTHERS | 3DO Interactive Multiplayer |
| 37 | `CPC` | OTHERS | Amstrad CPC |
| 38 | `A2` | OTHERS | Apple II |
| 27 | `ARC` | OTHERS | Arcade |
| 73 | `A2001` | OTHERS | Arcadia 2001 |
| 71 | `ARD` | OTHERS | Arduboy |
| 44 | `CV` | OTHERS | ColecoVision |
| 75 | `ELEK` | OTHERS | Elektor TV Games Computer |
| 57 | `CHF` | OTHERS | Fairchild Channel F |
| 45 | `INTV` | OTHERS | Intellivision |
| 74 | `VC4000` | OTHERS | Interton VC 4000 |
| 23 | `MO2` | OTHERS | Magnavox Odyssey 2 |
| 69 | `DUCK` | OTHERS | Mega Duck |
| 29 | `MSX` | OTHERS | MSX |
| 80 | `UZE` | OTHERS | Uzebox |
| 46 | `VECT` | OTHERS | Vectrex |
| 72 | `WASM4` | OTHERS | WASM-4 |
| 63 | `WSV` | OTHERS | Watara Supervision |
| 53 | `WS` | OTHERS | WonderSwan |
| 35 | `Amiga` |  | Amiga |
| 54 | `ECV` |  | Cassette Vision |
| 55 | `ESCV` |  | Super Cassette Vision |
| 30 | `C64` |  | Commodore 64 |
| 58 | `FMTowns` |  | FM Towns |
| 61 | `N-Gage` |  | Nokia N-Gage |
| 32 | `Oric` |  | Oric |
| 42 | `CD-i` |  | Philips CD-i |
| 64 | `X1` |  | Sharp X1 |
| 52 | `X68K` |  | Sharp X68000 |
| 66 | `TO8` |  | Thomson TO8 |
| 79 | `TI83` |  | TI-83 |
| 65 | `TIC-80` |  | TIC-80 |
| 34 | `VIC-20` |  | VIC-20 |
| 70 | `Zeebo` |  | Zeebo |
| 31 | `ZX81` |  | ZX81 |
| 59 | `ZXS` |  | ZX Spectrum |
| 26 | `DOS` |  | DOS |
| 22 | `Xbox` |  | Xbox |

!!! tip "Finding a console"
    Run `RASharp` with no arguments — the CLI prints this exact table,
    byte-identical to the 1.8.3 usage output (modulo the executable name).
