// Ported from rcheevos (MIT) — src/rhash/hash.c
// The '?' auto-detect iterator API (rc_hash_iterate) and the extension-
// handler table. The table entries and their order are copied verbatim;
// the RcHashIterator / ExtHandlerEntry models live in Models/.

using RASharp.Core.Models;

namespace RASharp.Core;

public static class HashIterator
{
    private static void InitializeIteratorSingle(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = (uint)data;
    }

    private static void InitializeIteratorBin(RcHashIterator iterator, int data)
    {
        if (iterator.BufferSize == 0)
        {
            /* raw bin file may be a CD track. if it's more than 32MB, try a CD hash. */
            long size = HashEngine.FileSize(iterator, iterator.Path!);
            if (size > 32 * 1024 * 1024)
            {
                iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_3DO; /* 4DO supports directly opening the bin file */
                iterator.Consoles[1] = ConsoleIds.RC_CONSOLE_PLAYSTATION; /* PCSX ReARMed supports directly opening the bin file*/
                iterator.Consoles[2] = ConsoleIds.RC_CONSOLE_PLAYSTATION_2; /* PCSX2 supports directly opening the bin file*/
                iterator.Consoles[3] = ConsoleIds.RC_CONSOLE_SEGA_CD; /* Genesis Plus GX supports directly opening the bin file*/

                /* fallback to megadrive which just does a full hash. */
                iterator.Consoles[4] = ConsoleIds.RC_CONSOLE_MEGA_DRIVE;
                return;
            }
        }

        /* bin is associated with MegaDrive, Sega32X, Atari 2600, Watara Supervision, MegaDuck,
         * Fairchild Channel F, Arcadia 2001, Interton VC 4000, and Super Cassette Vision.
         * Since they all use the same hashing algorithm, only specify one of them */
        iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_MEGA_DRIVE;
    }

    private static void InitializeIteratorChd(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_PLAYSTATION;
        iterator.Consoles[1] = ConsoleIds.RC_CONSOLE_PLAYSTATION_2;
        iterator.Consoles[2] = ConsoleIds.RC_CONSOLE_DREAMCAST;
        iterator.Consoles[3] = ConsoleIds.RC_CONSOLE_SEGA_CD; /* ASSERT: handles both Sega CD and Saturn */
        iterator.Consoles[4] = ConsoleIds.RC_CONSOLE_PSP;
        iterator.Consoles[5] = ConsoleIds.RC_CONSOLE_PC_ENGINE_CD;
        iterator.Consoles[6] = ConsoleIds.RC_CONSOLE_3DO;
        iterator.Consoles[7] = ConsoleIds.RC_CONSOLE_NEO_GEO_CD;
        iterator.Consoles[8] = ConsoleIds.RC_CONSOLE_PCFX;
    }

    private static void InitializeIteratorCue(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_PLAYSTATION;
        iterator.Consoles[1] = ConsoleIds.RC_CONSOLE_PLAYSTATION_2;
        iterator.Consoles[2] = ConsoleIds.RC_CONSOLE_DREAMCAST;
        iterator.Consoles[3] = ConsoleIds.RC_CONSOLE_SEGA_CD; /* ASSERT: handles both Sega CD and Saturn */
        iterator.Consoles[4] = ConsoleIds.RC_CONSOLE_PC_ENGINE_CD;
        iterator.Consoles[5] = ConsoleIds.RC_CONSOLE_3DO;
        iterator.Consoles[6] = ConsoleIds.RC_CONSOLE_PCFX;
        iterator.Consoles[7] = ConsoleIds.RC_CONSOLE_NEO_GEO_CD;
        iterator.Consoles[8] = ConsoleIds.RC_CONSOLE_ATARI_JAGUAR_CD;
    }

    private static void InitializeIteratorD88(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_PC8800;
        iterator.Consoles[1] = ConsoleIds.RC_CONSOLE_SHARPX1;
    }

