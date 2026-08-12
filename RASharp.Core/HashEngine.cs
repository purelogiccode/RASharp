// Ported from rcheevos (MIT) — src/rhash/hash.c
// Engine internals: message callbacks, filereader plumbing, whole-file /
// buffered-file / playlist hashing, and the console dispatch tables.
// Control flow, constants, and special cases are translated 1:1; do not
// "improve" behavior — parity is the requirement.

using System.Text;

namespace RASharp.Core;


using RASharp.Core.Models;
public static class HashEngine
{
    /* arbitrary limit to prevent allocating and hashing large files */
    public const long MAX_BUFFER_SIZE = 64 * 1024 * 1024;

    public const int SEEK_SET = 0;
    public const int SEEK_CUR = 1;
    public const int SEEK_END = 2;

    /* ===================================================== */
    /* message callbacks (hash.c statics)                    */

    private static RcHashMessageCallbackDeprecated? g_error_message_callback;
    private static RcHashMessageCallbackDeprecated? g_verbose_message_callback;

    private static void CallGErrorMessageCallback(string message, RcHashIterator? iterator)
    {
        g_error_message_callback!(message);
    }

    private static void CallGVerboseMessageCallback(string message, RcHashIterator? iterator)
    {
        g_verbose_message_callback!(message);
    }

    private static RcHashMessageCallback? GetErrorMessageCallback(RcHashCallbacks callbacks)
    {
        if (callbacks.ErrorMessage != null)
            return callbacks.ErrorMessage;

        if (g_error_message_callback != null)
            return CallGErrorMessageCallback;

        if (callbacks.VerboseMessage != null)
            return callbacks.VerboseMessage;

        if (g_verbose_message_callback != null)
            return CallGVerboseMessageCallback;

        return null;
    }

    public static void HashInitErrorMessageCallback(RcHashMessageCallbackDeprecated? callback)
    {
        g_error_message_callback = callback;
    }

    /* for Hash3DS (the C calls rhash_log_error_message directly) */
    public static void CallErrorMessage(string message)
    {
        if (g_error_message_callback != null)
            g_error_message_callback(message);
    }

    public static void HashInitVerboseMessageCallback(RcHashMessageCallbackDeprecated? callback)
    {
        g_verbose_message_callback = callback;
    }

    public static int IteratorError(RcHashIterator iterator, string message)
    {
        GetErrorMessageCallback(iterator.Callbacks)?.Invoke(message, iterator);
        return 0;
    }

    public static int IteratorErrorFormatted(RcHashIterator iterator, string format, params object?[] args)
    {
        GetErrorMessageCallback(iterator.Callbacks)?.Invoke(string.Format(format, args), iterator);
        return 0;
    }

    public static void IteratorVerbose(RcHashIterator iterator, string message)
    {
        if (iterator.Callbacks.VerboseMessage != null)
            iterator.Callbacks.VerboseMessage(message, iterator);
        else if (g_verbose_message_callback != null)
            g_verbose_message_callback(message);
    }

    public static void IteratorVerboseFormatted(RcHashIterator iterator, string format, params object?[] args)
    {
        string message = string.Format(format, args);
        if (iterator.Callbacks.VerboseMessage != null)
            iterator.Callbacks.VerboseMessage(message, iterator);
        else if (g_verbose_message_callback != null)
            g_verbose_message_callback(message);
    }

    /* ===================================================== */
    /* default filereader (hash.c filereader_*)              */

    private static object? FileReaderOpen(string path)
    {
        return FileUtil.OpenFile(path);
    }

    private static void FileReaderSeek(object fileHandle, long offset, int origin)
    {
        var fs = (FileStream)fileHandle;
        switch (origin)
        {
            case SEEK_SET: fs.Seek(offset, SeekOrigin.Begin); break;
            case SEEK_CUR: fs.Seek(offset, SeekOrigin.Current); break;
            case SEEK_END: fs.Seek(offset, SeekOrigin.End); break;
        }
    }

    private static long FileReaderTell(object fileHandle)
    {
        return ((FileStream)fileHandle).Position;
    }

    private static int FileReaderRead(object fileHandle, byte[] buffer, int requestedBytes)
    {
        return ((FileStream)fileHandle).Read(buffer, 0, requestedBytes);
    }

    private static void FileReaderClose(object fileHandle)
    {
        ((FileStream)fileHandle).Dispose();
    }

    /* for unit tests - normally would call InitCustomFilereader(null) */
    public static void ResetFilereader()
    {
        g_filereader = null;
    }

    private static RcHashFilereader? g_filereader;

