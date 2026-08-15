// RvzFilereader — GameCube/Wii disc reader over RVZSharp.
//
// RcHashGamecube and RcHashWii (HashDisc.cs) treat the disc image as a plain
// random-access file: FileOpen + FileSeek/FileRead over an ISO. An RVZ/WIA
// container is therefore served by mapping those reads onto RVZSharp's
// RvzReader.ReadAt, which decodes the stored chunks on demand and yields the
// original disc image byte-for-byte (Wii partition data re-encrypted, hash
// trees rebuilt) — no rvz->iso conversion needed.

using RetroAchievementsSharp.Models;
using Serilog;
using RVZSharp;

namespace RetroAchievementsSharp;

/// <summary>RvzFilereader — GameCube/Wii disc-image reader over RVZSharp. Mirrors ChdCdReader for the disc path, but for the filereader API (RcHashGamecube/RcWii read the ISO as a flat random-access file): FileOpen yields an RVZ-backed handle whose Seek/Read map onto the decoded image.</summary>
public static class RvzFilereader
{
    /* the file handle the engine sees: the decoded ISO view plus the
     * random-access position kept by the filereader emulation */
    private sealed class RvzFileHandle : IDisposable
    {
        public RvzReader Reader = null!;
        public long Position;

        public void Dispose()
        {
            Reader.Dispose();
        }
    }

    private static object? Open(string path)
    {
        var stream = FileUtil.OpenFile(path);
        if (stream == null)
        {
            return null;
        }

        try
        {
            var handle = new RvzFileHandle { Reader = RvzReader.Open(stream) };
            return handle;
        }
        catch (Exception ex)
        {
            stream.Dispose();
            Log.Debug(ex, "RvzFilereader: failed to open RVZ {Path}", path);
            return null;
        }
    }

    private static void Seek(object fileHandle, long offset, int origin)
    {
        var handle = (RvzFileHandle)fileHandle;
        var length = handle.Reader.Length;
        switch (origin)
        {
            case HashEngine.SeekSet:
                handle.Position = offset;
                break;
            case HashEngine.SeekCur:
                handle.Position += offset;
                break;
            case HashEngine.SeekEnd:
                handle.Position = length + offset;
                break;
        }
    }

    private static long Tell(object fileHandle)
    {
        return ((RvzFileHandle)fileHandle).Position;
    }

    private static int Read(object fileHandle, byte[] buffer, int requestedBytes)
    {
        var handle = (RvzFileHandle)fileHandle;
        var read = handle.Reader.ReadAt(handle.Position, buffer.AsSpan(0, requestedBytes));
        handle.Position += read;
        return read;
    }

    private static void Close(object fileHandle)
    {
        ((RvzFileHandle)fileHandle).Dispose();
    }

    /* rc_hash_init_chd_cdreader equivalent for the filereader API: installs
     * the RVZ-backed reader as the global filereader so ResetIterator picks it
     * up for GameCube/Wii disc hashing (call InitCustomFilereader(null) to
     * restore the default plain-file reader afterwards). */
    /// <summary>Installs the RVZ-backed reader as the global filereader so ResetIterator picks it up for GameCube/Wii disc hashing. Restore with InitCustomFilereader(null).</summary>
    public static void InitRvzFilereader()
    {
        var filereader = new RcHashFilereader
        {
            Open = Open,
            Seek = Seek,
            Tell = Tell,
            Read = Read,
            Close = Close
        };

        HashEngine.InitCustomFilereader(filereader);
    }
}