    private static void InitializeIteratorDsk(RcHashIterator iterator, int data)
    {
        long size = iterator.BufferSize;
        if (size == 0)
            size = HashEngine.FileSize(iterator, iterator.Path!);

        if (size == 512L * 9 * 80) /* 360KB */
        {
            /* FAT-12 3.5" DD (512 byte sectors, 9 sectors per track, 80 tracks per side */
            /* FAT-12 5.25" DD double-sided (512 byte sectors, 9 sectors per track, 80 tracks per side */
            iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_MSX;
        }
        else if (size == 512L * 9 * 80 * 2) /* 720KB */
        {
            /* FAT-12 3.5" DD double-sided (512 byte sectors, 9 sectors per track, 80 tracks per side */
            iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_MSX;
        }
        else if (size == 512L * 9 * 40) /* 180KB */
        {
            /* FAT-12 5.25" DD (512 byte sectors, 9 sectors per track, 40 tracks per side */
            iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_MSX;

            /* AMSDOS 3" - 40 tracks */
            iterator.Consoles[1] = ConsoleIds.RC_CONSOLE_AMSTRAD_PC;
        }
        else if (size == 256L * 16 * 35) /* 140KB */
        {
            /* Apple II new format - 256 byte sectors, 16 sectors per track, 35 tracks per side */
            iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_APPLE_II;
        }
        else if (size == 256L * 13 * 35) /* 113.75KB */
        {
            /* Apple II old format - 256 byte sectors, 13 sectors per track, 35 tracks per side */
            iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_APPLE_II;
        }

        /* once a best guess has been identified, make sure the others are added as fallbacks */

        /* check MSX first, as Apple II isn't supported by RetroArch, and RAppleWin won't use the iterator */
        AppendConsole(iterator, ConsoleIds.RC_CONSOLE_MSX);
        AppendConsole(iterator, ConsoleIds.RC_CONSOLE_AMSTRAD_PC);
        AppendConsole(iterator, ConsoleIds.RC_CONSOLE_ZX_SPECTRUM);
        AppendConsole(iterator, ConsoleIds.RC_CONSOLE_APPLE_II);
    }

    private static void InitializeIteratorIso(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_PLAYSTATION_2;
        iterator.Consoles[1] = ConsoleIds.RC_CONSOLE_PSP;
        iterator.Consoles[2] = ConsoleIds.RC_CONSOLE_3DO;
        iterator.Consoles[3] = ConsoleIds.RC_CONSOLE_SEGA_CD; /* ASSERT: handles both Sega CD and Saturn */
        iterator.Consoles[4] = ConsoleIds.RC_CONSOLE_GAMECUBE;
        iterator.Consoles[5] = ConsoleIds.RC_CONSOLE_WII;
    }

    private static void InitializeIteratorM3u(RcHashIterator iterator, int data)
    {
        /* temporarily read the first disc path out of the playlist. returns an
         * allocated string or NULL, so a failed lookup leaves the path alone */
        string? firstFilePath = HashEngine.GetFirstItemFromPlaylist(iterator);
        if (firstFilePath == null) /* did not find a disc */
            return;

        /* release the m3u path and replace with the first file path */
        iterator.Path = firstFilePath;

        iterator.Buffer = null; /* ignore buffer; assume it's the m3u contents */

        InitializeIteratorFromPath(iterator, iterator.Path);
    }

    private static void InitializeIteratorNib(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_APPLE_II;
        iterator.Consoles[1] = ConsoleIds.RC_CONSOLE_COMMODORE_64;
    }

    private static void InitializeIteratorRom(RcHashIterator iterator, int data)
    {
        /* rom is associated with MSX, Thomson TO-8, and Fairchild Channel F.
         * Since they all use the same hashing algorithm, only specify one of them */
        iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_MSX;
    }

    private static void InitializeIteratorTap(RcHashIterator iterator, int data)
    {
        /* also Oric and ZX Spectrum, but all are full file hashes */
        iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_COMMODORE_64;
    }

    private static void AppendConsole(RcHashIterator iterator, uint consoleId)
    {
        int i = 0;
        while (iterator.Consoles[i] != 0)
        {
            if (iterator.Consoles[i] == consoleId)
                return;

            ++i;
        }

        iterator.Consoles[i] = consoleId;
    }