    public static void InitCustomFilereader(RcHashFilereader? reader)
    {
        /* initialize with defaults first */
        var funcs = new RcHashFilereader
        {
            Open = FileReaderOpen,
            Seek = FileReaderSeek,
            Tell = FileReaderTell,
            Read = FileReaderRead,
            Close = FileReaderClose,
        };

        /* hook up any provided custom handlers */
        if (reader != null)
        {
            if (reader.Open != null) funcs.Open = reader.Open;
            if (reader.Seek != null) funcs.Seek = reader.Seek;
            if (reader.Tell != null) funcs.Tell = reader.Tell;
            if (reader.Read != null) funcs.Read = reader.Read;
            if (reader.Close != null) funcs.Close = reader.Close;
        }

        g_filereader = funcs;
    }

    /* ===================================================== */
    /* cdreader + encryption globals (hash_disc.c / hash_encrypted.c) */

    private static RcHashCdreader? g_cdreader;

    public static void ResetIteratorDisc(RcHashIterator iterator)
    {
        if (g_cdreader != null)
            iterator.Callbacks.Cdreader = g_cdreader;
        else
            GetDefaultCdreader(iterator.Callbacks.Cdreader);
    }

    public static void InitCustomCdreader(RcHashCdreader? reader)
    {
        if (reader != null)
        {
            g_cdreader = reader;
        }
        else
        {
            g_cdreader = null;
        }
    }

    /* for HashDisc's rc_cd_* fallbacks (the C keeps g_cdreader in hash_disc.c) */
    internal static RcHashCdreader? GetGlobalCdreader()
    {
        return g_cdreader;
    }

    /* default cdreader handlers (cdreader.c port) */
    public static void GetDefaultCdreader(RcHashCdreader cdreader)
    {
        CdReader.GetDefaultCdreader(cdreader);
    }

    private static RcHash3DsGetCiaNormalKeyFunc? g_3ds_cia_normal_key_func;
    private static RcHash3DsGetNcchNormalKeysFunc? g_3ds_ncch_normal_keys_func;

    public static void ResetIteratorEncrypted(RcHashIterator iterator)
    {
        iterator.Callbacks.Encryption.Get3DsCiaNormalKey = g_3ds_cia_normal_key_func;
        iterator.Callbacks.Encryption.Get3DsNcchNormalKeys = g_3ds_ncch_normal_keys_func;
    }

    public static void HashInit3DsGetCiaNormalKeyFunc(RcHash3DsGetCiaNormalKeyFunc func)
    {
        g_3ds_cia_normal_key_func = func;
    }

    public static void HashInit3DsGetNcchNormalKeysFunc(RcHash3DsGetNcchNormalKeysFunc func)
    {
        g_3ds_ncch_normal_keys_func = func;
    }

    /* ===================================================== */
    /* rc_file_* wrappers                                    */

    public static object? FileOpen(RcHashIterator iterator, string path)
    {
        object? handle = null;

        if (iterator.Callbacks.Filereader.Open == null)
        {
            IteratorError(iterator, "No callback registered for opening files");
        }
        else
        {
            handle = iterator.Callbacks.Filereader.Open(path);
            if (handle != null)
                IteratorVerboseFormatted(iterator, "Opened {0}", PathGetFilename(path));
        }

        return handle;
    }

    public static void FileSeek(RcHashIterator iterator, object fileHandle, long offset, int origin)
    {
        if (iterator.Callbacks.Filereader.Seek != null)
            iterator.Callbacks.Filereader.Seek(fileHandle, offset, origin);
    }

    public static long FileTell(RcHashIterator iterator, object fileHandle)
    {
        return iterator.Callbacks.Filereader.Tell != null ? iterator.Callbacks.Filereader.Tell(fileHandle) : 0;
    }

    public static int FileRead(RcHashIterator iterator, object fileHandle, byte[] buffer, int requestedBytes)
    {
        return iterator.Callbacks.Filereader.Read != null ? iterator.Callbacks.Filereader.Read(fileHandle, buffer, requestedBytes) : 0;
    }

    public static void FileClose(RcHashIterator iterator, object fileHandle)
    {
        if (iterator.Callbacks.Filereader.Close != null)
            iterator.Callbacks.Filereader.Close(fileHandle);
    }

    public static long FileSize(RcHashIterator iterator, string path)
    {
        long size = 0;

        /* don't use FileOpen to avoid log statements */
        if (iterator.Callbacks.Filereader.Open == null)
        {
            IteratorError(iterator, "No callback registered for opening files");
        }
        else
        {
            object? handle = iterator.Callbacks.Filereader.Open(path);
            if (handle != null)
            {
                FileSeek(iterator, handle, 0, SEEK_END);
                size = FileTell(iterator, handle);
                FileClose(iterator, handle);
            }
        }

        return size;
    }

