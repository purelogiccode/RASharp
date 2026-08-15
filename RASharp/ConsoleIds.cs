// Ported from rcheevos (MIT) — include/rc_consoles.h + include/rc_hash.h
// Console identifier constants, values copied verbatim.

namespace RASharp;

/// <summary>Ported from rcheevos (MIT) — include/rc_consoles.h + include/rc_hash.h Console identifier constants, values copied verbatim.</summary>
public static class ConsoleIds
{
    public const uint RcConsoleUnknown = 0;
    public const uint RcConsoleMegaDrive = 1;
    public const uint RcConsoleNintendo64 = 2;
    public const uint RcConsoleSuperNintendo = 3;
    public const uint RcConsoleGameboy = 4;
    public const uint RcConsoleGameboyAdvance = 5;
    public const uint RcConsoleGameboyColor = 6;
    public const uint RcConsoleNintendo = 7;
    public const uint RcConsolePcEngine = 8;
    public const uint RcConsoleSegaCd = 9;
    public const uint RcConsoleSega32X = 10;
    public const uint RcConsoleMasterSystem = 11;
    public const uint RcConsolePlaystation = 12;
    public const uint RcConsoleAtariLynx = 13;
    public const uint RcConsoleNeogeoPocket = 14;
    public const uint RcConsoleGameGear = 15;
    public const uint RcConsoleGamecube = 16;
    public const uint RcConsoleAtariJaguar = 17;
    public const uint RcConsoleNintendoDs = 18;
    public const uint RcConsoleWii = 19;
    public const uint RcConsoleWiiU = 20;
    public const uint RcConsolePlaystation2 = 21;
    public const uint RcConsoleXbox = 22;
    public const uint RcConsoleMagnavoxOdyssey2 = 23;
    public const uint RcConsolePokemonMini = 24;
    public const uint RcConsoleAtari2600 = 25;
    public const uint RcConsoleMsDos = 26;
    public const uint RcConsoleArcade = 27;
    public const uint RcConsoleVirtualBoy = 28;
    public const uint RcConsoleMsx = 29;
    public const uint RcConsoleCommodore64 = 30;
    public const uint RcConsoleZx81 = 31;
    public const uint RcConsoleOric = 32;
    public const uint RcConsoleSg1000 = 33;
    public const uint RcConsoleVic20 = 34;
    public const uint RcConsoleAmiga = 35;
    public const uint RcConsoleAtariSt = 36;
    public const uint RcConsoleAmstradPc = 37;
    public const uint RcConsoleAppleIi = 38;
    public const uint RcConsoleSaturn = 39;
    public const uint RcConsoleDreamcast = 40;
    public const uint RcConsolePsp = 41;
    public const uint RcConsoleCdi = 42;
    public const uint RcConsole3Do = 43;
    public const uint RcConsoleColecovision = 44;
    public const uint RcConsoleIntellivision = 45;
    public const uint RcConsoleVectrex = 46;
    public const uint RcConsolePc8800 = 47;
    public const uint RcConsolePc9800 = 48;
    public const uint RcConsolePcfx = 49;
    public const uint RcConsoleAtari5200 = 50;
    public const uint RcConsoleAtari7800 = 51;
    public const uint RcConsoleX68K = 52;
    public const uint RcConsoleWonderswan = 53;
    public const uint RcConsoleCassettevision = 54;
    public const uint RcConsoleSuperCassettevision = 55;
    public const uint RcConsoleNeoGeoCd = 56;
    public const uint RcConsoleFairchildChannelF = 57;
    public const uint RcConsoleFmTowns = 58;
    public const uint RcConsoleZxSpectrum = 59;
    public const uint RcConsoleGameAndWatch = 60;
    public const uint RcConsoleNokiaNgage = 61;
    public const uint RcConsoleNintendo3Ds = 62;
    public const uint RcConsoleSupervision = 63;
    public const uint RcConsoleSharpx1 = 64;
    public const uint RcConsoleTic80 = 65;
    public const uint RcConsoleThomsonto8 = 66;
    public const uint RcConsolePc6000 = 67;
    public const uint RcConsolePico = 68;
    public const uint RcConsoleMegaduck = 69;
    public const uint RcConsoleZeebo = 70;
    public const uint RcConsoleArduboy = 71;
    public const uint RcConsoleWasm4 = 72;
    public const uint RcConsoleArcadia2001 = 73;
    public const uint RcConsoleIntertonVc4000 = 74;
    public const uint RcConsoleElektorTvGamesComputer = 75;
    public const uint RcConsolePcEngineCd = 76;
    public const uint RcConsoleAtariJaguarCd = 77;
    public const uint RcConsoleNintendoDsi = 78;
    public const uint RcConsoleTi83 = 79;
    public const uint RcConsoleUzebox = 80;
    public const uint RcConsoleFamicomDiskSystem = 81;

    public const uint RcConsoleHubs = 100;
    public const uint RcConsoleEvents = 101;
    public const uint RcConsoleStandalone = 102;

    /* CLI '?' auto-detect uses RC_CONSOLE_MAX + 1 (RAHasher.cpp) */
    public const int RcConsoleMax = 90;

    /* rc_hash.h special cd tracks */
    public const uint RcHashCdtrackFirstData = unchecked((uint)-1);
    public const uint RcHashCdtrackLast = unchecked((uint)-2);
    public const uint RcHashCdtrackLargest = unchecked((uint)-3);
    public const uint RcHashCdtrackFirstOfSecondSession = unchecked((uint)-4);
}
