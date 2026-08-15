// Ported from rcheevos (MIT) — src/rhash/hash.c
// Engine internals: message callbacks, filereader plumbing, whole-file /
// buffered-file / playlist hashing, and the console dispatch tables.
// Control flow, constants, and special cases are translated 1:1; do not
// "improve" behavior — parity is the requirement.

using System.Text;
using RASharp.Core.Models;

namespace RASharp.Core;

/// <summary>Ported from rcheevos (MIT) — src/rhash/hash.c Engine internals: message callbacks, filereader plumbing, whole-file / buffered-file / playlist hashing, and the c</summary>
public static class HashEngine
{
    /* arbitrary limit to prevent allocating and hashing large files */
    public const long MaxBufferSize = 64 * 1024 * 1024;

    public const int SeekSet = 0;
    public const int SeekCur = 1;
    public const int SeekEnd = 2;

    /* ===================================================== */
    /* message callbacks (hash.c statics)                    */

    private static RcHashMessageCallbackDeprecated? _gErrorMessageCallback;
    private static RcHashMessageCallbackDeprecated? _gVerboseMessageCallback;

    /* wrappers bridging the deprecated single-arg global callbacks to the
     * iterator-based delegate; the iterator is not used by the globals, so
     * the parameter is a discard */
    private static void CallGErrorMessageCallback(string message, RcHashIterator? _)
    {
        _gErrorMessageCallback!(message);
    }

    private static void CallGVerboseMessageCallback(string message, RcHashIterator? _)
    {
        _gVerboseMessageCallback!(message);
    }

    private static RcHashMessageCallback? GetErrorMessageCallback(RcHashCallbacks callbacks)
    {
        if (callbacks.ErrorMessage != null)
            return callbacks.ErrorMessage;

        if (_gErrorMessageCallback != null)
            return CallGErrorMessageCallback;

        if (callbacks.VerboseMessage != null)
            return callbacks.VerboseMessage;

        if (_gVerboseMessageCallback != null)
            return CallGVerboseMessageCallback;

        return null;
    }

    /// <summary>Stores the global error message callback.</summary>
    /// <param name="callback">the callback to register</param>
    public static void HashInitErrorMessageCallback(RcHashMessageCallbackDeprecated? callback)
    {
        _gErrorMessageCallback = callback;
    }

    /* for Hash3DS (the C calls rhash_log_error_message directly) */
    /// <summary>for Hash3DS (the C calls rhash_log_error_message directly)</summary>
    /// <param name="message">the message text</param>
    public static void CallErrorMessage(string message)
    {
        if (_gErrorMessageCallback != null)
            _gErrorMessageCallback(message);
    }

    /// <summary>Stores the global verbose message callback.</summary>
    /// <param name="callback">the callback to register</param>
    public static void HashInitVerboseMessageCallback(RcHashMessageCallbackDeprecated? callback)
    {
        _gVerboseMessageCallback = callback;
    }

    /// <summary>Reports a non-formatted error through the error callback.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="message">the message text</param>
    /// <returns>the result</returns>
    public static int IteratorError(RcHashIterator iterator, string message)
    {
        GetErrorMessageCallback(iterator.Callbacks)?.Invoke(message, iterator);
        return 0;
    }

    /// <summary>Reports a formatted error through the error callback.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="format">the format parameter</param>
    /// <param name="args">the command-line arguments</param>
    /// <returns>the result</returns>
    public static int IteratorErrorFormatted(RcHashIterator iterator, string format, params object?[] args)
    {
        GetErrorMessageCallback(iterator.Callbacks)?.Invoke(string.Format(format, args), iterator);
        return 0;
    }

    /// <summary>Reports a non-formatted verbose message through the verbose callback.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="message">the message text</param>
    public static void IteratorVerbose(RcHashIterator iterator, string message)
    {
        if (iterator.Callbacks.VerboseMessage != null)
            iterator.Callbacks.VerboseMessage(message, iterator);
        else if (_gVerboseMessageCallback != null)
            _gVerboseMessageCallback(message);
    }

