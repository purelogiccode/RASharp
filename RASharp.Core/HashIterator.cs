// Ported from rcheevos (MIT) — src/rhash/hash.c
// The '?' auto-detect iterator API (rc_hash_iterate) and the extension-
// handler table. The table entries and their order are copied verbatim;
// the RcHashIterator / ExtHandlerEntry models live in Models/.

using RASharp.Core.Models;

namespace RASharp.Core;

/// <summary>Ported from rcheevos (MIT) — src/rhash/hash.c The '?' auto-detect iterator API (rc_hash_iterate) and the extension- handler table. The table entries and their o</summary>
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
            var size = HashEngine.FileSize(iterator, iterator.Path!);
            if (size > 32 * 1024 * 1024)
            {
                iterator.Consoles[0] = ConsoleIds.RcConsole3Do; /* 4DO supports directly opening the bin file */
                iterator.Consoles[1] = ConsoleIds.RcConsolePlaystation; /* PCSX ReARMed supports directly opening the bin file*/
                iterator.Consoles[2] = ConsoleIds.RcConsolePlaystation2; /* PCSX2 supports directly opening the bin file*/
                iterator.Consoles[3] = ConsoleIds.RcConsoleSegaCd; /* Genesis Plus GX supports directly opening the bin file*/

                /* fallback to megadrive which just does a full hash. */
                iterator.Consoles[4] = ConsoleIds.RcConsoleMegaDrive;
                return;
            }
        }

        /* bin is associated with MegaDrive, Sega32X, Atari 2600, Watara Supervision, MegaDuck,
         * Fairchild Channel F, Arcadia 2001, Interton VC 4000, and Super Cassette Vision.
         * Since they all use the same hashing algorithm, only specify one of them */
        iterator.Consoles[0] = ConsoleIds.RcConsoleMegaDrive;
    }

    private static void InitializeIteratorChd(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = ConsoleIds.RcConsolePlaystation;
        iterator.Consoles[1] = ConsoleIds.RcConsolePlaystation2;
        iterator.Consoles[2] = ConsoleIds.RcConsoleDreamcast;
        iterator.Consoles[3] = ConsoleIds.RcConsoleSegaCd; /* ASSERT: handles both Sega CD and Saturn */
        iterator.Consoles[4] = ConsoleIds.RcConsolePsp;
        iterator.Consoles[5] = ConsoleIds.RcConsolePcEngineCd;
        iterator.Consoles[6] = ConsoleIds.RcConsole3Do;
        iterator.Consoles[7] = ConsoleIds.RcConsoleNeoGeoCd;
        iterator.Consoles[8] = ConsoleIds.RcConsolePcfx;
    }

    private static void InitializeIteratorCue(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = ConsoleIds.RcConsolePlaystation;
        iterator.Consoles[1] = ConsoleIds.RcConsolePlaystation2;
        iterator.Consoles[2] = ConsoleIds.RcConsoleDreamcast;
        iterator.Consoles[3] = ConsoleIds.RcConsoleSegaCd; /* ASSERT: handles both Sega CD and Saturn */
        iterator.Consoles[4] = ConsoleIds.RcConsolePcEngineCd;
        iterator.Consoles[5] = ConsoleIds.RcConsole3Do;
        iterator.Consoles[6] = ConsoleIds.RcConsolePcfx;
        iterator.Consoles[7] = ConsoleIds.RcConsoleNeoGeoCd;
        iterator.Consoles[8] = ConsoleIds.RcConsoleAtariJaguarCd;
    }

    private static void InitializeIteratorD88(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = ConsoleIds.RcConsolePc8800;
        iterator.Consoles[1] = ConsoleIds.RcConsoleSharpx1;
    }

    private static void InitializeIteratorDsk(RcHashIterator iterator, int data)
    {
        long size = iterator.BufferSize;
        if (size == 0)
        {
            size = HashEngine.FileSize(iterator, iterator.Path!);
        }

        switch (size)
        {
            /* 360KB */
            case 512L * 9 * 80:
            /* 720KB */
            /* FAT-12 3.5" DD double-sided (512 byte sectors, 9 sectors per track, 80 tracks per side */
            case 512L * 9 * 80 * 2:
                /* FAT-12 3.5" DD (512 byte sectors, 9 sectors per track, 80 tracks per side */
                /* FAT-12 5.25" DD double-sided (512 byte sectors, 9 sectors per track, 80 tracks per side */
                iterator.Consoles[0] = ConsoleIds.RcConsoleMsx;
                break;
            /* 180KB */
            case 512L * 9 * 40:
                /* FAT-12 5.25" DD (512 byte sectors, 9 sectors per track, 40 tracks per side */
                iterator.Consoles[0] = ConsoleIds.RcConsoleMsx;

                /* AMSDOS 3" - 40 tracks */
                iterator.Consoles[1] = ConsoleIds.RcConsoleAmstradPc;
                break;
            /* 140KB */
            case 256L * 16 * 35:
            /* 113.75KB */
            /* Apple II old format - 256 byte sectors, 13 sectors per track, 35 tracks per side */
            case 256L * 13 * 35:
                /* Apple II new format - 256 byte sectors, 16 sectors per track, 35 tracks per side */
                iterator.Consoles[0] = ConsoleIds.RcConsoleAppleIi;
                break;
        }

        /* once a best guess has been identified, make sure the others are added as fallbacks */

        /* check MSX first, as Apple II isn't supported by RetroArch, and RAppleWin won't use the iterator */
        AppendConsole(iterator, ConsoleIds.RcConsoleMsx);
        AppendConsole(iterator, ConsoleIds.RcConsoleAmstradPc);
        AppendConsole(iterator, ConsoleIds.RcConsoleZxSpectrum);
        AppendConsole(iterator, ConsoleIds.RcConsoleAppleIi);
    }

    private static void InitializeIteratorIso(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = ConsoleIds.RcConsolePlaystation2;
        iterator.Consoles[1] = ConsoleIds.RcConsolePsp;
        iterator.Consoles[2] = ConsoleIds.RcConsole3Do;
        iterator.Consoles[3] = ConsoleIds.RcConsoleSegaCd; /* ASSERT: handles both Sega CD and Saturn */
        iterator.Consoles[4] = ConsoleIds.RcConsoleGamecube;
        iterator.Consoles[5] = ConsoleIds.RcConsoleWii;
    }

    private static void InitializeIteratorM3U(RcHashIterator iterator, int data)
    {
        /* temporarily read the first disc path out of the playlist. returns an
         * allocated string or NULL, so a failed lookup leaves the path alone */
        var firstFilePath = HashEngine.GetFirstItemFromPlaylist(iterator);
        if (firstFilePath == null) /* did not find a disc */
            return;

        /* release the m3u path and replace with the first file path */
        iterator.Path = firstFilePath;

        iterator.Buffer = null; /* ignore buffer; assume it's the m3u contents */

        InitializeIteratorFromPath(iterator, iterator.Path);
    }

    private static void InitializeIteratorNib(RcHashIterator iterator, int data)
    {
        iterator.Consoles[0] = ConsoleIds.RcConsoleAppleIi;
        iterator.Consoles[1] = ConsoleIds.RcConsoleCommodore64;
    }

    private static void InitializeIteratorRom(RcHashIterator iterator, int data)
    {
        /* rom is associated with MSX, Thomson TO-8, and Fairchild Channel F.
         * Since they all use the same hashing algorithm, only specify one of them */
        iterator.Consoles[0] = ConsoleIds.RcConsoleMsx;
    }

    private static void InitializeIteratorTap(RcHashIterator iterator, int data)
    {
        /* also Oric and ZX Spectrum, but all are full file hashes */
        iterator.Consoles[0] = ConsoleIds.RcConsoleCommodore64;
    }

    private static void AppendConsole(RcHashIterator iterator, uint consoleId)
    {
        var i = 0;
        while (iterator.Consoles[i] != 0)
        {
            if (iterator.Consoles[i] == consoleId)
                return;

            ++i;
        }

        iterator.Consoles[i] = consoleId;
    }

    private static readonly ExtHandlerEntry[] ExtHandlers =
    [
        new("2d", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleSharpx1),
        new("3ds", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo3Ds),
        new("3dsx", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo3Ds),
        new("7z", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleArcade),
        new("83g", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleTi83), /* http://tibasicdev.wikidot.com/file-extensions */
        new("83p", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleTi83),
        new("a26", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleAtari2600),
        new("a78", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleAtari7800),
        new("app", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo3Ds),
        new("arduboy", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleArduboy),
        new("axf", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo3Ds),
        new("bin", InitializeIteratorBin, 0),
        new("bs", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleSuperNintendo),
        new("cart", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleSuperCassettevision),
        new("cas", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleMsx),
        new("cci", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo3Ds),
        new("chd", InitializeIteratorChd, 0),
        new("chf", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleFairchildChannelF),
        new("cia", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo3Ds),
        new("col", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleColecovision),
        new("csw", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleZxSpectrum),
        new("cue", InitializeIteratorCue, 0),
        new("cxi", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo3Ds),
        new("d64", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleCommodore64),
        new("d88", InitializeIteratorD88, 0),
        new("dosz", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleMsDos),
        new("dsk", InitializeIteratorDsk, 0),
        new("elf", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo3Ds),
        new("fd", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleThomsonto8),
        new("fds", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo),
        new("fig", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleSuperNintendo),
        new("gb", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleGameboy),
        new("gba", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleGameboyAdvance),
        new("gbc", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleGameboyColor),
        new("gdi", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleDreamcast),
        new("gg", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleGameGear),
        new("hex", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleArduboy),
        new("iso", InitializeIteratorIso, 0),
        new("jag", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleAtariJaguar),
        new("k7", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleThomsonto8), /* tape */
        new("lnx", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleAtariLynx),
        new("m3u", InitializeIteratorM3U, 0),
        new("m5", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleThomsonto8), /* cartridge */
        new("m7", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleThomsonto8), /* cartridge */
        new("md", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleMegaDrive),
        new("min", InitializeIteratorSingle, (int)ConsoleIds.RcConsolePokemonMini),
        new("mx1", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleMsx),
        new("mx2", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleMsx),
        new("n64", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo64),
        new("ndd", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo64),
        new("nds", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendoDs), /* handles both DS and DSi */
        new("neo", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleArcade), /* Geolith Neo Geo cart format */
        new("nes", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo),
        new("ngc", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNeogeoPocket),
        new("nib", InitializeIteratorNib, 0),
        new("pbp", InitializeIteratorSingle, (int)ConsoleIds.RcConsolePsp),
        new("pce", InitializeIteratorSingle, (int)ConsoleIds.RcConsolePcEngine),
        new("pgm", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleElektorTvGamesComputer),
        new("pzx", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleZxSpectrum),
        new("ri", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleMsx),
        new("rom", InitializeIteratorRom, 0),
        new("sap", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleThomsonto8), /* disk */
        new("scl", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleZxSpectrum),
        new("sfc", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleSuperNintendo),
        new("sg", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleSg1000),
        new("sgx", InitializeIteratorSingle, (int)ConsoleIds.RcConsolePcEngine),
        new("smc", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleSuperNintendo),
        new("sms", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleMasterSystem),
        new("sv", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleSupervision),
        new("swc", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleSuperNintendo),
        new("tap", InitializeIteratorTap, 0),
        new("tic", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleTic80),
        new("trd", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleZxSpectrum),
        new("tvc", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleElektorTvGamesComputer),
        new("tzx", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleZxSpectrum),
        new("uze", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleUzebox),
        new("v64", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo64),
        new("vb", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleVirtualBoy),
        new("wad", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleWii),
        new("wasm", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleWasm4),
        new("woz", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleAppleIi),
        new("wsc", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleWonderswan),
        new("z64", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleNintendo64),
        new("zip", InitializeIteratorSingle, (int)ConsoleIds.RcConsoleArcade)
    ];

    /// <summary>Returns the extension-to-console handler table.</summary>
    /// <param name="numHandlers">the number of handlers in the table</param>
    /// <returns>the result</returns>
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
            var mid = (lo + hi) / 2;
            var cmp = string.CompareOrdinal(ExtHandlers[mid].Ext, key);
            switch (cmp)
            {
                case 0:
                    return ExtHandlers[mid];
                case < 0:
                    lo = mid + 1;
                    break;
                default:
                    hi = mid - 1;
                    break;
            }
        }

        return null;
    }

    /// <summary>initialize iterator from path.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="path">the file path</param>
    public static void InitializeIteratorFromPath(RcHashIterator iterator, string path)
    {
        var ext = HashEngine.PathGetExtension(path);

        /* lowercase the extension for the search */
        var key = ext.ToLowerInvariant();
        if (key.Length > 7)
        {
            key = key.Substring(0, 7);
        }

        ExtHandlerEntry? handler = FindHandler(key);
        if (handler != null)
        {
            handler.Handler(iterator, handler.Data);

            if (iterator.Callbacks.VerboseMessage != null)
            {
                var count = 0;
                while (iterator.Consoles[count] != 0)
                {
                    ++count;
                }

                HashEngine.IteratorVerboseFormatted(iterator, "Found {0} potential consoles for {1} file extension", count, ext);
            }
        }
        else
        {
            HashEngine.IteratorErrorFormatted(iterator, "No console mapping specified for {0} file extension - trying full file hash", ext);

            /* if we didn't match the extension, default to something that does a whole file hash */
            if (iterator.Consoles[0] == 0)
            {
                iterator.Consoles[0] = ConsoleIds.RcConsoleGameboy;
            }
        }
    }

    /// <summary>Initializes an iterator for a path or buffer.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="path">the file path</param>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="bufferSize">the size of the buffer</param>
    public static void InitializeIterator(RcHashIterator iterator, string? path, byte[]? buffer, int bufferSize)
    {
        HashEngine.ResetIterator(iterator);
        iterator.Buffer = buffer;
        iterator.BufferSize = bufferSize;

        if (path != null)
        {
            iterator.Path = path;
        }
    }

    /// <summary>Releases the iterator resources.</summary>
    /// <param name="iterator">the hash iterator</param>
    public static void DestroyIterator(RcHashIterator iterator)
    {
        iterator.Path = null;
        iterator.Buffer = null;
    }

    /// <summary>Walks the handler table and returns the first console that accepts the file.</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>nonzero when a console matched; zero when none did</returns>
    public static int Iterate(out string hash, RcHashIterator iterator)
    {
        var result = 0;
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

            var nextConsole = (int)iterator.Consoles[iterator.Index];
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

    /// <summary>generate.</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int Generate(out string hash, uint consoleId, RcHashIterator iterator)
    {
        if (iterator.Buffer != null)
            return HashEngine.FromBuffer(out hash, consoleId, iterator);

        return HashEngine.FromFile(out hash, consoleId, iterator);
    }
}