    private static readonly ExtHandlerEntry[] ExtHandlers = new[]
    {
        new ExtHandlerEntry("2d", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_SHARPX1),
        new ExtHandlerEntry("3ds", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_3DS),
        new ExtHandlerEntry("3dsx", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_3DS),
        new ExtHandlerEntry("7z", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ARCADE),
        new ExtHandlerEntry("83g", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_TI83), /* http://tibasicdev.wikidot.com/file-extensions */
        new ExtHandlerEntry("83p", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_TI83),
        new ExtHandlerEntry("a26", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ATARI_2600),
        new ExtHandlerEntry("a78", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ATARI_7800),
        new ExtHandlerEntry("app", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_3DS),
        new ExtHandlerEntry("arduboy", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ARDUBOY),
        new ExtHandlerEntry("axf", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_3DS),
        new ExtHandlerEntry("bin", InitializeIteratorBin, 0),
        new ExtHandlerEntry("bs", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_SUPER_NINTENDO),
        new ExtHandlerEntry("cart", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_SUPER_CASSETTEVISION),
        new ExtHandlerEntry("cas", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_MSX),
        new ExtHandlerEntry("cci", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_3DS),
        new ExtHandlerEntry("chd", InitializeIteratorChd, 0),
        new ExtHandlerEntry("chf", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_FAIRCHILD_CHANNEL_F),
        new ExtHandlerEntry("cia", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_3DS),
        new ExtHandlerEntry("col", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_COLECOVISION),
        new ExtHandlerEntry("csw", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ZX_SPECTRUM),
        new ExtHandlerEntry("cue", InitializeIteratorCue, 0),
        new ExtHandlerEntry("cxi", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_3DS),
        new ExtHandlerEntry("d64", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_COMMODORE_64),
        new ExtHandlerEntry("d88", InitializeIteratorD88, 0),
        new ExtHandlerEntry("dosz", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_MS_DOS),
        new ExtHandlerEntry("dsk", InitializeIteratorDsk, 0),
        new ExtHandlerEntry("elf", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_3DS),
        new ExtHandlerEntry("fd", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_THOMSONTO8),
        new ExtHandlerEntry("fds", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO),
        new ExtHandlerEntry("fig", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_SUPER_NINTENDO),
        new ExtHandlerEntry("gb", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_GAMEBOY),
        new ExtHandlerEntry("gba", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_GAMEBOY_ADVANCE),
        new ExtHandlerEntry("gbc", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_GAMEBOY_COLOR),
        new ExtHandlerEntry("gdi", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_DREAMCAST),
        new ExtHandlerEntry("gg", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_GAME_GEAR),
        new ExtHandlerEntry("hex", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ARDUBOY),
        new ExtHandlerEntry("iso", InitializeIteratorIso, 0),
        new ExtHandlerEntry("jag", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ATARI_JAGUAR),
        new ExtHandlerEntry("k7", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_THOMSONTO8), /* tape */
        new ExtHandlerEntry("lnx", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ATARI_LYNX),
        new ExtHandlerEntry("m3u", InitializeIteratorM3u, 0),
        new ExtHandlerEntry("m5", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_THOMSONTO8), /* cartridge */
        new ExtHandlerEntry("m7", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_THOMSONTO8), /* cartridge */
        new ExtHandlerEntry("md", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_MEGA_DRIVE),
        new ExtHandlerEntry("min", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_POKEMON_MINI),
        new ExtHandlerEntry("mx1", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_MSX),
        new ExtHandlerEntry("mx2", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_MSX),
        new ExtHandlerEntry("n64", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_64),
        new ExtHandlerEntry("ndd", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_64),
        new ExtHandlerEntry("nds", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_DS), /* handles both DS and DSi */
        new ExtHandlerEntry("neo", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ARCADE), /* Geolith Neo Geo cart format */
        new ExtHandlerEntry("nes", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO),
        new ExtHandlerEntry("ngc", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NEOGEO_POCKET),
        new ExtHandlerEntry("nib", InitializeIteratorNib, 0),
        new ExtHandlerEntry("pbp", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_PSP),
        new ExtHandlerEntry("pce", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_PC_ENGINE),
        new ExtHandlerEntry("pgm", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ELEKTOR_TV_GAMES_COMPUTER),
        new ExtHandlerEntry("pzx", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ZX_SPECTRUM),
        new ExtHandlerEntry("ri", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_MSX),
        new ExtHandlerEntry("rom", InitializeIteratorRom, 0),
        new ExtHandlerEntry("sap", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_THOMSONTO8), /* disk */
        new ExtHandlerEntry("scl", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ZX_SPECTRUM),
        new ExtHandlerEntry("sfc", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_SUPER_NINTENDO),
        new ExtHandlerEntry("sg", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_SG1000),
        new ExtHandlerEntry("sgx", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_PC_ENGINE),
        new ExtHandlerEntry("smc", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_SUPER_NINTENDO),
        new ExtHandlerEntry("sms", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_MASTER_SYSTEM),
        new ExtHandlerEntry("sv", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_SUPERVISION),
        new ExtHandlerEntry("swc", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_SUPER_NINTENDO),
        new ExtHandlerEntry("tap", InitializeIteratorTap, 0),
        new ExtHandlerEntry("tic", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_TIC80),
        new ExtHandlerEntry("trd", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ZX_SPECTRUM),
        new ExtHandlerEntry("tvc", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ELEKTOR_TV_GAMES_COMPUTER),
        new ExtHandlerEntry("tzx", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ZX_SPECTRUM),
        new ExtHandlerEntry("uze", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_UZEBOX),
        new ExtHandlerEntry("v64", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_64),
        new ExtHandlerEntry("vb", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_VIRTUAL_BOY),
        new ExtHandlerEntry("wad", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_WII),
        new ExtHandlerEntry("wasm", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_WASM4),
        new ExtHandlerEntry("woz", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_APPLE_II),
        new ExtHandlerEntry("wsc", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_WONDERSWAN),
        new ExtHandlerEntry("z64", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_NINTENDO_64),
        new ExtHandlerEntry("zip", InitializeIteratorSingle, (int)ConsoleIds.RC_CONSOLE_ARCADE),
    };