    /* ===================================================== */
    /* path helpers                                          */

    public static string PathGetFilename(string path)
    {
        int ptr = path.Length;
        while (ptr > 0)
        {
            if (path[ptr - 1] == '/' || path[ptr - 1] == '\\')
                break;

            --ptr;
        }

        return path.Substring(ptr);
    }

    public static string PathGetExtension(string path)
    {
        int ptr = path.Length;
        while (ptr > 0)
        {
            if (path[ptr - 1] == '.')
                return path.Substring(ptr);

            --ptr;
        }

        return "";
    }

    public static int PathCompareExtension(string path, string ext)
    {
        int pathLen = path.Length;
        int extLen = ext.Length;
        if (extLen > pathLen)
            return 0;

        int ptr = pathLen - extLen;
        if (ptr == 0 || path[ptr - 1] != '.')
            return 0;

        if (string.CompareOrdinal(path, ptr, ext, 0, extLen) == 0)
            return 1;

        for (int i = 0; i < extLen; ++i)
        {
            if (char.ToLowerInvariant(path[ptr + i]) != ext[i])
                return 0;
        }

        return 1;
    }

    /* ===================================================== */
    /* byteswap helpers (used by ROM/disc code)              */

    public static void Byteswap16(byte[] buffer, int count)
    {
        int ptr = 0;
        while (ptr + 4 <= count)
        {
            uint temp = (uint)(buffer[ptr] | (buffer[ptr + 1] << 8) | (buffer[ptr + 2] << 16) | (buffer[ptr + 3] << 24));
            temp = ((temp & 0xFF00FF00) >> 8) | ((temp & 0x00FF00FF) << 8);
            buffer[ptr] = (byte)temp;
            buffer[ptr + 1] = (byte)(temp >> 8);
            buffer[ptr + 2] = (byte)(temp >> 16);
            buffer[ptr + 3] = (byte)(temp >> 24);
            ptr += 4;
        }
    }

    public static void Byteswap32(byte[] buffer, int count)
    {
        int ptr = 0;
        while (ptr + 4 <= count)
        {
            uint temp = (uint)(buffer[ptr] | (buffer[ptr + 1] << 8) | (buffer[ptr + 2] << 16) | (buffer[ptr + 3] << 24));
            temp = ((temp & 0xFF000000) >> 24) | ((temp & 0x00FF0000) >> 8) | ((temp & 0x0000FF00) << 8) | ((temp & 0x000000FF) << 24);
            buffer[ptr] = (byte)temp;
            buffer[ptr + 1] = (byte)(temp >> 8);
            buffer[ptr + 2] = (byte)(temp >> 16);
            buffer[ptr + 3] = (byte)(temp >> 24);
            ptr += 4;
        }
    }

    /* ===================================================== */

    public static int Finalize(RcHashIterator iterator, HashMd5 md5, out string hash)
    {
        byte[] digest = md5.Finish();

        hash = Convert.ToHexStringLower(digest);

        IteratorVerboseFormatted(iterator, "Generated hash {0}", hash);

        return 1;
    }

    /* rc_hash_buffer — hashes buffer[offset .. offset + bufferSize) */
    public static int HashBuffer(out string hash, byte[] buffer, int offset, int bufferSize, RcHashIterator iterator)
    {
        var md5 = new HashMd5();

        if (bufferSize > MAX_BUFFER_SIZE)
            bufferSize = (int)MAX_BUFFER_SIZE;

        md5.Append(buffer, offset, bufferSize);

        IteratorVerboseFormatted(iterator, "Hashing {0} byte buffer", (uint)bufferSize);

        return Finalize(iterator, md5, out hash);
    }

    public static int HashBuffer(out string hash, byte[] buffer, int bufferSize, RcHashIterator iterator)
    {
        return HashBuffer(out hash, buffer, 0, bufferSize, iterator);
    }

    /* ===================================================== */
    /* buffered-file filereader (rc_buffered_file)           */

    private sealed class BufferedFileState
    {
        public byte[]? Data;
        public int ReadPtr;
        public long DataSize;
    }

    private sealed class BufferedFileHandle
    {
        public byte[]? Data;
        public int ReadPtr;
        public long DataSize;
    }

    private static readonly BufferedFileState s_bufferedFile = new();

