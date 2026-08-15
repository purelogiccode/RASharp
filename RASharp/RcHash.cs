// Ported from rcheevos (MIT) — include/rc_hash.h
// Public API mirror. Deprecated single-shot functions plus the global init
// functions; the iterator-based API lives on HashIterator.

using RASharp.Models;
using Serilog;

namespace RASharp;

/// <summary>Ported from rcheevos (MIT) — include/rc_hash.h Public API mirror. Deprecated single-shot functions plus the global init functions; the iterator-based API lives </summary>
public static class RcHash
{
    /* deprecated global message callbacks */
    /// <summary>deprecated global message callbacks</summary>
    /// <param name="callback">the callback to register</param>
    public static void InitErrorMessageCallback(RcHashMessageCallbackDeprecated? callback)
    {
        HashEngine.HashInitErrorMessageCallback(callback);
    }

    /// <summary>Registers the global verbose message callback.</summary>
    /// <param name="callback">the callback to register</param>
    public static void InitVerboseMessageCallback(RcHashMessageCallbackDeprecated? callback)
    {
        HashEngine.HashInitVerboseMessageCallback(callback);
    }

    /* deprecated global filereader */
    /// <summary>deprecated global filereader</summary>
    /// <param name="reader">the reader to register</param>
    public static void InitCustomFilereader(RcHashFilereader? reader)
    {
        HashEngine.InitCustomFilereader(reader);
    }

    /* deprecated global cdreader */
    /// <summary>deprecated global cdreader</summary>
    /// <param name="cdreader">the cdreader parameter</param>
    public static void GetDefaultCdreader(RcHashCdreader cdreader)
    {
        HashEngine.GetDefaultCdreader(cdreader);
    }

    /// <summary>Registers the default CD reader as the global cdreader.</summary>
    public static void InitDefaultCdreader()
    {
        var cdreader = new RcHashCdreader();
        GetDefaultCdreader(cdreader);
        InitCustomCdreader(cdreader);
    }

    /// <summary>Registers a custom global cdreader.</summary>
    /// <param name="reader">the reader to register</param>
    public static void InitCustomCdreader(RcHashCdreader? reader)
    {
        HashEngine.InitCustomCdreader(reader);
    }

    /* deprecated global 3DS key functions */
    /// <summary>deprecated global 3DS key functions</summary>
    /// <param name="func">the func parameter</param>
    public static void Init3DsGetCiaNormalKeyFunc(RcHash3DsGetCiaNormalKeyFunc func)
    {
        HashEngine.HashInit3DsGetCiaNormalKeyFunc(func);
    }

    /// <summary>Registers the 3DS NCCH normal-keys provider.</summary>
    /// <param name="func">the func parameter</param>
    public static void Init3DsGetNcchNormalKeysFunc(RcHash3DsGetNcchNormalKeysFunc func)
    {
        HashEngine.HashInit3DsGetNcchNormalKeysFunc(func);
    }

    /* deprecated single-shot hashing */
    /// <summary>deprecated single-shot hashing</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="bufferSize">the size of the buffer</param>
    /// <returns>true on success; otherwise false</returns>
    public static bool GenerateFromBuffer(out string hash, uint consoleId, byte[] buffer, int bufferSize)
    {
        hash = "";

        try
        {
            var iterator = new RcHashIterator();
            HashEngine.ResetIterator(iterator);
            iterator.Buffer = buffer;
            iterator.BufferSize = bufferSize;

            var result = HashEngine.FromBuffer(out hash, consoleId, iterator);

            HashIterator.DestroyIterator(iterator);
            return result != 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GenerateFromBuffer failed: console {ConsoleId}, buffer size {BufferSize}", consoleId, bufferSize);
            return false;
        }
    }

    /// <summary>Generates the hash for a console from a file on disk.</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="path">the file path</param>
    /// <returns>true on success; otherwise false</returns>
    public static bool GenerateFromFile(out string hash, uint consoleId, string path)
    {
        hash = "";

        try
        {
            var iterator = new RcHashIterator();
            HashEngine.ResetIterator(iterator);
            iterator.Path = path;

            var result = HashEngine.FromFile(out hash, consoleId, iterator);

            HashIterator.DestroyIterator(iterator);
            return result != 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GenerateFromFile failed: console {ConsoleId}, path {Path}", consoleId, path);
            return false;
        }
    }
}
