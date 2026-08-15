// Phase 5 — CHD reader tests.
// Uses vendored synthetic CHDs (created with chdman from generated disc
// images; the expected hashes are the outputs of the original RAHasher 1.8.3
// binary with HAVE_CHD=1, verified during development):
//   psx.chd   — single MODE2/2352 track, PSX disc (SYSTEM.CNF + SLUS_007.45)
//   psp.chd   — single MODE2/2352 track, PSP disc (PARAM.SFO + EBOOT.BIN)
//   multi.chd — 3 tracks (AUDIO 300 + MODE2/2352 271 + AUDIO 200)

using CHDSharp;
using RASharp;
using RASharp.Models;
using VideoGameFileSystemParser.Models;

namespace RASharp.Tests;

/// <summary>Phase 5 — CHD reader tests. Uses vendored synthetic CHDs (created with chdman from generated disc images; the expected hashes are the outputs of the original RA</summary>
public class TestChdCdReader
{
    private static string TestDataPath(string name)
    {
        return Path.Combine(AppContext.BaseDirectory, "TestData", name);
    }

    private static RcHashIterator NewIterator()
    {
        /* populate the iterator's cdreader from the registered global
         * (ResetIterator does this in the generate-from-file flow) */
        var iterator = new RcHashIterator();
        iterator.Callbacks.Cdreader = HashEngine.GetGlobalCdreader()!;
        return iterator;
    }

    private static ChdCdReaderHandle OpenTrack(string chdPath, uint track)
    {
        ChdCdReader.InitChdCdreader();
        var iterator = NewIterator();
        var handle = iterator.Callbacks.Cdreader.OpenTrackIterator!(chdPath, track, iterator);
        Assert.NotNull(handle);
        return new ChdCdReaderHandle(iterator, handle);
    }

    private sealed class ChdCdReaderHandle : IDisposable
    {
        public readonly RcHashIterator Iterator;
        public readonly object Handle;

        public ChdCdReaderHandle(RcHashIterator iterator, object handle)
        {
            Iterator = iterator;
            Handle = handle;
        }

        /// <summary>Releases the mounted filesystem.</summary>
        public void Dispose()
        {
            Iterator.Callbacks.Cdreader.CloseTrack!(Handle);
        }
    }

    /// <summary>Tests chd open tracks.</summary>
    [Fact]
    public void TestChdOpenTracks()
    {
        var path = TestDataPath("multi.chd");

        /* track 1: AUDIO, 300 frames */
        using (ChdCdReaderHandle track = OpenTrack(path, 1))
        {
            Assert.Equal(0u, track.Iterator.Callbacks.Cdreader.FirstTrackSector!(track.Handle));
            var buffer = new byte[16];
            Assert.Equal(16, track.Iterator.Callbacks.Cdreader.ReadSector!(track.Handle, 0, buffer, 16));
            Assert.Equal((byte)0x55, buffer[0]);
            Assert.Equal((byte)0x55, buffer[15]);
        }

        /* track 2: MODE2_RAW data, 271 frames (probe: CD001 at raw offset 17 -> header 16) */
        using (ChdCdReaderHandle track = OpenTrack(path, 2))
        {
            Assert.Equal(300u, track.Iterator.Callbacks.Cdreader.FirstTrackSector!(track.Handle));

            /* sector 300 = first data sector; the read starts at the data
             * region (sync/header skipped), which is the ISO9660 boot area */
            var buffer = new byte[16];
            Assert.Equal(16, track.Iterator.Callbacks.Cdreader.ReadSector!(track.Handle, 300, buffer, 16));
            Assert.Equal(0x00, buffer[0]);
            Assert.Equal(0x00, buffer[1]);

            /* sector 316 = ISO9660 volume descriptor: 0x01 + "CD001" */
            var vd = new byte[32];
            Assert.Equal(32, track.Iterator.Callbacks.Cdreader.ReadSector!(track.Handle, 316, vd, 32));
            Assert.Equal(0x01, vd[0]);
            Assert.Equal((byte)'C', vd[1]);
            Assert.Equal((byte)'D', vd[2]);
            Assert.Equal((byte)'0', vd[3]);
            Assert.Equal((byte)'0', vd[4]);
            Assert.Equal((byte)'1', vd[5]);
        }

        /* track 3: AUDIO, 200 frames */
        using (ChdCdReaderHandle track = OpenTrack(path, 3))
        {
            Assert.Equal(571u, track.Iterator.Callbacks.Cdreader.FirstTrackSector!(track.Handle));
            var buffer = new byte[4];
            Assert.Equal(4, track.Iterator.Callbacks.Cdreader.ReadSector!(track.Handle, 571, buffer, 4));
            Assert.Equal((byte)0xAA, buffer[0]);
        }
    }