    private static RcHashFilereader CreateBufferedFileReader()
    {
        return new RcHashFilereader
        {
            Open = path =>
            {
                return new BufferedFileHandle
                {
                    Data = s_bufferedFile.Data,
                    ReadPtr = s_bufferedFile.ReadPtr,
                    DataSize = s_bufferedFile.DataSize,
                };
            },
            Seek = (fileHandle, offset, origin) =>
            {
                var bufferedFile = (BufferedFileHandle)fileHandle;
                switch (origin)
                {
                    case SEEK_SET: bufferedFile.ReadPtr = (int)offset; break;
                    case SEEK_CUR: bufferedFile.ReadPtr += (int)offset; break;
                    case SEEK_END: bufferedFile.ReadPtr = (int)(bufferedFile.DataSize + offset); break;
                }

                if (bufferedFile.ReadPtr < 0)
                    bufferedFile.ReadPtr = 0;
                else if (bufferedFile.ReadPtr > bufferedFile.DataSize)
                    bufferedFile.ReadPtr = (int)bufferedFile.DataSize;
            },
            Tell = fileHandle => ((BufferedFileHandle)fileHandle).ReadPtr,
            Read = (fileHandle, buffer, requestedBytes) =>
            {
                var bufferedFile = (BufferedFileHandle)fileHandle;
                long remaining = bufferedFile.DataSize - bufferedFile.ReadPtr;
                if (requestedBytes > remaining)
                    requestedBytes = (int)remaining;

                if (requestedBytes > 0)
                {
                    Array.Copy(bufferedFile.Data!, bufferedFile.ReadPtr, buffer, 0, requestedBytes);
                    bufferedFile.ReadPtr += requestedBytes;
                }

                return requestedBytes;
            },
            Close = _ => { },
        };
    }

    private static RcHashCallbacks CopyCallbacks(RcHashCallbacks callbacks)
    {
        return new RcHashCallbacks
        {
            VerboseMessage = callbacks.VerboseMessage,
            ErrorMessage = callbacks.ErrorMessage,
            Filereader = callbacks.Filereader,
            Cdreader = callbacks.Cdreader,
            Encryption = callbacks.Encryption,
        };
    }

    private static int FileFromBuffer(out string hash, uint consoleId, RcHashIterator iterator)
    {
        var bufferedFileIterator = new RcHashIterator
        {
            Callbacks = CopyCallbacks(iterator.Callbacks),
            Userdata = iterator.Userdata,
            Path = "memory stream",
        };

        bufferedFileIterator.Callbacks.Filereader = CreateBufferedFileReader();

        s_bufferedFile.Data = iterator.Buffer;
        s_bufferedFile.ReadPtr = 0;
        s_bufferedFile.DataSize = iterator.BufferSize;

        return FromFile(out hash, consoleId, bufferedFileIterator);
    }

    /* ===================================================== */
    /* whole-file / buffered-file hashing                    */

    public static int WholeFile(out string hash, RcHashIterator iterator)
    {
        var md5 = new HashMd5();
        byte[] buffer = new byte[65536];
        long size;
        long remaining;
        int result = 0;
        hash = "";

        object? fileHandle = FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return IteratorError(iterator, "Could not open file");

        FileSeek(iterator, fileHandle, 0, SEEK_END);
        size = FileTell(iterator, fileHandle);

        if (size > MAX_BUFFER_SIZE)
        {
            IteratorVerboseFormatted(iterator, "Hashing first {0} bytes (of {1} bytes) of {2}", (uint)MAX_BUFFER_SIZE, (uint)size, PathGetFilename(iterator.Path!));
            remaining = MAX_BUFFER_SIZE;
        }
        else
        {
            IteratorVerboseFormatted(iterator, "Hashing {0} ({1} bytes)", PathGetFilename(iterator.Path!), (uint)size);
            remaining = size;
        }

        FileSeek(iterator, fileHandle, 0, SEEK_SET);
        while (remaining >= buffer.Length)
        {
            FileRead(iterator, fileHandle, buffer, buffer.Length);
            md5.Append(buffer, buffer.Length);
            remaining -= buffer.Length;
        }

        if (remaining > 0)
        {
            FileRead(iterator, fileHandle, buffer, (int)remaining);
            md5.Append(buffer, (int)remaining);
        }

        result = Finalize(iterator, md5, out hash);

        FileClose(iterator, fileHandle);
        return result;
    }