    /// <summary>Reports a formatted verbose message through the verbose callback.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="format">the format parameter</param>
    /// <param name="args">the command-line arguments</param>
    public static void IteratorVerboseFormatted(RcHashIterator iterator, string format, params object?[] args)
    {
        var message = string.Format(format, args);
        if (iterator.Callbacks.VerboseMessage != null)
            iterator.Callbacks.VerboseMessage(message, iterator);
        else if (_gVerboseMessageCallback != null)
            _gVerboseMessageCallback(message);
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
            case SeekSet: fs.Seek(offset, SeekOrigin.Begin); break;
            case SeekCur: fs.Seek(offset, SeekOrigin.Current); break;
            case SeekEnd: fs.Seek(offset, SeekOrigin.End); break;
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
    /// <summary>for unit tests - normally would call InitCustomFilereader(null)</summary>
    public static void ResetFilereader()
    {
        _gFilereader = null;
    }

    private static RcHashFilereader? _gFilereader;

    /// <summary>Registers a custom global file reader.</summary>
    /// <param name="reader">the reader to register</param>
    public static void InitCustomFilereader(RcHashFilereader? reader)
    {
        /* initialize with defaults first */
        var funcs = new RcHashFilereader
        {
            Open = FileReaderOpen,
            Seek = FileReaderSeek,
            Tell = FileReaderTell,
            Read = FileReaderRead,
            Close = FileReaderClose
        };

        /* hook up any provided custom handlers */
        if (reader != null)
        {
            if (reader.Open != null)
            {
                funcs.Open = reader.Open;
            }

            if (reader.Seek != null)
            {
                funcs.Seek = reader.Seek;
            }

            if (reader.Tell != null)
            {
                funcs.Tell = reader.Tell;
            }

            if (reader.Read != null)
            {
                funcs.Read = reader.Read;
            }

            if (reader.Close != null)
            {
                funcs.Close = reader.Close;
            }
        }

        _gFilereader = funcs;
    }

    /* ===================================================== */
    /* cdreader + encryption globals (hash_disc.c / hash_encrypted.c) */

    private static RcHashCdreader? _gCdreader;

    /// <summary>reset iterator disc.</summary>
    /// <param name="iterator">the hash iterator</param>
    public static void ResetIteratorDisc(RcHashIterator iterator)
    {
        if (_gCdreader != null)
        {
            iterator.Callbacks.Cdreader = _gCdreader;
        }
        else
            GetDefaultCdreader(iterator.Callbacks.Cdreader);
    }

    /// <summary>Registers a custom global cdreader.</summary>
    /// <param name="reader">the reader to register</param>
    public static void InitCustomCdreader(RcHashCdreader? reader)
    {
        if (reader != null)
        {
            _gCdreader = reader;
        }
        else
        {
            _gCdreader = null;
        }
    }

    /* for HashDisc's rc_cd_* fallbacks (the C keeps g_cdreader in hash_disc.c) */
    /// <summary>for HashDisc's rc_cd_* fallbacks (the C keeps g_cdreader in hash_disc.c)</summary>
    /// <returns>the result</returns>
    internal static RcHashCdreader? GetGlobalCdreader()
    {
        return _gCdreader;
    }

    /* default cdreader handlers (cdreader.c port) */
    /// <summary>default cdreader handlers (cdreader.c port)</summary>
    /// <param name="cdreader">the cdreader parameter</param>
    public static void GetDefaultCdreader(RcHashCdreader cdreader)
    {
        CdReader.GetDefaultCdreader(cdreader);
    }

    private static RcHash3DsGetCiaNormalKeyFunc? _g3DsCiaNormalKeyFunc;
    private static RcHash3DsGetNcchNormalKeysFunc? _g3DsNcchNormalKeysFunc;

    /// <summary>reset iterator encrypted.</summary>
    /// <param name="iterator">the hash iterator</param>
    public static void ResetIteratorEncrypted(RcHashIterator iterator)
    {
        iterator.Callbacks.Encryption.Get3DsCiaNormalKey = _g3DsCiaNormalKeyFunc;
        iterator.Callbacks.Encryption.Get3DsNcchNormalKeys = _g3DsNcchNormalKeysFunc;
    }

    /// <summary>Stores the global 3DS CIA normal-key provider.</summary>
    /// <param name="func">the func parameter</param>
    public static void HashInit3DsGetCiaNormalKeyFunc(RcHash3DsGetCiaNormalKeyFunc func)
    {
        _g3DsCiaNormalKeyFunc = func;
    }

    /// <summary>Stores the global 3DS NCCH normal-keys provider.</summary>
    /// <param name="func">the func parameter</param>
    public static void HashInit3DsGetNcchNormalKeysFunc(RcHash3DsGetNcchNormalKeysFunc func)
    {
        _g3DsNcchNormalKeysFunc = func;
    }

    /* ===================================================== */
    /* rc_file_* wrappers                                    */

    /// <summary>===================================================== rc_file_* wrappers</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="path">the file path</param>
    /// <returns>the handle, or null on failure</returns>
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

    /// <summary>Seeks a file through the iterator filereader.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="fileHandle">the open file handle</param>
    /// <param name="offset">the byte offset</param>
    /// <param name="origin">the seek origin</param>
    public static void FileSeek(RcHashIterator iterator, object fileHandle, long offset, int origin)
    {
        if (iterator.Callbacks.Filereader.Seek != null)
            iterator.Callbacks.Filereader.Seek(fileHandle, offset, origin);
    }

    /// <summary>Returns the current position of a file through the iterator filereader.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="fileHandle">the open file handle</param>
    /// <returns>the current position</returns>
    public static long FileTell(RcHashIterator iterator, object fileHandle)
    {
        return iterator.Callbacks.Filereader.Tell != null ? iterator.Callbacks.Filereader.Tell(fileHandle) : 0;
    }

    /// <summary>Reads bytes from a file through the iterator filereader.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="fileHandle">the open file handle</param>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="requestedBytes">the number of bytes requested</param>
    /// <returns>the number of bytes read</returns>
    public static int FileRead(RcHashIterator iterator, object fileHandle, byte[] buffer, int requestedBytes)
    {
        return iterator.Callbacks.Filereader.Read != null ? iterator.Callbacks.Filereader.Read(fileHandle, buffer, requestedBytes) : 0;
    }

    /// <summary>Closes a file through the iterator filereader.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="fileHandle">the open file handle</param>
    public static void FileClose(RcHashIterator iterator, object fileHandle)
    {
        if (iterator.Callbacks.Filereader.Close != null)
            iterator.Callbacks.Filereader.Close(fileHandle);
    }

    /// <summary>file size.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="path">the file path</param>
    /// <returns>the result</returns>
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
            var handle = iterator.Callbacks.Filereader.Open(path);
            if (handle != null)
            {
                FileSeek(iterator, handle, 0, SeekEnd);
                size = FileTell(iterator, handle);
                FileClose(iterator, handle);
            }
        }