    /// <summary>Tests chd pregap.</summary>
    [Fact]
    public void TestChdPregap()
    {
        /* 2-track disc with a 2-second pregap before the data track:
         * track 1 = 50 AUDIO frames, track 2 = data with PREGAP 00:02:00.
         * sector_offset must stay 50 (pregap is not a sector), while the
         * frame math must skip the 150 pregap frames. */
        var path = TestDataPath("pregap.chd");

        using (ChdCdReaderHandle track = OpenTrack(path, 1))
        {
            Assert.Equal(0u, track.Iterator.Callbacks.Cdreader.FirstTrackSector!(track.Handle));
        }

        using (ChdCdReaderHandle track = OpenTrack(path, 2))
        {
            Assert.Equal(50u, track.Iterator.Callbacks.Cdreader.FirstTrackSector!(track.Handle));

            /* HashCHD.cpp adds both the track alignment padding (50 -> 52
             * frames) AND the pregap (150) to the frame offset, even though
             * chdman stores no pregap frames (pregap is metadata-only).
             * Verified against RAHasher 1.8.3: both report "Not a PC-FX CD"
             * on this CHD. So sector 66 maps to CHD frame 52 + 150 + 16 = 218
             * (image sector 166, NOT the volume descriptor), and the format
             * probe (no CD001 in a fill sector) yields header 16. */
            var buffer = new byte[16];
            Assert.Equal(16, track.Iterator.Callbacks.Cdreader.ReadSector!(track.Handle, 66, buffer, 16));

            ChdFile.Open(path, out ChdFile? chd);
            var framesPerHunk = (int)(chd!.HunkBytes / chd.UnitBytes);
            var hunk = new byte[(int)chd.HunkBytes];
            chd.ReadHunk((uint)(218 / framesPerHunk), hunk);
            var off = (218 % framesPerHunk) * (int)chd.UnitBytes;
            for (var i = 0; i < 16; i++)
                Assert.Equal(hunk[off + 16 + i], buffer[i]);
            chd.Dispose();
        }
    }

    /// <summary>Tests chd track selection.</summary>
    [Fact]
    public void TestChdTrackSelection()
    {
        var path = TestDataPath("multi.chd");

        /* FIRST_DATA -> track 2 */
        using (ChdCdReaderHandle track = OpenTrack(path, ConsoleIds.RcHashCdtrackFirstData))
        {
            Assert.Equal(300u, track.Iterator.Callbacks.Cdreader.FirstTrackSector!(track.Handle));
        }

        /* LARGEST -> track 2, but the C re-fetches the metadata without
         * recomputing the offsets, so first_track_sector reports track 3's
         * offset (571) — a quirk of HashCHD.cpp reproduced 1:1; verified
         * against RAHasher 1.8.3 on this exact CHD (both fail PC-FX with
         * "Not a PC-FX CD") */
        using (ChdCdReaderHandle track = OpenTrack(path, ConsoleIds.RcHashCdtrackLargest))
        {
            Assert.Equal(571u, track.Iterator.Callbacks.Cdreader.FirstTrackSector!(track.Handle));
        }

        /* LAST -> track 3 */
        using (ChdCdReaderHandle track = OpenTrack(path, ConsoleIds.RcHashCdtrackLast))
        {
            Assert.Equal(571u, track.Iterator.Callbacks.Cdreader.FirstTrackSector!(track.Handle));
        }

        /* FIRST_OF_SECOND_SESSION -> track 2 (CHD has no sessions) */
        using (ChdCdReaderHandle track = OpenTrack(path, ConsoleIds.RcHashCdtrackFirstOfSecondSession))
        {
            Assert.Equal(300u, track.Iterator.Callbacks.Cdreader.FirstTrackSector!(track.Handle));
        }
    }

