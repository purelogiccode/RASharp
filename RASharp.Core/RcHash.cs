// Ported from rcheevos (MIT) — include/rc_hash.h
// Public API mirror. Deprecated single-shot functions plus the global init
// functions; the iterator-based API lives on HashIterator.

namespace RASharp.Core;

using Serilog;

public static class RcHash
{
    /* deprecated global message callbacks */
    public static void InitErrorMessageCallback(RcHashMessageCallbackDeprecated? callback)
    {
        HashEngine.HashInitErrorMessageCallback(callback);
    }

    public static void InitVerboseMessageCallback(RcHashMessageCallbackDeprecated? callback)
    {
        HashEngine.HashInitVerboseMessageCallback(callback);
    }

    /* deprecated global filereader */
    public static void InitCustomFilereader(RcHashFilereader? reader)
    {
        HashEngine.InitCustomFilereader(reader);
    }

    /* deprecated global cdreader */
    public static void GetDefaultCdreader(RcHashCdreader cdreader)
    {
        HashEngine.GetDefaultCdreader(cdreader);
    }

    public static void InitDefaultCdreader()
    {
        var cdreader = new RcHashCdreader();
        GetDefaultCdreader(cdreader);
        InitCustomCdreader(cdreader);
    }

    public static void InitCustomCdreader(RcHashCdreader? reader)
    {
        HashEngine.InitCustomCdreader(reader);
    }

    /* deprecated global 3DS key functions */
    public static void Init3DsGetCiaNormalKeyFunc(RcHash3DsGetCiaNormalKeyFunc func)
    {
        HashEngine.HashInit3DsGetCiaNormalKeyFunc(func);
    }

    public static void Init3DsGetNcchNormalKeysFunc(RcHash3DsGetNcchNormalKeysFunc func)
    {
        HashEngine.HashInit3DsGetNcchNormalKeysFunc(func);
    }

    /* deprecated single-shot hashing */
    public static bool GenerateFromBuffer(out string hash, uint consoleId, byte[] buffer, int bufferSize)
    {
        hash = "";

        try
        {
            var iterator = new RcHashIterator();
            HashEngine.ResetIterator(iterator);
            iterator.Buffer = buffer;
            iterator.BufferSize = bufferSize;

            int result = HashEngine.FromBuffer(out hash, consoleId, iterator);

            HashIterator.DestroyIterator(iterator);
            return result != 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GenerateFromBuffer failed: console {ConsoleId}, buffer size {BufferSize}", consoleId, bufferSize);
            return false;
        }
    }

    public static bool GenerateFromFile(out string hash, uint consoleId, string path)
    {
        hash = "";

        try
        {
            var iterator = new RcHashIterator();
            HashEngine.ResetIterator(iterator);
            iterator.Path = path;

            int result = HashEngine.FromFile(out hash, consoleId, iterator);

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
