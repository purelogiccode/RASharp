// Ported from rcheevos (MIT) — include/rc_consoles.h + include/rc_hash.h
// Console identifier constants, values copied verbatim.

namespace RASharp;

/// <summary>Console identifier constants (rc_consoles.h) plus the special CD-track selectors from rc_hash.h. Pass one of these ids to <see cref="RcHash.GenerateFromFile"/> or <see cref="RcHash.GenerateFromBuffer"/> to select the hashing algorithm.</summary>
public static class ConsoleIds
{
    /// <summary>Id for an unknown or unset console (0).</summary>
    public const uint RcConsoleUnknown = 0;

    /// <summary>Id for the Sega Mega Drive / Genesis (1).</summary>
    public const uint RcConsoleMegaDrive = 1;

    /// <summary>Id for the Nintendo 64 (2).</summary>
    public const uint RcConsoleNintendo64 = 2;

    /// <summary>Id for the Super Nintendo / Super Famicom (3).</summary>
    public const uint RcConsoleSuperNintendo = 3;

    /// <summary>Id for the Nintendo Game Boy (4).</summary>
    public const uint RcConsoleGameboy = 4;

    /// <summary>Id for the Nintendo Game Boy Advance (5).</summary>
    public const uint RcConsoleGameboyAdvance = 5;

    /// <summary>Id for the Nintendo Game Boy Color (6).</summary>
    public const uint RcConsoleGameboyColor = 6;

    /// <summary>Id for the Nintendo Entertainment System / Famicom (7).</summary>
    public const uint RcConsoleNintendo = 7;

    /// <summary>Id for the NEC PC Engine / TurboGrafx-16 (8).</summary>
    public const uint RcConsolePcEngine = 8;

    /// <summary>Id for the Sega CD / Mega-CD (9).</summary>
    public const uint RcConsoleSegaCd = 9;

    /// <summary>Id for the Sega 32X (10).</summary>
    public const uint RcConsoleSega32X = 10;

    /// <summary>Id for the Sega Master System (11).</summary>
    public const uint RcConsoleMasterSystem = 11;

    /// <summary>Id for the Sony PlayStation (12).</summary>
    public const uint RcConsolePlaystation = 12;

    /// <summary>Id for the Atari Lynx (13).</summary>
    public const uint RcConsoleAtariLynx = 13;

    /// <summary>Id for the SNK Neo Geo Pocket / Pocket Color (14).</summary>
    public const uint RcConsoleNeogeoPocket = 14;

    /// <summary>Id for the Sega Game Gear (15).</summary>
    public const uint RcConsoleGameGear = 15;

    /// <summary>Id for the Nintendo GameCube (16).</summary>
    public const uint RcConsoleGamecube = 16;

    /// <summary>Id for the Atari Jaguar (17).</summary>
    public const uint RcConsoleAtariJaguar = 17;

    /// <summary>Id for the Nintendo DS (18).</summary>
    public const uint RcConsoleNintendoDs = 18;

    /// <summary>Id for the Nintendo Wii (19).</summary>
    public const uint RcConsoleWii = 19;

    /// <summary>Id for the Nintendo Wii U (20).</summary>
    public const uint RcConsoleWiiU = 20;

    /// <summary>Id for the Sony PlayStation 2 (21).</summary>
    public const uint RcConsolePlaystation2 = 21;

    /// <summary>Id for the Microsoft Xbox (22).</summary>
    public const uint RcConsoleXbox = 22;

    /// <summary>Id for the Magnavox Odyssey 2 (23).</summary>
    public const uint RcConsoleMagnavoxOdyssey2 = 23;

    /// <summary>Id for the Nintendo Pokemon Mini (24).</summary>
    public const uint RcConsolePokemonMini = 24;

    /// <summary>Id for the Atari 2600 (25).</summary>
    public const uint RcConsoleAtari2600 = 25;

    /// <summary>Id for MS-DOS (26).</summary>
    public const uint RcConsoleMsDos = 26;

    /// <summary>Id for the Arcade (27).</summary>
    public const uint RcConsoleArcade = 27;