    public static int BufferedFile(out string hash, uint consoleId, RcHashIterator iterator)
    {
        int result = 0;
        hash = "";

        object? fileHandle = FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return IteratorError(iterator, "Could not open file");

        FileSeek(iterator, fileHandle, 0, SEEK_END);
        long size = FileTell(iterator, fileHandle);

        if (size > MAX_BUFFER_SIZE)
        {
            IteratorVerboseFormatted(iterator, "Buffering first {0} bytes (of {1} bytes) of {2}", (uint)MAX_BUFFER_SIZE, (uint)size, PathGetFilename(iterator.Path!));
            size = MAX_BUFFER_SIZE;
        }
        else
        {
            IteratorVerboseFormatted(iterator, "Buffering {0} ({1} bytes)", PathGetFilename(iterator.Path!), (uint)size);
        }

        byte[] buffer = new byte[(int)size];

        var bufferIterator = new RcHashIterator
        {
            Callbacks = CopyCallbacks(iterator.Callbacks),
            Userdata = iterator.Userdata,
            Path = iterator.Path,
            Buffer = buffer,
            BufferSize = (int)size,
        };

        FileSeek(iterator, fileHandle, 0, SEEK_SET);
        FileRead(iterator, fileHandle, buffer, (int)size);

        result = FromBuffer(out hash, consoleId, bufferIterator);

        FileClose(iterator, fileHandle);
        return result;
    }

    /* ===================================================== */
    /* playlist (m3u) handling                               */

    private static bool IsPathAbsolute(string path)
    {
        if (path.Length == 0)
            return false;

        /* "/path/to/file" or "\path\to\file" */
        if (path[0] == '/' || path[0] == '\\')
            return true;

        /* "C:\path\to\file" */
        if (path.Length > 2 && path[1] == ':' && path[2] == '\\')
            return true;

        /* "scheme:/path/to/file" */
        for (int i = 0; i < path.Length - 1; ++i)
        {
            if (path[i] == ':' && path[i + 1] == '/')
                return true;
        }

        return false;
    }

    public static string? GetFirstItemFromPlaylist(RcHashIterator iterator)
    {
        object? fileHandle = FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
        {
            IteratorError(iterator, "Could not open playlist");
            return null;
        }

        byte[] buffer = new byte[1023];
        int numRead = FileRead(iterator, fileHandle, buffer, 1023);

        FileClose(iterator, fileHandle);

        /* decode bytes 1:1 (paths are expected ASCII/UTF-8; Latin1 is lossless) */
        string text = Encoding.Latin1.GetString(buffer, 0, numRead);

        int ptr = 0;
        int start;
        int next;
        int fileLen;

        while (true)
        {
            /* ignore empty and commented lines */
            while (ptr < text.Length && (text[ptr] == '#' || text[ptr] == '\r' || text[ptr] == '\n'))
            {
                while (ptr < text.Length && text[ptr] != '\n')
                    ++ptr;
                if (ptr < text.Length)
                    ++ptr;
            }

            /* find and extract the current line */
            start = ptr;
            while (ptr < text.Length && text[ptr] != '\n')
                ++ptr;
            next = ptr;

            /* remove trailing whitespace - especially '\r' */
            while (ptr > start && char.IsWhiteSpace(text[ptr - 1]))
                --ptr;

            /* if we found a non-empty line, break out of the loop to process it */
            fileLen = ptr - start;
            if (fileLen > 0)
                break;

            /* did we reach the end of the file? */
            if (next >= text.Length)
                return null;

            /* if the line only contained whitespace, keep searching */
            ptr = next + 1;
        }

        IteratorVerboseFormatted(iterator, "Extracted {0} from playlist", text.Substring(start, fileLen));

        string line = text.Substring(start, fileLen);
        int pathLen;
        if (IsPathAbsolute(line))
            pathLen = 0;
        else
            pathLen = iterator.Path!.Length - PathGetFilename(iterator.Path!).Length;

        return iterator.Path!.Substring(0, pathLen) + line;
    }

    public static int GenerateFromPlaylist(out string hash, uint consoleId, RcHashIterator iterator)
    {
        IteratorVerboseFormatted(iterator, "Processing playlist: {0}", PathGetFilename(iterator.Path!));

        string? discPath = GetFirstItemFromPlaylist(iterator);
        if (discPath == null)
        {
            hash = "";
            return IteratorError(iterator, "Failed to get first item from playlist");
        }

        var firstFileIterator = new RcHashIterator
        {
            Callbacks = CopyCallbacks(iterator.Callbacks),
            Userdata = iterator.Userdata,
            Path = discPath,
        };

        return FromFile(out hash, consoleId, firstFileIterator);
    }

    /* ===================================================== */
    /* dispatch tables                                       */

    /* Phase 3/4/5/6/7 targets; replaced as each phase lands */
    public static int NotYetImplemented(out string hash, RcHashIterator iterator, string name, string phase)
    {
        hash = "";
        return IteratorErrorFormatted(iterator, "RASharp: {0} not yet implemented ({1})", name, phase);
    }