    public static ExtHandlerEntry[] GetIteratorExtHandlers(out int numHandlers)
    {
        numHandlers = ExtHandlers.Length;
        return ExtHandlers;
    }

    /* mirror of bsearch on the sorted table; keys are the lowercased extension
     * capped at 7 chars, matching the 8-byte (7 + NUL) search struct in C */
    private static ExtHandlerEntry? FindHandler(string key)
    {
        int lo = 0, hi = ExtHandlers.Length - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            int cmp = string.CompareOrdinal(ExtHandlers[mid].Ext, key);
            if (cmp == 0)
                return ExtHandlers[mid];
            if (cmp < 0)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return null;
    }

    public static void InitializeIteratorFromPath(RcHashIterator iterator, string path)
    {
        string ext = HashEngine.PathGetExtension(path);

        /* lowercase the extension for the search */
        string key = ext.ToLowerInvariant();
        if (key.Length > 7)
            key = key.Substring(0, 7);

        ExtHandlerEntry? handler = FindHandler(key);
        if (handler != null)
        {
            handler.Handler(iterator, handler.Data);

            if (iterator.Callbacks.VerboseMessage != null)
            {
                int count = 0;
                while (iterator.Consoles[count] != 0)
                    ++count;

                HashEngine.IteratorVerboseFormatted(iterator, "Found {0} potential consoles for {1} file extension", count, ext);
            }
        }
        else
        {
            HashEngine.IteratorErrorFormatted(iterator, "No console mapping specified for {0} file extension - trying full file hash", ext);

            /* if we didn't match the extension, default to something that does a whole file hash */
            if (iterator.Consoles[0] == 0)
                iterator.Consoles[0] = ConsoleIds.RC_CONSOLE_GAMEBOY;
        }
    }

    public static void InitializeIterator(RcHashIterator iterator, string? path, byte[]? buffer, int bufferSize)
    {
        HashEngine.ResetIterator(iterator);
        iterator.Buffer = buffer;
        iterator.BufferSize = bufferSize;

        if (path != null)
            iterator.Path = path;
    }

    public static void DestroyIterator(RcHashIterator iterator)
    {
        iterator.Path = null;
        iterator.Buffer = null;
    }

    public static int Iterate(out string hash, RcHashIterator iterator)
    {
        int result = 0;
        hash = "";

        if (iterator.Index == -1)
        {
            InitializeIteratorFromPath(iterator, iterator.Path!);
            iterator.Index = 0;
        }

        do
        {
            if (iterator.Index >= iterator.Consoles.Length)
                break;

            int nextConsole = (int)iterator.Consoles[iterator.Index];
            if (nextConsole == 0)
            {
                hash = "";
                break;
            }

            ++iterator.Index;

            HashEngine.IteratorVerboseFormatted(iterator, "Trying console {0}", nextConsole);

            result = Generate(out hash, (uint)nextConsole, iterator);
        } while (result == 0);

        return result;
    }

    public static int Generate(out string hash, uint consoleId, RcHashIterator iterator)
    {
        if (iterator.Buffer != null)
            return HashEngine.FromBuffer(out hash, consoleId, iterator);

        return HashEngine.FromFile(out hash, consoleId, iterator);
    }
}