    /// <summary>Id for the Nintendo Virtual Boy (28).</summary>
    public const uint RcConsoleVirtualBoy = 28;

    /// <summary>Id for the Microsoft MSX (29).</summary>
    public const uint RcConsoleMsx = 29;

    /// <summary>Id for the Commodore 64 (30).</summary>
    public const uint RcConsoleCommodore64 = 30;

    /// <summary>Id for the Sinclair ZX81 (31).</summary>
    public const uint RcConsoleZx81 = 31;

    /// <summary>Id for the Oric 1 / Atmos (32).</summary>
    public const uint RcConsoleOric = 32;

    /// <summary>Id for the Sega SG-1000 (33).</summary>
    public const uint RcConsoleSg1000 = 33;

    /// <summary>Id for the Commodore VIC-20 (34).</summary>
    public const uint RcConsoleVic20 = 34;

    /// <summary>Id for the Commodore Amiga (35).</summary>
    public const uint RcConsoleAmiga = 35;

    /// <summary>Id for the Atari ST (36).</summary>
    public const uint RcConsoleAtariSt = 36;

    /// <summary>Id for the Amstrad CPC (37).</summary>
    public const uint RcConsoleAmstradPc = 37;

    /// <summary>Id for the Apple II (38).</summary>
    public const uint RcConsoleAppleIi = 38;

    /// <summary>Id for the Sega Saturn (39).</summary>
    public const uint RcConsoleSaturn = 39;

    /// <summary>Id for the Sega Dreamcast (40).</summary>
    public const uint RcConsoleDreamcast = 40;

    /// <summary>Id for the Sony PlayStation Portable (41).</summary>
    public const uint RcConsolePsp = 41;

    /// <summary>Id for the Philips CD-i (42).</summary>
    public const uint RcConsoleCdi = 42;

    /// <summary>Id for the 3DO Interactive Multiplayer (43).</summary>
    public const uint RcConsole3Do = 43;

    /// <summary>Id for the ColecoVision (44).</summary>
    public const uint RcConsoleColecovision = 44;

    /// <summary>Id for the Mattel Intellivision (45).</summary>
    public const uint RcConsoleIntellivision = 45;

    /// <summary>Id for the Vectrex (46).</summary>
    public const uint RcConsoleVectrex = 46;

    /// <summary>Id for the NEC PC-8000 / PC-8800 (47).</summary>
    public const uint RcConsolePc8800 = 47;

    /// <summary>Id for the NEC PC-9800 (48).</summary>
    public const uint RcConsolePc9800 = 48;

    /// <summary>Id for the NEC PC-FX (49).</summary>
    public const uint RcConsolePcfx = 49;

    /// <summary>Id for the Atari 5200 (50).</summary>
    public const uint RcConsoleAtari5200 = 50;

    /// <summary>Id for the Atari 7800 (51).</summary>
    public const uint RcConsoleAtari7800 = 51;

    /// <summary>Id for the Sharp X68000 (52).</summary>
    public const uint RcConsoleX68K = 52;

    /// <summary>Id for the Bandai WonderSwan / WonderSwan Color (53).</summary>
    public const uint RcConsoleWonderswan = 53;

    /// <summary>Id for the Epoch Cassette Vision (54).</summary>
    public const uint RcConsoleCassettevision = 54;

    /// <summary>Id for the Epoch Super Cassette Vision (55).</summary>
    public const uint RcConsoleSuperCassettevision = 55;

    /// <summary>Id for the SNK Neo Geo CD (56).</summary>
    public const uint RcConsoleNeoGeoCd = 56;

    /// <summary>Id for the Fairchild Channel F (57).</summary>
    public const uint RcConsoleFairchildChannelF = 57;

    /// <summary>Id for the Fujitsu FM Towns (58).</summary>
    public const uint RcConsoleFmTowns = 58;

    /// <summary>Id for the Sinclair ZX Spectrum (59).</summary>
    public const uint RcConsoleZxSpectrum = 59;

    /// <summary>Id for Game &amp; Watch (60).</summary>
    public const uint RcConsoleGameAndWatch = 60;