    private static int RcHash3Do(out string hash, RcHashIterator iterator) => HashDisc.RcHash3Do(out hash, iterator);
    private static int RcHashDreamcast(out string hash, RcHashIterator iterator) => HashDisc.RcHashDreamcast(out hash, iterator);
    private static int RcHashGamecube(out string hash, RcHashIterator iterator) => HashDisc.RcHashGamecube(out hash, iterator);
    private static int RcHashJaguarCd(out string hash, RcHashIterator iterator) => HashDisc.RcHashJaguarCd(out hash, iterator);
    private static int RcHashNeogeoCd(out string hash, RcHashIterator iterator) => HashDisc.RcHashNeogeoCd(out hash, iterator);
    private static int RcHashPceCd(out string hash, RcHashIterator iterator) => HashDisc.RcHashPceCd(out hash, iterator);
    private static int RcHashPcfxCd(out string hash, RcHashIterator iterator) => HashDisc.RcHashPcfxCd(out hash, iterator);
    private static int RcHashPsx(out string hash, RcHashIterator iterator) => HashDisc.RcHashPsx(out hash, iterator);
    private static int RcHashPs2(out string hash, RcHashIterator iterator) => HashDisc.RcHashPs2(out hash, iterator);
    private static int RcHashPsp(out string hash, RcHashIterator iterator) => HashDisc.RcHashPsp(out hash, iterator);
    private static int RcHashSegaCd(out string hash, RcHashIterator iterator) => HashDisc.RcHashSegaCd(out hash, iterator);
    private static int RcHashWii(out string hash, RcHashIterator iterator) => HashDisc.RcHashWii(out hash, iterator);
        private static int RcHashNintendo3Ds(out string hash, RcHashIterator iterator) => HashEncrypted.RcHashNintendo3Ds(out hash, iterator);
    private static int RcHashMsDos(out string hash, RcHashIterator iterator) => HashZip.RcHashMsDos(out hash, iterator);

