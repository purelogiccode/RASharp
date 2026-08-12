// Ported from rcheevos (MIT) — include/rc_consoles.h + include/rc_hash.h
// Console identifier constants, values copied verbatim.

namespace RASharp.Core;

/// <summary>Ported from rcheevos (MIT) — include/rc_consoles.h + include/rc_hash.h Console identifier constants, values copied verbatim.</summary>
public static class ConsoleIds
{
    public const uint RC_CONSOLE_UNKNOWN = 0;
    public const uint RC_CONSOLE_MEGA_DRIVE = 1;
    public const uint RC_CONSOLE_NINTENDO_64 = 2;
    public const uint RC_CONSOLE_SUPER_NINTENDO = 3;
    public const uint RC_CONSOLE_GAMEBOY = 4;
    public const uint RC_CONSOLE_GAMEBOY_ADVANCE = 5;
    public const uint RC_CONSOLE_GAMEBOY_COLOR = 6;
    public const uint RC_CONSOLE_NINTENDO = 7;
    public const uint RC_CONSOLE_PC_ENGINE = 8;
    public const uint RC_CONSOLE_SEGA_CD = 9;
    public const uint RC_CONSOLE_SEGA_32X = 10;
    public const uint RC_CONSOLE_MASTER_SYSTEM = 11;
    public const uint RC_CONSOLE_PLAYSTATION = 12;
    public const uint RC_CONSOLE_ATARI_LYNX = 13;
    public const uint RC_CONSOLE_NEOGEO_POCKET = 14;
    public const uint RC_CONSOLE_GAME_GEAR = 15;
    public const uint RC_CONSOLE_GAMECUBE = 16;
    public const uint RC_CONSOLE_ATARI_JAGUAR = 17;
    public const uint RC_CONSOLE_NINTENDO_DS = 18;
    public const uint RC_CONSOLE_WII = 19;
    public const uint RC_CONSOLE_WII_U = 20;
    public const uint RC_CONSOLE_PLAYSTATION_2 = 21;
    public const uint RC_CONSOLE_XBOX = 22;
    public const uint RC_CONSOLE_MAGNAVOX_ODYSSEY2 = 23;
    public const uint RC_CONSOLE_POKEMON_MINI = 24;
    public const uint RC_CONSOLE_ATARI_2600 = 25;
    public const uint RC_CONSOLE_MS_DOS = 26;
    public const uint RC_CONSOLE_ARCADE = 27;
    public const uint RC_CONSOLE_VIRTUAL_BOY = 28;
    public const uint RC_CONSOLE_MSX = 29;
    public const uint RC_CONSOLE_COMMODORE_64 = 30;
    public const uint RC_CONSOLE_ZX81 = 31;
    public const uint RC_CONSOLE_ORIC = 32;
    public const uint RC_CONSOLE_SG1000 = 33;
    public const uint RC_CONSOLE_VIC20 = 34;
    public const uint RC_CONSOLE_AMIGA = 35;
    public const uint RC_CONSOLE_ATARI_ST = 36;
    public const uint RC_CONSOLE_AMSTRAD_PC = 37;
    public const uint RC_CONSOLE_APPLE_II = 38;
    public const uint RC_CONSOLE_SATURN = 39;
    public const uint RC_CONSOLE_DREAMCAST = 40;
    public const uint RC_CONSOLE_PSP = 41;
    public const uint RC_CONSOLE_CDI = 42;
    public const uint RC_CONSOLE_3DO = 43;
    public const uint RC_CONSOLE_COLECOVISION = 44;
    public const uint RC_CONSOLE_INTELLIVISION = 45;
    public const uint RC_CONSOLE_VECTREX = 46;
    public const uint RC_CONSOLE_PC8800 = 47;
    public const uint RC_CONSOLE_PC9800 = 48;
    public const uint RC_CONSOLE_PCFX = 49;
    public const uint RC_CONSOLE_ATARI_5200 = 50;
    public const uint RC_CONSOLE_ATARI_7800 = 51;
    public const uint RC_CONSOLE_X68K = 52;
    public const uint RC_CONSOLE_WONDERSWAN = 53;
    public const uint RC_CONSOLE_CASSETTEVISION = 54;
    public const uint RC_CONSOLE_SUPER_CASSETTEVISION = 55;
    public const uint RC_CONSOLE_NEO_GEO_CD = 56;
    public const uint RC_CONSOLE_FAIRCHILD_CHANNEL_F = 57;
    public const uint RC_CONSOLE_FM_TOWNS = 58;
    public const uint RC_CONSOLE_ZX_SPECTRUM = 59;
    public const uint RC_CONSOLE_GAME_AND_WATCH = 60;
    public const uint RC_CONSOLE_NOKIA_NGAGE = 61;
    public const uint RC_CONSOLE_NINTENDO_3DS = 62;
    public const uint RC_CONSOLE_SUPERVISION = 63;
    public const uint RC_CONSOLE_SHARPX1 = 64;
    public const uint RC_CONSOLE_TIC80 = 65;
    public const uint RC_CONSOLE_THOMSONTO8 = 66;
    public const uint RC_CONSOLE_PC6000 = 67;
    public const uint RC_CONSOLE_PICO = 68;
    public const uint RC_CONSOLE_MEGADUCK = 69;
    public const uint RC_CONSOLE_ZEEBO = 70;
    public const uint RC_CONSOLE_ARDUBOY = 71;
    public const uint RC_CONSOLE_WASM4 = 72;
    public const uint RC_CONSOLE_ARCADIA_2001 = 73;
    public const uint RC_CONSOLE_INTERTON_VC_4000 = 74;
    public const uint RC_CONSOLE_ELEKTOR_TV_GAMES_COMPUTER = 75;
    public const uint RC_CONSOLE_PC_ENGINE_CD = 76;
    public const uint RC_CONSOLE_ATARI_JAGUAR_CD = 77;
    public const uint RC_CONSOLE_NINTENDO_DSI = 78;
    public const uint RC_CONSOLE_TI83 = 79;
    public const uint RC_CONSOLE_UZEBOX = 80;
    public const uint RC_CONSOLE_FAMICOM_DISK_SYSTEM = 81;

    public const uint RC_CONSOLE_HUBS = 100;
    public const uint RC_CONSOLE_EVENTS = 101;
    public const uint RC_CONSOLE_STANDALONE = 102;

    /* CLI '?' auto-detect uses RC_CONSOLE_MAX + 1 (RAHasher.cpp) */
    public const int RC_CONSOLE_MAX = 90;

    /* rc_hash.h special cd tracks */
    public const uint RC_HASH_CDTRACK_FIRST_DATA = unchecked((uint)-1);
    public const uint RC_HASH_CDTRACK_LAST = unchecked((uint)-2);
    public const uint RC_HASH_CDTRACK_LARGEST = unchecked((uint)-3);
    public const uint RC_HASH_CDTRACK_FIRST_OF_SECOND_SESSION = unchecked((uint)-4);
}
