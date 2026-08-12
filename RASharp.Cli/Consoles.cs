// New implementation, behavior parity with RALibretro RAHasher (GPL-3.0,
// used as reference only). The console table below is factual console
// metadata (IDs, keys, group names, display names) copied from
// RAHasher.cpp's CONSOLES[]; group NULL marks "not supported by RA".

using RASharp.Cli.Models;

namespace RASharp.Cli;

/// <summary>New implementation, behavior parity with RALibretro RAHasher (GPL-3.0, used as reference only). The console table below is factual console metadata (IDs, keys, </summary>
public static class Consoles
{
    private const string NINTENDO = "Nintendo";
    private const string SONY = "Sony";
    private const string ATARI = "Atari";
    private const string SEGA = "Sega";
    private const string NEC = "NEC";
    private const string SNK = "SNK";
    private const string OTHERS = "Others";

    public static readonly ConsoleInfo[] All = new[]
    {
        new ConsoleInfo(7, "NES", NINTENDO, "NES/Famicom"),
        new ConsoleInfo(81, "FDS", NINTENDO, "Famicom Disk System"),
        new ConsoleInfo(3, "SNES", NINTENDO, "SNES/Super Famicom"),
        new ConsoleInfo(2, "N64", NINTENDO, "Nintendo 64"),
        new ConsoleInfo(16, "GC", NINTENDO, "GameCube"),
        new ConsoleInfo(19, "Wii", NINTENDO, "Wii"),
        new ConsoleInfo(4, "GB", NINTENDO, "Game Boy"),
        new ConsoleInfo(6, "GBC", NINTENDO, "Game Boy Color"),
        new ConsoleInfo(5, "GBA", NINTENDO, "Game Boy Advance"),
        new ConsoleInfo(18, "DS", NINTENDO, "Nintendo DS"),
        new ConsoleInfo(78, "DSi", NINTENDO, "Nintendo DSi"),
        new ConsoleInfo(24, "MINI", NINTENDO, "Pokemon Mini"),
        new ConsoleInfo(28, "VB", NINTENDO, "Virtual Boy"),
        new ConsoleInfo(60, "G&W", null, "Game & Watch"),
        new ConsoleInfo(62, "3DS", null, "Nintendo 3DS"),
        new ConsoleInfo(20, "WiiU", null, "Wii U"),

        new ConsoleInfo(12, "PS1", SONY, "PlayStation"),
        new ConsoleInfo(21, "PS2", SONY, "PlayStation 2"),
        new ConsoleInfo(41, "PSP", SONY, "PlayStation Portable"),

        new ConsoleInfo(25, "2600", ATARI, "Atari 2600"),
        new ConsoleInfo(51, "7800", ATARI, "Atari 7800"),
        new ConsoleInfo(17, "JAG", ATARI, "Atari Jaguar"),
        new ConsoleInfo(77, "JCD", ATARI, "Atari Jaguar CD"),
        new ConsoleInfo(13, "Lynx", ATARI, "Atari Lynx"),
        new ConsoleInfo(50, "5200", null, "Atari 5200"),
        new ConsoleInfo(36, "AST", null, "Atari ST"),

        new ConsoleInfo(33, "SG1K", SEGA, "SG-1000"),
        new ConsoleInfo(11, "SMS", SEGA, "Master System"),
        new ConsoleInfo(1, "MD", SEGA, "Genesis/Mega Drive"),
        new ConsoleInfo(9, "SCD", SEGA, "Sega CD"),
        new ConsoleInfo(10, "32X", SEGA, "32X"),
        new ConsoleInfo(39, "SAT", SEGA, "Saturn"),
        new ConsoleInfo(40, "DC", SEGA, "Dreamcast"),
        new ConsoleInfo(15, "GG", SEGA, "Game Gear"),
        new ConsoleInfo(68, "Pico", null, "Sega Pico"),

        new ConsoleInfo(47, "80/88", NEC, "PC-8000/8800"),
        new ConsoleInfo(8, "PCE", NEC, "PC Engine/TurboGrafx-16"),
        new ConsoleInfo(76, "PCCD", NEC, "PC Engine CD/TurboGrafx-CD"),
        new ConsoleInfo(49, "PC-FX", NEC, "PC-FX"),
        new ConsoleInfo(67, "PC-6000", null, "PC-6000"),
        new ConsoleInfo(48, "9800", null, "PC-9800"),

        new ConsoleInfo(56, "NGCD", SNK, "Neo Geo CD"),
        new ConsoleInfo(14, "NGP", SNK, "Neo Geo Pocket"),

        new ConsoleInfo(43, "3DO", OTHERS, "3DO Interactive Multiplayer"),
        new ConsoleInfo(37, "CPC", OTHERS, "Amstrad CPC"),
        new ConsoleInfo(38, "A2", OTHERS, "Apple II"),
        new ConsoleInfo(27, "ARC", OTHERS, "Arcade"),
        new ConsoleInfo(73, "A2001", OTHERS, "Arcadia 2001"),
        new ConsoleInfo(71, "ARD", OTHERS, "Arduboy"),
        new ConsoleInfo(44, "CV", OTHERS, "ColecoVision"),
        new ConsoleInfo(75, "ELEK", OTHERS, "Elektor TV Games Computer"),
        new ConsoleInfo(57, "CHF", OTHERS, "Fairchild Channel F"),
        new ConsoleInfo(45, "INTV", OTHERS, "Intellivision"),
        new ConsoleInfo(74, "VC4000", OTHERS, "Interton VC 4000"),
        new ConsoleInfo(23, "MO2", OTHERS, "Magnavox Odyssey 2"),
        new ConsoleInfo(69, "DUCK", OTHERS, "Mega Duck"),
        new ConsoleInfo(29, "MSX", OTHERS, "MSX"),
        /* {RC_CONSOLE_STANDALONE, "EXE", OTHERS, "Standalone"} - >90, not usable */
        new ConsoleInfo(80, "UZE", OTHERS, "Uzebox"),
        new ConsoleInfo(46, "VECT", OTHERS, "Vectrex"),
        new ConsoleInfo(72, "WASM4", OTHERS, "WASM-4"),
        new ConsoleInfo(63, "WSV", OTHERS, "Watara Supervision"),
        new ConsoleInfo(53, "WS", OTHERS, "WonderSwan"),
        new ConsoleInfo(35, "Amiga", null, "Amiga"),
        new ConsoleInfo(54, "ECV", null, "Cassette Vision"),
        new ConsoleInfo(55, "ESCV", null, "Super Cassette Vision"),
        new ConsoleInfo(30, "C64", null, "Commodore 64"),
        new ConsoleInfo(58, "FMTowns", null, "FM Towns"),
        new ConsoleInfo(61, "N-Gage", null, "Nokia N-Gage"),
        new ConsoleInfo(32, "Oric", null, "Oric"),
        new ConsoleInfo(42, "CD-i", null, "Philips CD-i"),
        new ConsoleInfo(64, "X1", null, "Sharp X1"),
        new ConsoleInfo(52, "X68K", null, "Sharp X68000"),
        new ConsoleInfo(66, "TO8", null, "Thomson TO8"),
        new ConsoleInfo(79, "TI83", null, "TI-83"),
        new ConsoleInfo(65, "TIC-80", null, "TIC-80"),
        new ConsoleInfo(34, "VIC-20", null, "VIC-20"),
        new ConsoleInfo(70, "Zeebo", null, "Zeebo"),
        new ConsoleInfo(31, "ZX81", null, "ZX81"),
        new ConsoleInfo(59, "ZXS", null, "ZX Spectrum"),
        new ConsoleInfo(26, "DOS", null, "DOS"),
        new ConsoleInfo(22, "Xbox", null, "Xbox"),
    };
}
