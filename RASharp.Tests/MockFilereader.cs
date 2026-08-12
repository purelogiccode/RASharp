// Ported from rcheevos (MIT) — test/rhash/mock_filereader.c
// In-memory filereader for the vector tests (CD reader parts deferred to Phase 3).

using RASharp.Core;

namespace RASharp.Tests;

public sealed class MockFileData
{
    public string Path = "";
    public byte[]? Data;
    public long Size;
    public long Pos;
    public int FirstSector;
}

public static class MockFilereader
{
    private static readonly MockFileData[] Instances = new MockFileData[16];
    private static int mockCdTracks;

    private static object? MockFileOpen(string path)
    {
        foreach (var file in Instances)
        {
            if (string.Equals(file.Path, path, StringComparison.Ordinal))
            {
                file.Pos = 0;
                return file;
            }
        }

        return null;
    }

    private static void MockFileSeek(object fileHandle, long offset, int origin)
    {
        var file = (MockFileData)fileHandle;
        switch (origin)
        {
            case 0: /* SEEK_SET */
                file.Pos = offset;
                break;
            case 1: /* SEEK_CUR */
                file.Pos += offset;
                break;
            case 2: /* SEEK_END */
                file.Pos = file.Size - offset;
                break;
        }

        if (file.Pos > file.Size)
            file.Pos = file.Size;
    }

    private static long MockFileTell(object fileHandle)
    {
        return ((MockFileData)fileHandle).Pos;
    }

    private static int MockFileRead(object fileHandle, byte[] buffer, int count)
    {
        var file = (MockFileData)fileHandle;
        long remaining = file.Size - file.Pos;
        if (count > remaining)
            count = (int)remaining;

        if (count > 0)
        {
            if (file.Data != null)
                Array.Copy(file.Data, file.Pos, buffer, 0, count);
            else
                Array.Clear(buffer, 0, count);

            file.Pos += count;
        }

        return count;
    }

    private static void MockFileClose(object fileHandle)
    {
    }

    private static void ResetMockFiles()
    {
        for (int i = 0; i < Instances.Length; ++i)
            Instances[i] = new MockFileData { Path = "" };

        mockCdTracks = 0;
    }

    public static RcHashFilereader GetMockFilereader()
    {
        var reader = new RcHashFilereader
        {
            Open = MockFileOpen,
            Seek = MockFileSeek,
            Tell = MockFileTell,
            Read = MockFileRead,
            Close = MockFileClose,
        };

        ResetMockFiles();
        return reader;
    }

    public static void InitMockFilereader()
    {
        RcHash.InitCustomFilereader(GetMockFilereader());
    }

    public static void MockFile(int index, string filename, byte[]? buffer, int bufferSize)
    {
        if (index == 0)
            ResetMockFiles();

        Instances[index].Path = filename;
        Instances[index].Data = buffer;
        Instances[index].Size = bufferSize;
        Instances[index].Pos = 0;
        Instances[index].FirstSector = 0;
    }

    public static void MockFileText(int index, string filename, string contents)
    {
        MockFile(index, filename, System.Text.Encoding.ASCII.GetBytes(contents), contents.Length);
    }

    public static void MockFileSize(int index, int mockSize)
    {
        Instances[index].Size = mockSize;
    }

    public static void MockEmptyFile(int index, string filename, int mockSize)
    {
        MockFile(index, filename, null, mockSize);
    }

    public static void MockCdNumTracks(int numTracks)
    {
        mockCdTracks = numTracks;
    }

    public static void MockFileFirstSector(int index, int firstSector)
    {
        Instances[index].FirstSector = firstSector;
    }

    /* ===================================================== */
    /* mock cdreader (mock_filereader.c's _mock_cd_* funcs)  */

    private static object? MockCdOpenTrack(string path, uint track)
    {
        if (track == ConsoleIds.RC_HASH_CDTRACK_LAST)
            track = (uint)mockCdTracks;

        if (track == 1 || track == ConsoleIds.RC_HASH_CDTRACK_FIRST_DATA || track == ConsoleIds.RC_HASH_CDTRACK_LARGEST)
        {
            if (path.Contains(".cue", StringComparison.Ordinal))
            {
                MockFileData? file = (MockFileData?)MockFileOpen(path);
                if (file == null)
                    return file;

                return MockFileOpen(System.Text.Encoding.ASCII.GetString(file.Data ?? Array.Empty<byte>()));
            }

            return MockFileOpen(path);
        }
        else if (path.Contains(".cue", StringComparison.Ordinal))
        {
            MockFileData? file = (MockFileData?)MockFileOpen(path);
            if (file != null)
            {
                string data = System.Text.Encoding.ASCII.GetString(file.Data ?? Array.Empty<byte>());
                int fileLen = data.Length;
                string buffer = data.Substring(0, fileLen - 4) + track.ToString() + data.Substring(fileLen - 4);
                return MockFileOpen(buffer);
            }
        }
        else if (path.Contains(".gdi", StringComparison.Ordinal))
        {
            MockFileData? file = (MockFileData?)MockFileOpen(path);
            if (file != null)
            {
                string buffer = string.Format("track{0:D2}.bin", track);
                return MockFileOpen(buffer);
            }
        }

        return null;
    }

    private static int MockCdReadSector(object trackHandle, uint sector, byte[] buffer, int requestedBytes)
    {
        var file = (MockFileData)trackHandle;
        sector -= (uint)file.FirstSector;
        MockFileSeek(trackHandle, sector * 2048, 0 /* SEEK_SET */);
        return MockFileRead(trackHandle, buffer, requestedBytes);
    }

    private static uint MockCdFirstTrackSector(object trackHandle)
    {
        return (uint)((MockFileData)trackHandle).FirstSector;
    }

    public static void InitMockCdreader()
    {
        var cdreader = new RcHashCdreader
        {
            OpenTrack = MockCdOpenTrack,
            CloseTrack = MockFileClose,
            ReadSector = MockCdReadSector,
            FirstTrackSector = MockCdFirstTrackSector,
        };

        RcHash.InitCustomCdreader(cdreader);

        mockCdTracks = 0;
    }

    public static string GetMockFilename(object fileHandle)
    {
        return ((MockFileData)fileHandle).Path;
    }
}