    /// <summary>Tests chd metadata vs chd sharp tracks.</summary>
    [Fact]
    public void TestChdMetadataVsChdSharpTracks()
    {
        /* CHDSharp's pre-parsed Tracks must agree with the raw metadata string
         * parsing (the C parses the strings; CHDSharp normalizes) */
        foreach (var name in new[] { "psx.chd", "psp.chd", "multi.chd", "pregap.chd" })
        {
            var path = TestDataPath(name);
            ChdFile.Open(path, out ChdFile? file);
            Assert.NotNull(file);

            var parsed = ChdCdReader.ParseTrackTable(file);
            var tracks = file.Tracks!;

            Assert.Equal(tracks.Count, parsed.Count);
            for (var i = 0; i < parsed.Count; i++)
            {
                Assert.Equal((uint)tracks[i].TrackNumber, parsed[i].Track);
                Assert.Equal((uint)tracks[i].Frames, parsed[i].Frames);
                Assert.Equal((uint)tracks[i].PreGap, parsed[i].Pregap);
                Assert.Equal((uint)tracks[i].PostGap, parsed[i].Postgap);
                /* TrackInfo.DataSize = data bytes per sector (CHDSharp's normalization) */
                var expectedDataSize = parsed[i].Type switch
                {
                    "AUDIO" => 2352u,
                    "MODE1" => 2048u,
                    "MODE1_RAW" => 2048u,
                    "MODE2_RAW" => 2352u,
                    _ => 2352u
                };
                Assert.Equal(expectedDataSize, (uint)tracks[i].DataSize);
            }

            file.Dispose();
        }
    }

    /// <summary>Tests chd hash psx.</summary>
    [Fact]
    public void TestChdHashPsx()
    {
        /* hash of psx.chd verified against RAHasher 1.8.3 (HAVE_CHD=1):
         * db433fb038cde4fb15c144e8c7dea6e3 (identical to the ISO hash) */
        ChdCdReader.InitChdCdreader();
        Assert.True(RcHash.GenerateFromFile(out var hash, ConsoleIds.RcConsolePlaystation, TestDataPath("psx.chd")));
        Assert.Equal("db433fb038cde4fb15c144e8c7dea6e3", hash);
    }

    /// <summary>Tests chd hash psp.</summary>
    [Fact]
    public void TestChdHashPsp()
    {
        /* hash of psp.chd verified against RAHasher 1.8.3 (HAVE_CHD=1):
         * a7070bf07f5c1a0afb2b2d202d7e3893 */
        ChdCdReader.InitChdCdreader();
        Assert.True(RcHash.GenerateFromFile(out var hash, ConsoleIds.RcConsolePsp, TestDataPath("psp.chd")));
        Assert.Equal("a7070bf07f5c1a0afb2b2d202d7e3893", hash);
    }

    /* ========================================================================= */
    /* Phase 4 deferred item: mini-parser vs VideoGameFileSystemParser agreement */

    private static void AssertBackendsAgree(string chdPath, ConsoleType consoleType, string[] paths)
    {
        /* mini-parser (default backend) */
        ChdCdReader.InitChdCdreader();
        var iterator = NewIterator();
        var trackHandle = iterator.Callbacks.Cdreader.OpenTrackIterator!(chdPath, 1, iterator);

        /* library backend */
        using var resolver = new FileSystemResolver(chdPath, consoleType);

        foreach (var path in paths)
        {
            var miniSector = (uint)HashDisc.CdFindFileSector(iterator, trackHandle, path, out var miniSize);
            Assert.True(miniSector != 0, $"mini-parser could not resolve {path}");

            FileEntry? entry = resolver.Find(path);
            Assert.NotNull(entry);
            Assert.Equal(entry.Lba, miniSector);
            Assert.Equal(entry.Size, miniSize);

            /* the resolver's TryResolve must agree too */
            Assert.True(resolver.TryResolve(path, out var lba, out var size));
            Assert.Equal(entry.Lba, lba);
            Assert.Equal(entry.Size, size);
        }

        iterator.Callbacks.Cdreader.CloseTrack!(trackHandle!);
    }

    /// <summary>Tests fs resolver agreement psx.</summary>
    [Fact]
    public void TestFsResolverAgreementPsx()
    {
        AssertBackendsAgree(TestDataPath("psx.chd"), ConsoleType.Ps1,
            ["SYSTEM.CNF", "SLUS_007.45"]);
    }

    /// <summary>Tests fs resolver agreement psp.</summary>
    [Fact]
    public void TestFsResolverAgreementPsp()
    {
        AssertBackendsAgree(TestDataPath("psp.chd"), ConsoleType.Psp,
            ["PSP_GAME\\PARAM.SFO", @"PSP_GAME\SYSDIR\EBOOT.BIN"]);
    }
}