    public static int FromFile(out string hash, uint consoleId, RcHashIterator iterator)
    {
        string path = iterator.Path!;

        switch (consoleId)
        {
            default:
                hash = "";
                return IteratorErrorFormatted(iterator, "Unsupported console for file hash: {0}", consoleId);

            case ConsoleIds.RC_CONSOLE_ARCADIA_2001:
            case ConsoleIds.RC_CONSOLE_ATARI_2600:
            case ConsoleIds.RC_CONSOLE_ATARI_JAGUAR:
            case ConsoleIds.RC_CONSOLE_COLECOVISION:
            case ConsoleIds.RC_CONSOLE_ELEKTOR_TV_GAMES_COMPUTER:
            case ConsoleIds.RC_CONSOLE_FAIRCHILD_CHANNEL_F:
            case ConsoleIds.RC_CONSOLE_GAMEBOY:
            case ConsoleIds.RC_CONSOLE_GAMEBOY_ADVANCE:
            case ConsoleIds.RC_CONSOLE_GAMEBOY_COLOR:
            case ConsoleIds.RC_CONSOLE_GAME_GEAR:
            case ConsoleIds.RC_CONSOLE_INTELLIVISION:
            case ConsoleIds.RC_CONSOLE_INTERTON_VC_4000:
            case ConsoleIds.RC_CONSOLE_MAGNAVOX_ODYSSEY2:
            case ConsoleIds.RC_CONSOLE_MASTER_SYSTEM:
            case ConsoleIds.RC_CONSOLE_MEGADUCK:
            case ConsoleIds.RC_CONSOLE_NEOGEO_POCKET:
            case ConsoleIds.RC_CONSOLE_ORIC:
            case ConsoleIds.RC_CONSOLE_POKEMON_MINI:
            case ConsoleIds.RC_CONSOLE_SEGA_32X:
            case ConsoleIds.RC_CONSOLE_SG1000:
            case ConsoleIds.RC_CONSOLE_SUPERVISION:
            case ConsoleIds.RC_CONSOLE_TI83:
            case ConsoleIds.RC_CONSOLE_TIC80:
            case ConsoleIds.RC_CONSOLE_UZEBOX:
            case ConsoleIds.RC_CONSOLE_VECTREX:
            case ConsoleIds.RC_CONSOLE_VIRTUAL_BOY:
            case ConsoleIds.RC_CONSOLE_WASM4:
            case ConsoleIds.RC_CONSOLE_WONDERSWAN:
            case ConsoleIds.RC_CONSOLE_ZX_SPECTRUM:
                /* generic whole-file hash - don't buffer */
                return WholeFile(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_MEGA_DRIVE:
                /* generic whole-file hash with m3u support - don't buffer */
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return WholeFile(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_ATARI_7800:
            case ConsoleIds.RC_CONSOLE_ATARI_LYNX:
            case ConsoleIds.RC_CONSOLE_FAMICOM_DISK_SYSTEM:
            case ConsoleIds.RC_CONSOLE_NINTENDO:
            case ConsoleIds.RC_CONSOLE_PC_ENGINE:
            case ConsoleIds.RC_CONSOLE_SUPER_CASSETTEVISION:
            case ConsoleIds.RC_CONSOLE_SUPER_NINTENDO:
                /* additional logic whole-file hash - buffer then call rc_hash_generate_from_buffer */
                return BufferedFile(out hash, consoleId, iterator);

            case ConsoleIds.RC_CONSOLE_AMSTRAD_PC:
            case ConsoleIds.RC_CONSOLE_APPLE_II:
            case ConsoleIds.RC_CONSOLE_COMMODORE_64:
            case ConsoleIds.RC_CONSOLE_MSX:
            case ConsoleIds.RC_CONSOLE_PC8800:
                /* generic whole-file hash with m3u support - don't buffer */
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return WholeFile(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_3DO:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHash3Do(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_ARCADE:
                /* .neo files (Geolith Neo Geo cart format) contain the actual ROM data,
                 * so are content-hashed. Everything else (.zip/.7z) hashes by filename. */
                if (PathCompareExtension(path, "neo") != 0)
                    return HashRom.RcHashNeogeoCart(out hash, iterator);

                return HashRom.RcHashArcade(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_ARDUBOY:
                return HashRom.RcHashArduboy(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_ATARI_JAGUAR_CD:
                return RcHashJaguarCd(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_DREAMCAST:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHashDreamcast(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_GAMECUBE:
                return RcHashGamecube(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_MS_DOS:
                return RcHashMsDos(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_NEO_GEO_CD:
                return RcHashNeogeoCd(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_NINTENDO_64:
                return HashRom.RcHashN64(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_NINTENDO_3DS:
                return RcHashNintendo3Ds(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_NINTENDO_DS:
            case ConsoleIds.RC_CONSOLE_NINTENDO_DSI:
                return HashRom.RcHashNintendoDs(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_PC_ENGINE_CD:
                if (PathCompareExtension(path, "cue") != 0 || PathCompareExtension(path, "chd") != 0)
                    return RcHashPceCd(out hash, iterator);

                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return BufferedFile(out hash, consoleId, iterator);

            case ConsoleIds.RC_CONSOLE_PCFX:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHashPcfxCd(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_PLAYSTATION:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHashPsx(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_PLAYSTATION_2:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHashPs2(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_PSP:
                return RcHashPsp(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_SEGA_CD:
            case ConsoleIds.RC_CONSOLE_SATURN:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHashSegaCd(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_WII:
                return RcHashWii(out hash, iterator);
        }
    }

    public static int FromBuffer(out string hash, uint consoleId, RcHashIterator iterator)
    {
        switch (consoleId)
        {
            default:
                hash = "";
                return IteratorErrorFormatted(iterator, "Unsupported console for buffer hash: {0}", consoleId);

            case ConsoleIds.RC_CONSOLE_AMSTRAD_PC:
            case ConsoleIds.RC_CONSOLE_APPLE_II:
            case ConsoleIds.RC_CONSOLE_ARCADIA_2001:
            case ConsoleIds.RC_CONSOLE_ATARI_2600:
            case ConsoleIds.RC_CONSOLE_ATARI_JAGUAR:
            case ConsoleIds.RC_CONSOLE_COLECOVISION:
            case ConsoleIds.RC_CONSOLE_COMMODORE_64:
            case ConsoleIds.RC_CONSOLE_ELEKTOR_TV_GAMES_COMPUTER:
            case ConsoleIds.RC_CONSOLE_FAIRCHILD_CHANNEL_F:
            case ConsoleIds.RC_CONSOLE_GAMEBOY:
            case ConsoleIds.RC_CONSOLE_GAMEBOY_ADVANCE:
            case ConsoleIds.RC_CONSOLE_GAMEBOY_COLOR:
            case ConsoleIds.RC_CONSOLE_GAME_GEAR:
            case ConsoleIds.RC_CONSOLE_INTELLIVISION:
            case ConsoleIds.RC_CONSOLE_INTERTON_VC_4000:
            case ConsoleIds.RC_CONSOLE_MAGNAVOX_ODYSSEY2:
            case ConsoleIds.RC_CONSOLE_MASTER_SYSTEM:
            case ConsoleIds.RC_CONSOLE_MEGA_DRIVE:
            case ConsoleIds.RC_CONSOLE_MEGADUCK:
            case ConsoleIds.RC_CONSOLE_MSX:
            case ConsoleIds.RC_CONSOLE_NEOGEO_POCKET:
            case ConsoleIds.RC_CONSOLE_ORIC:
            case ConsoleIds.RC_CONSOLE_PC8800:
            case ConsoleIds.RC_CONSOLE_POKEMON_MINI:
            case ConsoleIds.RC_CONSOLE_SEGA_32X:
            case ConsoleIds.RC_CONSOLE_SG1000:
            case ConsoleIds.RC_CONSOLE_SUPERVISION:
            case ConsoleIds.RC_CONSOLE_TI83:
            case ConsoleIds.RC_CONSOLE_TIC80:
            case ConsoleIds.RC_CONSOLE_UZEBOX:
            case ConsoleIds.RC_CONSOLE_VECTREX:
            case ConsoleIds.RC_CONSOLE_VIRTUAL_BOY:
            case ConsoleIds.RC_CONSOLE_WASM4:
            case ConsoleIds.RC_CONSOLE_WONDERSWAN:
            case ConsoleIds.RC_CONSOLE_ZX_SPECTRUM:
                return HashBuffer(out hash, iterator.Buffer!, iterator.BufferSize, iterator);

            case ConsoleIds.RC_CONSOLE_ARCADE:
                /* .neo (Geolith Neo Geo cart) files carry the ROM data; other arcade
                 * formats are archives, which aren't hashed from a buffer. */
                return HashRom.RcHashNeogeoCart(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_ARDUBOY:
                return HashRom.RcHashArduboy(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_ATARI_7800:
                return HashRom.RcHash7800(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_ATARI_LYNX:
                return HashRom.RcHashLynx(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_FAMICOM_DISK_SYSTEM:
            case ConsoleIds.RC_CONSOLE_NINTENDO:
                return HashRom.RcHashNes(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_PC_ENGINE: /* NOTE: does not support PCEngine CD */
                return HashRom.RcHashPce(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_SUPER_CASSETTEVISION:
                return HashRom.RcHashScv(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_SUPER_NINTENDO:
                return HashRom.RcHashSnes(out hash, iterator);

            case ConsoleIds.RC_CONSOLE_NINTENDO_64:
            case ConsoleIds.RC_CONSOLE_NINTENDO_3DS:
            case ConsoleIds.RC_CONSOLE_NINTENDO_DS:
            case ConsoleIds.RC_CONSOLE_NINTENDO_DSI:
                return FileFromBuffer(out hash, consoleId, iterator);
        }
    }

    /* ===================================================== */
    /* iterator reset / callback merge                       */

    public static void ResetIterator(RcHashIterator iterator)
    {
        iterator.Buffer = null;
        iterator.BufferSize = 0;
        iterator.Consoles = new uint[12];
        iterator.Index = -1;
        iterator.Path = null;
        iterator.Userdata = null;
        iterator.Callbacks = new RcHashCallbacks();

        if (g_verbose_message_callback != null)
            iterator.Callbacks.VerboseMessage = CallGVerboseMessageCallback;
        if (g_error_message_callback != null)
            iterator.Callbacks.ErrorMessage = CallGErrorMessageCallback;

        if (g_filereader != null)
        {
            iterator.Callbacks.Filereader = g_filereader;
        }
        else if (iterator.Callbacks.Filereader.Open == null)
        {
            iterator.Callbacks.Filereader.Open = FileReaderOpen;
            iterator.Callbacks.Filereader.Close = FileReaderClose;
            iterator.Callbacks.Filereader.Seek = FileReaderSeek;
            iterator.Callbacks.Filereader.Tell = FileReaderTell;
            iterator.Callbacks.Filereader.Read = FileReaderRead;
        }

        ResetIteratorDisc(iterator);
        ResetIteratorEncrypted(iterator);
    }

    /* ported verbatim, including the upstream quirk: the C code assigns
     * callbacks->error_message to iterator->callbacks.verbose_message */
    public static void MergeCallbacks(RcHashIterator iterator, RcHashCallbacks callbacks)
    {
        if (callbacks.VerboseMessage != null)
            iterator.Callbacks.VerboseMessage = callbacks.VerboseMessage;
        if (callbacks.ErrorMessage != null)
            iterator.Callbacks.ErrorMessage = callbacks.ErrorMessage;

        if (callbacks.Filereader.Open != null)
            iterator.Callbacks.Filereader = callbacks.Filereader;

        if (callbacks.Cdreader.OpenTrack != null)
            iterator.Callbacks.Cdreader = callbacks.Cdreader;

        if (callbacks.Encryption.Get3DsCiaNormalKey != null)
            iterator.Callbacks.Encryption.Get3DsCiaNormalKey = callbacks.Encryption.Get3DsCiaNormalKey;
        if (callbacks.Encryption.Get3DsNcchNormalKeys != null)
            iterator.Callbacks.Encryption.Get3DsNcchNormalKeys = callbacks.Encryption.Get3DsNcchNormalKeys;
    }
}
