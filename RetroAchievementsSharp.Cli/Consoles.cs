// New implementation, behavior parity with RALibretro RAHasher (GPL-3.0,
// used as reference only). The console table below is factual console
// metadata (IDs, keys, group names, display names) copied from
// RAHasher.cpp's CONSOLES[]; group NULL marks "not supported by RA".

using RetroAchievementsSharp.Cli.Models;

namespace RetroAchievementsSharp.Cli;

/// <summary>New implementation, behavior parity with RALibretro RAHasher (GPL-3.0, used as reference only). The console table below is factual console metadata (IDs, keys, </summary>
internal static class Consoles
{
    private const string Nintendo = "Nintendo";
    private const string Sony = "Sony";
    private const string Atari = "Atari";
    private const string Sega = "Sega";
    private const string Nec = "NEC";
    private const string Snk = "SNK";
    private const string Others = "Others";

    /// <summary>Every console the CLI knows, in the RAHasher table order.</summary>
    internal static readonly ConsoleInfo[] All =
    [
        new(7, "NES", Nintendo, "NES/Famicom"),
        new(81, "FDS", Nintendo, "Famicom Disk System"),
        new(3, "SNES", Nintendo, "SNES/Super Famicom"),
        new(2, "N64", Nintendo, "Nintendo 64"),
        new(16, "GC", Nintendo, "GameCube"),
        new(19, "Wii", Nintendo, "Wii"),
        new(4, "GB", Nintendo, "Game Boy"),
        new(6, "GBC", Nintendo, "Game Boy Color"),
        new(5, "GBA", Nintendo, "Game Boy Advance"),
        new(18, "DS", Nintendo, "Nintendo DS"),
        new(78, "DSi", Nintendo, "Nintendo DSi"),
        new(24, "MINI", Nintendo, "Pokemon Mini"),
        new(28, "VB", Nintendo, "Virtual Boy"),
        new(60, "G&W", null, "Game & Watch"),
        new(62, "3DS", null, "Nintendo 3DS"),
        new(20, "WiiU", null, "Wii U"),

        new(12, "PS1", Sony, "PlayStation"),
        new(21, "PS2", Sony, "PlayStation 2"),
        new(41, "PSP", Sony, "PlayStation Portable"),

        new(25, "2600", Atari, "Atari 2600"),
        new(51, "7800", Atari, "Atari 7800"),
        new(17, "JAG", Atari, "Atari Jaguar"),
        new(77, "JCD", Atari, "Atari Jaguar CD"),
        new(13, "Lynx", Atari, "Atari Lynx"),
        new(50, "5200", null, "Atari 5200"),
        new(36, "AST", null, "Atari ST"),

        new(33, "SG1K", Sega, "SG-1000"),
        new(11, "SMS", Sega, "Master System"),
        new(1, "MD", Sega, "Genesis/Mega Drive"),
        new(9, "SCD", Sega, "Sega CD"),
        new(10, "32X", Sega, "32X"),
        new(39, "SAT", Sega, "Saturn"),
        new(40, "DC", Sega, "Dreamcast"),
        new(15, "GG", Sega, "Game Gear"),
        new(68, "Pico", null, "Sega Pico"),

        new(47, "80/88", Nec, "PC-8000/8800"),
        new(8, "PCE", Nec, "PC Engine/TurboGrafx-16"),
        new(76, "PCCD", Nec, "PC Engine CD/TurboGrafx-CD"),
        new(49, "PC-FX", Nec, "PC-FX"),
        new(67, "PC-6000", null, "PC-6000"),
        new(48, "9800", null, "PC-9800"),

        new(56, "NGCD", Snk, "Neo Geo CD"),
        new(14, "NGP", Snk, "Neo Geo Pocket"),

        new(43, "3DO", Others, "3DO Interactive Multiplayer"),
        new(37, "CPC", Others, "Amstrad CPC"),
        new(38, "A2", Others, "Apple II"),
        new(27, "ARC", Others, "Arcade"),
        new(73, "A2001", Others, "Arcadia 2001"),
        new(71, "ARD", Others, "Arduboy"),
        new(44, "CV", Others, "ColecoVision"),
        new(75, "ELEK", Others, "Elektor TV Games Computer"),
        new(57, "CHF", Others, "Fairchild Channel F"),
        new(45, "INTV", Others, "Intellivision"),
        new(74, "VC4000", Others, "Interton VC 4000"),
        new(23, "MO2", Others, "Magnavox Odyssey 2"),
        new(69, "DUCK", Others, "Mega Duck"),
        new(29, "MSX", Others, "MSX"),
        /* {RC_CONSOLE_STANDALONE, "EXE", OTHERS, "Standalone"} - >90, not usable */
        new(80, "UZE", Others, "Uzebox"),
        new(46, "VECT", Others, "Vectrex"),
        new(72, "WASM4", Others, "WASM-4"),
        new(63, "WSV", Others, "Watara Supervision"),
        new(53, "WS", Others, "WonderSwan"),
        new(35, "Amiga", null, "Amiga"),
        new(54, "ECV", null, "Cassette Vision"),
        new(55, "ESCV", null, "Super Cassette Vision"),
        new(30, "C64", null, "Commodore 64"),
        new(58, "FMTowns", null, "FM Towns"),
        new(61, "N-Gage", null, "Nokia N-Gage"),
        new(32, "Oric", null, "Oric"),
        new(42, "CD-i", null, "Philips CD-i"),
        new(64, "X1", null, "Sharp X1"),
        new(52, "X68K", null, "Sharp X68000"),
        new(66, "TO8", null, "Thomson TO8"),
        new(79, "TI83", null, "TI-83"),
        new(65, "TIC-80", null, "TIC-80"),
        new(34, "VIC-20", null, "VIC-20"),
        new(70, "Zeebo", null, "Zeebo"),
        new(31, "ZX81", null, "ZX81"),
        new(59, "ZXS", null, "ZX Spectrum"),
        new(26, "DOS", null, "DOS"),
        new(22, "Xbox", null, "Xbox")
    ];
}