        return size;
    }

    /* ===================================================== */
    /* path helpers                                          */

    /// <summary>===================================================== path helpers</summary>
    /// <param name="path">the file path</param>
    /// <returns>the generated value</returns>
    public static string PathGetFilename(string path)
    {
        var ptr = path.Length;
        while (ptr > 0)
        {
            if (path[ptr - 1] == '/' || path[ptr - 1] == '\\')
                break;

            --ptr;
        }

        return path.Substring(ptr);
    }

    /// <summary>Returns the lowercase extension of a path, including the dot.</summary>
    /// <param name="path">the file path</param>
    /// <returns>the generated value</returns>
    public static string PathGetExtension(string path)
    {
        var ptr = path.Length;
        while (ptr > 0)
        {
            if (path[ptr - 1] == '.')
                return path.Substring(ptr);

            --ptr;
        }

        return "";
    }

    /// <summary>Compares a path extension with a candidate (case-insensitive).</summary>
    /// <param name="path">the file path</param>
    /// <param name="ext">the ext parameter</param>
    /// <returns>the result</returns>
    public static int PathCompareExtension(string path, string ext)
    {
        var pathLen = path.Length;
        var extLen = ext.Length;
        if (extLen > pathLen)
            return 0;

        var ptr = pathLen - extLen;
        if (ptr == 0 || path[ptr - 1] != '.')
            return 0;

        if (string.CompareOrdinal(path, ptr, ext, 0, extLen) == 0)
            return 1;

        for (var i = 0; i < extLen; ++i)
        {
            if (char.ToLowerInvariant(path[ptr + i]) != ext[i])
                return 0;
        }

        return 1;
    }

    /* ===================================================== */
    /* byteswap helpers (used by ROM/disc code)              */

    /// <summary>===================================================== byteswap helpers (used by ROM/disc code)</summary>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="count">the number of bytes</param>
    public static void Byteswap16(byte[] buffer, int count)
    {
        var ptr = 0;
        while (ptr + 4 <= count)
        {
            var temp = (uint)(buffer[ptr] | (buffer[ptr + 1] << 8) | (buffer[ptr + 2] << 16) | (buffer[ptr + 3] << 24));
            temp = ((temp & 0xFF00FF00) >> 8) | ((temp & 0x00FF00FF) << 8);
            buffer[ptr] = (byte)temp;
            buffer[ptr + 1] = (byte)(temp >> 8);
            buffer[ptr + 2] = (byte)(temp >> 16);
            buffer[ptr + 3] = (byte)(temp >> 24);
            ptr += 4;
        }
    }

    /// <summary>byteswap32.</summary>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="count">the number of bytes</param>
    public static void Byteswap32(byte[] buffer, int count)
    {
        var ptr = 0;
        while (ptr + 4 <= count)
        {
            var temp = (uint)(buffer[ptr] | (buffer[ptr + 1] << 8) | (buffer[ptr + 2] << 16) | (buffer[ptr + 3] << 24));
            temp = ((temp & 0xFF000000) >> 24) | ((temp & 0x00FF0000) >> 8) | ((temp & 0x0000FF00) << 8) | ((temp & 0x000000FF) << 24);
            buffer[ptr] = (byte)temp;
            buffer[ptr + 1] = (byte)(temp >> 8);
            buffer[ptr + 2] = (byte)(temp >> 16);
            buffer[ptr + 3] = (byte)(temp >> 24);
            ptr += 4;
        }
    }

    /* ===================================================== */

    /// <summary>=====================================================</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="md5">the MD5 state</param>
    /// <param name="hash">the generated 32-char hash</param>
    /// <returns>the result</returns>
    public static int Finalize(RcHashIterator iterator, HashMd5 md5, out string hash)
    {
        var digest = md5.Finish();

#if NET8_0
        hash = Convert.ToHexString(digest).ToLowerInvariant();
#else
        hash = Convert.ToHexStringLower(digest);
#endif

        IteratorVerboseFormatted(iterator, "Generated hash {0}", hash);

        return 1;
    }

    /* rc_hash_buffer — hashes buffer[offset .. offset + bufferSize) */
    /// <summary>rc_hash_buffer — hashes buffer[offset .. offset + bufferSize)</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="offset">the byte offset</param>
    /// <param name="bufferSize">the size of the buffer</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int HashBuffer(out string hash, byte[] buffer, int offset, int bufferSize, RcHashIterator iterator)
    {
        var md5 = new HashMd5();

        if (bufferSize > MaxBufferSize)
        {
            bufferSize = (int)MaxBufferSize;
        }

        md5.Append(buffer, offset, bufferSize);

        IteratorVerboseFormatted(iterator, "Hashing {0} byte buffer", (uint)bufferSize);

        return Finalize(iterator, md5, out hash);
    }

    /// <summary>Hashes a buffer region with the given MD5 state.</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="bufferSize">the size of the buffer</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
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

    private static readonly BufferedFileState SBufferedFile = new();

    private static RcHashFilereader CreateBufferedFileReader()
    {
        return new RcHashFilereader
        {
            Open = _ => new BufferedFileHandle
            {
                Data = SBufferedFile.Data,
                ReadPtr = SBufferedFile.ReadPtr,
                DataSize = SBufferedFile.DataSize
            },
            Seek = (fileHandle, offset, origin) =>
            {
                var bufferedFile = (BufferedFileHandle)fileHandle;
                switch (origin)
                {
                    case SeekSet: bufferedFile.ReadPtr = (int)offset; break;
                    case SeekCur: bufferedFile.ReadPtr += (int)offset; break;
                    case SeekEnd: bufferedFile.ReadPtr = (int)(bufferedFile.DataSize + offset); break;
                }

                if (bufferedFile.ReadPtr < 0)
                {
                    bufferedFile.ReadPtr = 0;
                }
                else if (bufferedFile.ReadPtr > bufferedFile.DataSize)
                {
                    bufferedFile.ReadPtr = (int)bufferedFile.DataSize;
                }
            },
            Tell = fileHandle => ((BufferedFileHandle)fileHandle).ReadPtr,
            Read = (fileHandle, buffer, requestedBytes) =>
            {
                var bufferedFile = (BufferedFileHandle)fileHandle;
                var remaining = bufferedFile.DataSize - bufferedFile.ReadPtr;
                if (requestedBytes > remaining)
                {
                    requestedBytes = (int)remaining;
                }

                if (requestedBytes > 0)
                {
                    Array.Copy(bufferedFile.Data!, bufferedFile.ReadPtr, buffer, 0, requestedBytes);
                    bufferedFile.ReadPtr += requestedBytes;
                }

                return requestedBytes;
            },
            Close = _ => { }
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
            Encryption = callbacks.Encryption
        };
    }

    private static int FileFromBuffer(out string hash, uint consoleId, RcHashIterator iterator)
    {
        var bufferedFileIterator = new RcHashIterator
        {
            Callbacks = CopyCallbacks(iterator.Callbacks),
            Userdata = iterator.Userdata,
            Path = "memory stream"
        };

        bufferedFileIterator.Callbacks.Filereader = CreateBufferedFileReader();

        SBufferedFile.Data = iterator.Buffer;
        SBufferedFile.ReadPtr = 0;
        SBufferedFile.DataSize = iterator.BufferSize;

        return FromFile(out hash, consoleId, bufferedFileIterator);
    }

    /* ===================================================== */
    /* whole-file / buffered-file hashing                    */

    /// <summary>===================================================== whole-file / buffered-file hashing</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int WholeFile(out string hash, RcHashIterator iterator)
    {
        var md5 = new HashMd5();
        var buffer = new byte[65536];
        long remaining;
        hash = "";

        var fileHandle = FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return IteratorError(iterator, "Could not open file");

        FileSeek(iterator, fileHandle, 0, SeekEnd);
        var size = FileTell(iterator, fileHandle);

        if (size > MaxBufferSize)
        {
            IteratorVerboseFormatted(iterator, "Hashing first {0} bytes (of {1} bytes) of {2}", (uint)MaxBufferSize, (uint)size, PathGetFilename(iterator.Path!));
            remaining = MaxBufferSize;
        }
        else
        {
            IteratorVerboseFormatted(iterator, "Hashing {0} ({1} bytes)", PathGetFilename(iterator.Path!), (uint)size);
            remaining = size;
        }

        FileSeek(iterator, fileHandle, 0, SeekSet);
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

        var result = Finalize(iterator, md5, out hash);

        FileClose(iterator, fileHandle);
        return result;
    }

    /// <summary>Reads the file into memory (capped at MAX_BUFFER_SIZE) and dispatches to the buffer path.</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int BufferedFile(out string hash, uint consoleId, RcHashIterator iterator)
    {
        hash = "";

        var fileHandle = FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return IteratorError(iterator, "Could not open file");

        FileSeek(iterator, fileHandle, 0, SeekEnd);
        var size = FileTell(iterator, fileHandle);

        if (size > MaxBufferSize)
        {
            IteratorVerboseFormatted(iterator, "Buffering first {0} bytes (of {1} bytes) of {2}", (uint)MaxBufferSize, (uint)size, PathGetFilename(iterator.Path!));
            size = MaxBufferSize;
        }
        else
        {
            IteratorVerboseFormatted(iterator, "Buffering {0} ({1} bytes)", PathGetFilename(iterator.Path!), (uint)size);
        }

        var buffer = new byte[(int)size];

        var bufferIterator = new RcHashIterator
        {
            Callbacks = CopyCallbacks(iterator.Callbacks),
            Userdata = iterator.Userdata,
            Path = iterator.Path,
            Buffer = buffer,
            BufferSize = (int)size
        };

        FileSeek(iterator, fileHandle, 0, SeekSet);
        FileRead(iterator, fileHandle, buffer, (int)size);

        var result = FromBuffer(out hash, consoleId, bufferIterator);

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
        for (var i = 0; i < path.Length - 1; ++i)
        {
            if (path[i] == ':' && path[i + 1] == '/')
                return true;
        }

        return false;
    }

    /// <summary>get first item from playlist.</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static string? GetFirstItemFromPlaylist(RcHashIterator iterator)
    {
        var fileHandle = FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
        {
            IteratorError(iterator, "Could not open playlist");
            return null;
        }

        var buffer = new byte[1023];
        var numRead = FileRead(iterator, fileHandle, buffer, 1023);

        FileClose(iterator, fileHandle);

        /* decode bytes 1:1 (paths are expected ASCII/UTF-8; Latin1 is lossless) */
        var text = Encoding.Latin1.GetString(buffer, 0, numRead);

        var ptr = 0;
        int start;
        int fileLen;

        while (true)
        {
            /* ignore empty and commented lines */
            while (ptr < text.Length && (text[ptr] == '#' || text[ptr] == '\r' || text[ptr] == '\n'))
            {
                while (ptr < text.Length && text[ptr] != '\n')
                {
                    ++ptr;
                }

                if (ptr < text.Length)
                {
                    ++ptr;
                }
            }

            /* find and extract the current line */
            start = ptr;
            while (ptr < text.Length && text[ptr] != '\n')
            {
                ++ptr;
            }

            var next = ptr;

            /* remove trailing whitespace - especially '\r' */
            while (ptr > start && char.IsWhiteSpace(text[ptr - 1]))
            {
                --ptr;
            }

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

        var line = text.Substring(start, fileLen);
        int pathLen;
        if (IsPathAbsolute(line))
        {
            pathLen = 0;
        }
        else
        {
            pathLen = iterator.Path!.Length - PathGetFilename(iterator.Path!).Length;
        }

        return iterator.Path!.Substring(0, pathLen) + line;
    }

    /// <summary>Hashes the first entry of an m3u playlist with the console.</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int GenerateFromPlaylist(out string hash, uint consoleId, RcHashIterator iterator)
    {
        IteratorVerboseFormatted(iterator, "Processing playlist: {0}", PathGetFilename(iterator.Path!));

        var discPath = GetFirstItemFromPlaylist(iterator);
        if (discPath == null)
        {
            hash = "";
            return IteratorError(iterator, "Failed to get first item from playlist");
        }

        var firstFileIterator = new RcHashIterator
        {
            Callbacks = CopyCallbacks(iterator.Callbacks),
            Userdata = iterator.Userdata,
            Path = discPath
        };

        return FromFile(out hash, consoleId, firstFileIterator);
    }

    /* ===================================================== */
    /* dispatch tables                                       */

    /* Phase 3/4/5/6/7 targets; replaced as each phase lands */
    /// <summary>===================================================== dispatch tables Phase 3/4/5/6/7 targets; replaced as each phase lands</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="name">the name parameter</param>
    /// <param name="phase">the phase parameter</param>
    /// <returns>the result</returns>
    public static int NotYetImplemented(out string hash, RcHashIterator iterator, string name, string phase)
    {
        hash = "";
        return IteratorErrorFormatted(iterator, "RASharp: {0} not yet implemented ({1})", name, phase);
    }

    private static int RcHash3Do(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHash3Do(out hash, iterator);
    }

    private static int RcHashDreamcast(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashDreamcast(out hash, iterator);
    }

    private static int RcHashGamecube(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashGamecube(out hash, iterator);
    }

    private static int RcHashJaguarCd(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashJaguarCd(out hash, iterator);
    }

    private static int RcHashNeogeoCd(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashNeogeoCd(out hash, iterator);
    }

    private static int RcHashPceCd(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashPceCd(out hash, iterator);
    }

    private static int RcHashPcfxCd(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashPcfxCd(out hash, iterator);
    }

    private static int RcHashPsx(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashPsx(out hash, iterator);
    }

    private static int RcHashPs2(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashPs2(out hash, iterator);
    }

    private static int RcHashPsp(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashPsp(out hash, iterator);
    }

    private static int RcHashSegaCd(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashSegaCd(out hash, iterator);
    }

    private static int RcHashWii(out string hash, RcHashIterator iterator)
    {
        return HashDisc.RcHashWii(out hash, iterator);
    }

    private static int RcHashNintendo3Ds(out string hash, RcHashIterator iterator)
    {
        return HashEncrypted.RcHashNintendo3Ds(out hash, iterator);
    }

    private static int RcHashMsDos(out string hash, RcHashIterator iterator)
    {
        return HashZip.RcHashMsDos(out hash, iterator);
    }

    /// <summary>Dispatches a file hash for the console.</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="consoleId">the console identifier</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int FromFile(out string hash, uint consoleId, RcHashIterator iterator)
    {
        var path = iterator.Path!;

        switch (consoleId)
        {
            default:
                hash = "";
                return IteratorErrorFormatted(iterator, "Unsupported console for file hash: {0}", consoleId);

            case ConsoleIds.RcConsoleArcadia2001:
            case ConsoleIds.RcConsoleAtari2600:
            case ConsoleIds.RcConsoleAtariJaguar:
            case ConsoleIds.RcConsoleColecovision:
            case ConsoleIds.RcConsoleElektorTvGamesComputer:
            case ConsoleIds.RcConsoleFairchildChannelF:
            case ConsoleIds.RcConsoleGameboy:
            case ConsoleIds.RcConsoleGameboyAdvance:
            case ConsoleIds.RcConsoleGameboyColor:
            case ConsoleIds.RcConsoleGameGear:
            case ConsoleIds.RcConsoleIntellivision:
            case ConsoleIds.RcConsoleIntertonVc4000:
            case ConsoleIds.RcConsoleMagnavoxOdyssey2:
            case ConsoleIds.RcConsoleMasterSystem:
            case ConsoleIds.RcConsoleMegaduck:
            case ConsoleIds.RcConsoleNeogeoPocket:
            case ConsoleIds.RcConsoleOric:
            case ConsoleIds.RcConsolePokemonMini:
            case ConsoleIds.RcConsoleSega32X:
            case ConsoleIds.RcConsoleSg1000:
            case ConsoleIds.RcConsoleSupervision:
            case ConsoleIds.RcConsoleTi83:
            case ConsoleIds.RcConsoleTic80:
            case ConsoleIds.RcConsoleUzebox:
            case ConsoleIds.RcConsoleVectrex:
            case ConsoleIds.RcConsoleVirtualBoy:
            case ConsoleIds.RcConsoleWasm4:
            case ConsoleIds.RcConsoleWonderswan:
            case ConsoleIds.RcConsoleZxSpectrum:
                /* generic whole-file hash - don't buffer */
                return WholeFile(out hash, iterator);

            case ConsoleIds.RcConsoleMegaDrive:
                /* generic whole-file hash with m3u support - don't buffer */
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return WholeFile(out hash, iterator);

            case ConsoleIds.RcConsoleAtari7800:
            case ConsoleIds.RcConsoleAtariLynx:
            case ConsoleIds.RcConsoleFamicomDiskSystem:
            case ConsoleIds.RcConsoleNintendo:
            case ConsoleIds.RcConsolePcEngine:
            case ConsoleIds.RcConsoleSuperCassettevision:
            case ConsoleIds.RcConsoleSuperNintendo:
                /* additional logic whole-file hash - buffer then call rc_hash_generate_from_buffer */
                return BufferedFile(out hash, consoleId, iterator);

            case ConsoleIds.RcConsoleAmstradPc:
            case ConsoleIds.RcConsoleAppleIi:
            case ConsoleIds.RcConsoleCommodore64:
            case ConsoleIds.RcConsoleMsx:
            case ConsoleIds.RcConsolePc8800:
                /* generic whole-file hash with m3u support - don't buffer */
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return WholeFile(out hash, iterator);

            case ConsoleIds.RcConsole3Do:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHash3Do(out hash, iterator);

            case ConsoleIds.RcConsoleArcade:
                /* .neo files (Geolith Neo Geo cart format) contain the actual ROM data,
                 * so are content-hashed. Everything else (.zip/.7z) hashes by filename. */
                if (PathCompareExtension(path, "neo") != 0)
                    return HashRom.RcHashNeogeoCart(out hash, iterator);

                return HashRom.RcHashArcade(out hash, iterator);

            case ConsoleIds.RcConsoleArduboy:
                return HashRom.RcHashArduboy(out hash, iterator);

            case ConsoleIds.RcConsoleAtariJaguarCd:
                return RcHashJaguarCd(out hash, iterator);

            case ConsoleIds.RcConsoleDreamcast:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHashDreamcast(out hash, iterator);

            case ConsoleIds.RcConsoleGamecube:
                return RcHashGamecube(out hash, iterator);

            case ConsoleIds.RcConsoleMsDos:
                return RcHashMsDos(out hash, iterator);

            case ConsoleIds.RcConsoleNeoGeoCd:
                return RcHashNeogeoCd(out hash, iterator);

            case ConsoleIds.RcConsoleNintendo64:
                return HashRom.RcHashN64(out hash, iterator);

            case ConsoleIds.RcConsoleNintendo3Ds:
                return RcHashNintendo3Ds(out hash, iterator);

            case ConsoleIds.RcConsoleNintendoDs:
            case ConsoleIds.RcConsoleNintendoDsi:
                return HashRom.RcHashNintendoDs(out hash, iterator);

            case ConsoleIds.RcConsolePcEngineCd:
                if (PathCompareExtension(path, "cue") != 0 || PathCompareExtension(path, "chd") != 0)
                    return RcHashPceCd(out hash, iterator);

                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return BufferedFile(out hash, consoleId, iterator);

            case ConsoleIds.RcConsolePcfx:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHashPcfxCd(out hash, iterator);

            case ConsoleIds.RcConsolePlaystation:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHashPsx(out hash, iterator);

            case ConsoleIds.RcConsolePlaystation2:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHashPs2(out hash, iterator);

            case ConsoleIds.RcConsolePsp:
                return RcHashPsp(out hash, iterator);

            case ConsoleIds.RcConsoleSegaCd:
            case ConsoleIds.RcConsoleSaturn:
                if (PathCompareExtension(path, "m3u") != 0)
                    return GenerateFromPlaylist(out hash, consoleId, iterator);

                return RcHashSegaCd(out hash, iterator);

            case ConsoleIds.RcConsoleWii:
                return RcHashWii(out hash, iterator);
        }
    }

    /// <summary>Dispatches a buffer hash for the console.</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int FromBuffer(out string hash, uint consoleId, RcHashIterator iterator)
    {
        switch (consoleId)
        {
            default:
                hash = "";
                return IteratorErrorFormatted(iterator, "Unsupported console for buffer hash: {0}", consoleId);

            case ConsoleIds.RcConsoleAmstradPc:
            case ConsoleIds.RcConsoleAppleIi:
            case ConsoleIds.RcConsoleArcadia2001:
            case ConsoleIds.RcConsoleAtari2600:
            case ConsoleIds.RcConsoleAtariJaguar:
            case ConsoleIds.RcConsoleColecovision:
            case ConsoleIds.RcConsoleCommodore64:
            case ConsoleIds.RcConsoleElektorTvGamesComputer:
            case ConsoleIds.RcConsoleFairchildChannelF:
            case ConsoleIds.RcConsoleGameboy:
            case ConsoleIds.RcConsoleGameboyAdvance:
            case ConsoleIds.RcConsoleGameboyColor:
            case ConsoleIds.RcConsoleGameGear:
            case ConsoleIds.RcConsoleIntellivision:
            case ConsoleIds.RcConsoleIntertonVc4000:
            case ConsoleIds.RcConsoleMagnavoxOdyssey2:
            case ConsoleIds.RcConsoleMasterSystem:
            case ConsoleIds.RcConsoleMegaDrive:
            case ConsoleIds.RcConsoleMegaduck:
            case ConsoleIds.RcConsoleMsx:
            case ConsoleIds.RcConsoleNeogeoPocket:
            case ConsoleIds.RcConsoleOric:
            case ConsoleIds.RcConsolePc8800:
            case ConsoleIds.RcConsolePokemonMini:
            case ConsoleIds.RcConsoleSega32X:
            case ConsoleIds.RcConsoleSg1000:
            case ConsoleIds.RcConsoleSupervision:
            case ConsoleIds.RcConsoleTi83:
            case ConsoleIds.RcConsoleTic80:
            case ConsoleIds.RcConsoleUzebox:
            case ConsoleIds.RcConsoleVectrex:
            case ConsoleIds.RcConsoleVirtualBoy:
            case ConsoleIds.RcConsoleWasm4:
            case ConsoleIds.RcConsoleWonderswan:
            case ConsoleIds.RcConsoleZxSpectrum:
                return HashBuffer(out hash, iterator.Buffer!, iterator.BufferSize, iterator);

            case ConsoleIds.RcConsoleArcade:
                /* .neo (Geolith Neo Geo cart) files carry the ROM data; other arcade
                 * formats are archives, which aren't hashed from a buffer. */
                return HashRom.RcHashNeogeoCart(out hash, iterator);

            case ConsoleIds.RcConsoleArduboy:
                return HashRom.RcHashArduboy(out hash, iterator);

            case ConsoleIds.RcConsoleAtari7800:
                return HashRom.RcHash7800(out hash, iterator);

            case ConsoleIds.RcConsoleAtariLynx:
                return HashRom.RcHashLynx(out hash, iterator);

            case ConsoleIds.RcConsoleFamicomDiskSystem:
            case ConsoleIds.RcConsoleNintendo:
                return HashRom.RcHashNes(out hash, iterator);

            case ConsoleIds.RcConsolePcEngine: /* NOTE: does not support PCEngine CD */
                return HashRom.RcHashPce(out hash, iterator);

            case ConsoleIds.RcConsoleSuperCassettevision:
                return HashRom.RcHashScv(out hash, iterator);

            case ConsoleIds.RcConsoleSuperNintendo:
                return HashRom.RcHashSnes(out hash, iterator);

            case ConsoleIds.RcConsoleNintendo64:
            case ConsoleIds.RcConsoleNintendo3Ds:
            case ConsoleIds.RcConsoleNintendoDs:
            case ConsoleIds.RcConsoleNintendoDsi:
                return FileFromBuffer(out hash, consoleId, iterator);
        }
    }

    /* ===================================================== */
    /* iterator reset / callback merge                       */

    /// <summary>===================================================== iterator reset / callback merge</summary>
    /// <param name="iterator">the hash iterator</param>
    public static void ResetIterator(RcHashIterator iterator)
    {
        iterator.Buffer = null;
        iterator.BufferSize = 0;
        iterator.Consoles = new uint[12];
        iterator.Index = -1;
        iterator.Path = null;
        iterator.Userdata = null;
        iterator.Callbacks = new RcHashCallbacks();

        if (_gVerboseMessageCallback != null)
        {
            iterator.Callbacks.VerboseMessage = CallGVerboseMessageCallback;
        }

        if (_gErrorMessageCallback != null)
        {
            iterator.Callbacks.ErrorMessage = CallGErrorMessageCallback;
        }

        if (_gFilereader != null)
        {
            iterator.Callbacks.Filereader = _gFilereader;
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
    /// <summary>ported verbatim, including the upstream quirk: the C code assigns callbacks-&gt;error_message to iterator-&gt;callbacks.verbose_message</summary>
    /// <param name="iterator">the hash iterator</param>
    /// <param name="callbacks">the callbacks parameter</param>
    public static void MergeCallbacks(RcHashIterator iterator, RcHashCallbacks callbacks)
    {
        if (callbacks.VerboseMessage != null)
        {
            iterator.Callbacks.VerboseMessage = callbacks.VerboseMessage;
        }

        if (callbacks.ErrorMessage != null)
        {
            iterator.Callbacks.ErrorMessage = callbacks.ErrorMessage;
        }

        if (callbacks.Filereader.Open != null)
        {
            iterator.Callbacks.Filereader = callbacks.Filereader;
        }

        if (callbacks.Cdreader.OpenTrack != null)
        {
            iterator.Callbacks.Cdreader = callbacks.Cdreader;
        }

        if (callbacks.Encryption.Get3DsCiaNormalKey != null)
        {
            iterator.Callbacks.Encryption.Get3DsCiaNormalKey = callbacks.Encryption.Get3DsCiaNormalKey;
        }

        if (callbacks.Encryption.Get3DsNcchNormalKeys != null)
        {
            iterator.Callbacks.Encryption.Get3DsNcchNormalKeys = callbacks.Encryption.Get3DsNcchNormalKeys;
        }
    }
}