    /// <summary>Id for the Nokia N-Gage (61).</summary>
    public const uint RcConsoleNokiaNgage = 61;

    /// <summary>Id for the Nintendo 3DS (62).</summary>
    public const uint RcConsoleNintendo3Ds = 62;

    /// <summary>Id for the Watara Supervision (63).</summary>
    public const uint RcConsoleSupervision = 63;

    /// <summary>Id for the Sharp X1 (64).</summary>
    public const uint RcConsoleSharpx1 = 64;

    /// <summary>Id for TIC-80 (65).</summary>
    public const uint RcConsoleTic80 = 65;

    /// <summary>Id for the Thomson TO8 (66).</summary>
    public const uint RcConsoleThomsonto8 = 66;

    /// <summary>Id for the NEC PC-6000 (67).</summary>
    public const uint RcConsolePc6000 = 67;

    /// <summary>Id for the Sega Pico (68).</summary>
    public const uint RcConsolePico = 68;

    /// <summary>Id for the Mega Duck (69).</summary>
    public const uint RcConsoleMegaduck = 69;

    /// <summary>Id for the Zeebo (70).</summary>
    public const uint RcConsoleZeebo = 70;

    /// <summary>Id for the Arduboy (71).</summary>
    public const uint RcConsoleArduboy = 71;

    /// <summary>Id for WASM-4 (72).</summary>
    public const uint RcConsoleWasm4 = 72;

    /// <summary>Id for the Emerson Arcadia 2001 (73).</summary>
    public const uint RcConsoleArcadia2001 = 73;

    /// <summary>Id for the Interton VC 4000 (74).</summary>
    public const uint RcConsoleIntertonVc4000 = 74;

    /// <summary>Id for the Elektor TV Games Computer (75).</summary>
    public const uint RcConsoleElektorTvGamesComputer = 75;

    /// <summary>Id for the NEC PC Engine CD / TurboGrafx-CD (76).</summary>
    public const uint RcConsolePcEngineCd = 76;

    /// <summary>Id for the Atari Jaguar CD (77).</summary>
    public const uint RcConsoleAtariJaguarCd = 77;

    /// <summary>Id for the Nintendo DSi (78).</summary>
    public const uint RcConsoleNintendoDsi = 78;

    /// <summary>Id for the Texas Instruments TI-83 (79).</summary>
    public const uint RcConsoleTi83 = 79;

    /// <summary>Id for the Uzebox (80).</summary>
    public const uint RcConsoleUzebox = 80;

    /// <summary>Id for the Famicom Disk System (81).</summary>
    public const uint RcConsoleFamicomDiskSystem = 81;

    /// <summary>Id for the RetroAchievements Hubs group (100, not a hashable console).</summary>
    public const uint RcConsoleHubs = 100;

    /// <summary>Id for the RetroAchievements Events group (101, not a hashable console).</summary>
    public const uint RcConsoleEvents = 101;

    /// <summary>Id for the RetroAchievements Standalone group (102, not a hashable console).</summary>
    public const uint RcConsoleStandalone = 102;

    /* CLI '?' auto-detect uses RC_CONSOLE_MAX + 1 (RAHasher.cpp) */
    /// <summary>The largest hashing console id; the CLI's '?' auto-detect uses <c>RcConsoleMax + 1</c>.</summary>
    public const int RcConsoleMax = 90;

    /* rc_hash.h special cd tracks */
    /// <summary>Selector requesting the first data track of a disc (track −1).</summary>
    public const uint RcHashCdtrackFirstData = unchecked((uint)-1);

    /// <summary>Selector requesting the last track of a disc (track −2).</summary>
    public const uint RcHashCdtrackLast = unchecked((uint)-2);

    /// <summary>Selector requesting the largest track of a disc (track −3).</summary>
    public const uint RcHashCdtrackLargest = unchecked((uint)-3);

    /// <summary>Selector requesting the first track of the second session of a multi-session disc (track −4).</summary>
    public const uint RcHashCdtrackFirstOfSecondSession = unchecked((uint)-4);
}