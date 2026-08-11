// Ported from rcheevos (MIT) — test/rhash/test_cdreader.c
// Cue/gdi track-open semantics, sector-size determination, and read_sector.

using RASharp.Core;
using Xunit;

namespace RASharp.Tests;

public class TestCdreader
{
    private static readonly byte[] SyncPattern =
    {
        0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00
    };

    private static readonly string CueSingleTrack =
        "FILE \"game.bin\" BINARY\n" +
        "  TRACK 01 MODE2/2352\n" +
        "    INDEX 01 00:00:00\n";

    private static readonly string CueSingleBinMultipleData =
        "FILE \"game.bin\" BINARY\n" +
        "  TRACK 01 AUDIO\n" +
        "    INDEX 01 00:00:00\n" +
        "  TRACK 02 MODE1/2352\n" +
        "    PREGAP 00:03:00\n" +
        "    INDEX 01 00:55:45\n" +
        "  TRACK 03 MODE1/2352\n" +
        "    INDEX 01 11:30:74\n" +
        "  TRACK 04 MODE1/2352\n" +
        "    INDEX 01 13:31:51\n" +
        "  TRACK 05 MODE1/2352\n" +
        "    INDEX 01 13:48:56\n" +
        "  TRACK 06 MODE1/2352\n" +
        "    INDEX 01 34:48:19\n" +
        "  TRACK 07 MODE1/2352\n" +
        "    INDEX 01 50:42:74\n" +
        "  TRACK 08 MODE1/2352\n" +
        "    INDEX 01 55:20:74\n" +
        "  TRACK 09 MODE1/2352\n" +
        "    INDEX 01 56:25:67\n" +
        "  TRACK 10 MODE1/2352\n" +
        "    INDEX 01 59:04:08\n" +
        "  TRACK 11 MODE1/2352\n" +
        "    INDEX 01 61:17:18\n" +
        "  TRACK 12 MODE1/2352\n" +
        "    INDEX 01 62:44:33\n" +
        "  TRACK 13 AUDIO\n" +
        "    PREGAP 00:02:00\n" +
        "    INDEX 01 66:24:37\n";

    private static readonly string CueMultipleBinMultipleData =
        "FILE \"track1.bin\" BINARY\n" +
        "  TRACK 01 AUDIO\n" +
        "    INDEX 01 00:00:00\n" +
        "FILE \"track2.bin\" BINARY\n" +
        "  TRACK 02 MODE1/2352\n" +
        "    INDEX 00 00:00:00\n" +
        "    INDEX 01 00:03:00\n" +
        "FILE \"track3.bin\" BINARY\n" +
        "  TRACK 03 MODE1/2352\n" +
        "    INDEX 00 00:00:00\n" +
        "    INDEX 01 00:02:00\n" +
        "FILE \"track4.bin\" BINARY\n" +
        "  TRACK 04 AUDIO\n" +
        "    INDEX 00 00:00:00\n";

    private static readonly string GdiThreeTracks =
        "3\n" +
        "1 0 4 2352 track01.bin 0\n" +
        "2 600 0 2352 track02.raw 0\n" +
        "3 45000 4 2352 track03.bin 0";

    private static readonly string GdiManyTracks =
        "26\n" +
        "1 0 4 2352 track01.bin 0\n" +
        "2 450 0 2352 track02.raw 0\n" +
        "3 45000 4 2352 track03.bin 0\n" +
        "4 370673 0 2352 track04.raw 0\n" +
        "5 371347 0 2352 track05.raw 0\n" +
        "6 372014 0 2352 track06.raw 0\n" +
        "7 372915 0 2352 track07.raw 0\n" +
        "8 373626 0 2352 track08.raw 0\n" +
        "9 379011 0 2352 track09.raw 0\n" +
        "10 384738 0 2352 track10.raw 0\n" +
        "11 390481 0 2352 track11.raw 0\n" +
        "12 395473 0 2352 track12.raw 0\n" +
        "13 398926 0 2352 track13.raw 0\n" +
        "14 404448 0 2352 track14.raw 0\n" +
        "15 425246 0 2352 track15.raw 0\n" +
        "16 445520 0 2352 track16.raw 0\n" +
        "17 466032 0 2352 track17.raw 0\n" +
        "18 474231 0 2352 track18.raw 0\n" +
        "19 485598 0 2352 track19.raw 0\n" +
        "20 486386 0 2352 track20.raw 0\n" +
        "21 487098 0 2352 track21.raw 0\n" +
        "22 487822 0 2352 track22.raw 0\n" +
        "23 498356 0 2352 track23.raw 0\n" +
        "24 508297 0 2352 track24.raw 0\n" +
        "25 527383 0 2352 track25.raw 0\n" +
        "26 548106 4 2352 track26.bin 0\n";

    public TestCdreader()
    {
        MockFilereader.InitMockFilereader();
        RcHash.InitDefaultCdreader();
    }

    private static RcHashIterator InitializeIterator()
    {
        var iterator = new RcHashIterator();
        CdReader.GetDefaultCdreader(iterator.Callbacks.Cdreader);
        iterator.Callbacks.Filereader = MockFilereader.GetMockFilereader();
        return iterator;
    }

    private static CdromTrack? OpenTrack(RcHashIterator iterator, string path, uint index)
    {
        return (CdromTrack?)iterator.Callbacks.Cdreader.OpenTrackIterator!(path, index, iterator);
    }

    private static void CloseTrack(RcHashIterator iterator, CdromTrack? trackHandle)
    {
        iterator.Callbacks.Cdreader.CloseTrack!(trackHandle!);
    }

    private static void AssertTrackCommon(CdromTrack? track, string binFilename, long fileTrackOffset, int sectorSize)
    {
        Assert.NotNull(track);
        Assert.NotNull(track!.FileHandle);
        Assert.Equal(binFilename, MockFilereader.GetMockFilename(track.FileHandle));
        Assert.Equal(fileTrackOffset, track.FileTrackOffset);
        Assert.Equal(sectorSize, track.SectorSize);
    }

    [Fact]
    public void TestOpenCueTrack2()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", CueSingleBinMultipleData);
        MockFilereader.MockEmptyFile(1, "game.bin", 718310208);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 2);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 9807840, 2352); /* track 2: 0x95A7E0 */
        Assert.Equal(16, trackHandle!.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenCueTrack12()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", CueSingleBinMultipleData);
        MockFilereader.MockEmptyFile(1, "game.bin", 718310208);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 12);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 664047216, 2352); /* track 12: 0x27948E70 */
        Assert.Equal(16, trackHandle!.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenCueTrack14()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", CueSingleBinMultipleData);
        MockFilereader.MockEmptyFile(1, "game.bin", 718310208);

        /* only 13 tracks */
        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 14);
        Assert.Null(trackHandle);
    }

    [Fact]
    public void TestOpenCueTrackMissingBin()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", CueSingleBinMultipleData);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 2);
        Assert.Null(trackHandle);
    }

    [Fact]
    public void TestOpenGdiTrack3()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.gdi", GdiThreeTracks);
        MockFilereader.MockEmptyFile(1, "track03.bin", 1185760800);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.gdi", 3);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "track03.bin", 0, 2352);
        Assert.Equal(0, trackHandle!.TrackPregapSectors);
        Assert.Equal(45000, trackHandle.TrackFirstSector);
        Assert.Equal(16, trackHandle.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenGdiTrack3Quoted()
    {
        const string gdiContents =
            "3\n" +
            "1 0 4 2352 \"track 01.bin\" 0\n" +
            "2 600 0 2352 \"track 02.raw\" 0\n" +
            "3 45000 4 2352 \"track 03.bin\" 0";

        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.gdi", gdiContents);
        MockFilereader.MockEmptyFile(1, "track 03.bin", 1185760800);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.gdi", 3);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "track 03.bin", 0, 2352);
        Assert.Equal(0, trackHandle!.TrackPregapSectors);
        Assert.Equal(45000, trackHandle.TrackFirstSector);
        Assert.Equal(16, trackHandle.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenGdiTrack3ExtraWhitespace()
    {
        const string gdiContents =
            "3\n\n" +
            "  1       0   4   2352   \"track 01.bin\"   0\n\n" +
            "  2     600   0   2352   \"track 02.raw\"   0\n\n" +
            "  3   45000   4   2352   \"track 03.bin\"   0\n\n";

        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.gdi", gdiContents);
        MockFilereader.MockEmptyFile(1, "track 03.bin", 1185760800);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.gdi", 3);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "track 03.bin", 0, 2352);
        Assert.Equal(0, trackHandle!.TrackPregapSectors);
        Assert.Equal(45000, trackHandle.TrackFirstSector);
        Assert.Equal(16, trackHandle.SectorHeaderSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenGdiTrackLast()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.gdi", GdiManyTracks);
        MockFilereader.MockEmptyFile(1, "track26.bin", 2457600);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.gdi", ConsoleIds.RC_HASH_CDTRACK_LAST);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "track26.bin", 0, 2352);
        Assert.Equal(0, trackHandle!.TrackPregapSectors);
        Assert.Equal(548106, trackHandle.TrackFirstSector);
        Assert.Equal(16, trackHandle.SectorHeaderSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenCueTrackLargestData()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", CueSingleBinMultipleData);
        MockFilereader.MockEmptyFile(1, "game.bin", 718310208);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", ConsoleIds.RC_HASH_CDTRACK_LARGEST);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 146190912, 2352); /* track 5: 0x8B6B240 */
        Assert.Equal(16, trackHandle!.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenCueTrackLargestDataMultipleBin()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", CueMultipleBinMultipleData);
        MockFilereader.MockEmptyFile(1, "track2.bin", 406423248);
        MockFilereader.MockEmptyFile(2, "track3.bin", 11553024);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", ConsoleIds.RC_HASH_CDTRACK_LARGEST);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "track2.bin", 0, 2352);
        Assert.Equal(225, trackHandle!.TrackPregapSectors); /* INDEX 01 00:03:00 */
        Assert.Equal(16, trackHandle.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenCueTrackLargestDataBackwardsCompatibility()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", CueSingleBinMultipleData);
        MockFilereader.MockEmptyFile(1, "game.bin", 718310208);

        /* before defining the enum, 0 meant largest */
        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 0);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 146190912, 2352); /* track 5: 0x8B6B240 */
        Assert.Equal(16, trackHandle!.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenCueTrackLargestDataLastTrack()
    {
        const string cue =
            "FILE \"game.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "  TRACK 02 MODE1/2352\n" +
            "    PREGAP 00:03:00\n" +
            "    INDEX 01 00:55:45\n" +
            "  TRACK 03 MODE1/2352\n" +
            "    INDEX 01 11:30:74\n" +
            "  TRACK 04 MODE1/2352\n" +
            "    INDEX 01 13:31:51\n" +
            "  TRACK 05 MODE1/2352\n" +
            "    INDEX 01 13:48:56\n";

        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", cue);
        MockFilereader.MockEmptyFile(1, "game.bin", 718310208);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", ConsoleIds.RC_HASH_CDTRACK_LARGEST);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 146190912, 2352); /* track 5: 0x8B6B240 (13:48:56) */
        Assert.Equal(16, trackHandle!.SectorHeaderSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenCueTrackLargestDataIndex0s()
    {
        const string cue =
            "FILE \"game.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "  TRACK 02 MODE1/2352\n" +
            "    INDEX 00 00:44:65\n" +
            "    INDEX 01 00:47:65\n" +
            "  TRACK 03 AUDIO\n" +
            "    INDEX 00 01:19:52\n" +
            "    INDEX 01 01:21:52\n";

        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", cue);
        MockFilereader.MockEmptyFile(1, "game.bin", 718310208);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", ConsoleIds.RC_HASH_CDTRACK_LARGEST);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 7914480, 2352); /* track 2: 0x78C3F0 (00:44:65) */
        Assert.Equal(225, trackHandle!.TrackPregapSectors);
        Assert.Equal(16, trackHandle.SectorHeaderSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenCueTrackLargestDataIndex2()
    {
        const string cue =
            "FILE \"game.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "  TRACK 02 MODE1/2352\n" +
            "    INDEX 00 00:00:00\n" +
            "    INDEX 01 00:02:00\n" +
            "    INDEX 02 00:08:64\n";

        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", cue);
        MockFilereader.MockEmptyFile(1, "game.bin", 718310208);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", ConsoleIds.RC_HASH_CDTRACK_LARGEST);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 0, 2352);
        Assert.Equal(150, trackHandle!.TrackPregapSectors); /* 00:02:00 = 150 frames in */
        Assert.Equal(16, trackHandle.SectorHeaderSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenCueTrackLargestDataMultipleBins()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", CueMultipleBinMultipleData);
        MockFilereader.MockEmptyFile(1, "track1.bin", 4132464);
        MockFilereader.MockEmptyFile(2, "track2.bin", 30080102);
        MockFilereader.MockEmptyFile(3, "track3.bin", 40343152);
        MockFilereader.MockEmptyFile(4, "track4.bin", 47277552);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 0);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "track3.bin", 0, 2352);
        Assert.Equal(150, trackHandle!.TrackPregapSectors); /* 00:02:00 = 150 frames in */
        Assert.Equal(16, trackHandle.SectorHeaderSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestOpenCueTrackLargestDataOnlyAudio()
    {
        const string cue =
            "FILE \"track1.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"track2.bin\" BINARY\n" +
            "  TRACK 02 AUDIO\n" +
            "    INDEX 00 00:00:00\n" +
            "    INDEX 01 00:03:00\n" +
            "FILE \"track3.bin\" BINARY\n" +
            "  TRACK 03 AUDIO\n" +
            "    INDEX 00 00:00:00\n" +
            "    INDEX 01 00:02:00\n" +
            "FILE \"track4.bin\" BINARY\n" +
            "  TRACK 04 AUDIO\n" +
            "    INDEX 00 00:00:00\n";

        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", cue);
        MockFilereader.MockEmptyFile(1, "track1.bin", 4132464);
        MockFilereader.MockEmptyFile(2, "track2.bin", 30080102);
        MockFilereader.MockEmptyFile(3, "track3.bin", 40343152);
        MockFilereader.MockEmptyFile(4, "track4.bin", 47277552);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 0);
        Assert.Null(trackHandle);
    }

    [Fact]
    public void TestOpenCueTrackFirstData()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.cue", CueSingleBinMultipleData);
        MockFilereader.MockEmptyFile(1, "game.bin", 718310208);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", ConsoleIds.RC_HASH_CDTRACK_FIRST_DATA);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 9807840, 2352); /* track 2: 0x0095a7e0 (00:55:45) */
        Assert.Equal(0, trackHandle!.TrackPregapSectors);
        Assert.Equal(16, trackHandle.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    private static void TestDetermineSectorSizeSync(int sectorSize)
    {
        var iterator = InitializeIterator();
        int imageSize = sectorSize * 32;
        byte[] image = new byte[imageSize];

        MockFilereader.MockFileText(0, "game.cue", CueSingleTrack);
        MockFilereader.MockFile(1, "game.bin", image, imageSize);

        Array.Clear(image, 0, imageSize);
        Array.Copy(SyncPattern, 0, image, sectorSize * 16, SyncPattern.Length);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 1);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 0, sectorSize);
        Assert.Equal(16, trackHandle!.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    private static void TestDetermineSectorSizeSyncPrimaryVolumeDescriptor(int sectorSize)
    {
        var iterator = InitializeIterator();
        int imageSize = sectorSize * 32;
        byte[] image = new byte[imageSize];

        MockFilereader.MockFileText(0, "game.cue", CueSingleTrack);
        MockFilereader.MockFile(1, "game.bin", image, imageSize);

        Array.Clear(image, 0, imageSize);
        Array.Copy(SyncPattern, 0, image, sectorSize * 16, SyncPattern.Length);
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("CD001"), 0, image, sectorSize * 16 + 25, 5);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 1);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 0, sectorSize);
        Assert.Equal(24, trackHandle!.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    private static void TestDetermineSectorSizeSyncPrimaryVolumeDescriptorIndex0(int sectorSize)
    {
        string cue =
            "FILE \"game.bin\" BINARY\n" +
            "  TRACK 01 MODE2/2352\n" +
            "    INDEX 00 00:00:00\n" +
            "    INDEX 01 00:02:00\n";

        var iterator = InitializeIterator();
        int imageSize = sectorSize * 200;
        byte[] image = new byte[imageSize];

        /* the C modifies the cue in place (the mock holds a pointer to it) */
        cue = cue.Replace("MODE2/2352", "MODE2/" + sectorSize);

        MockFilereader.MockFileText(0, "game.cue", cue);
        MockFilereader.MockFile(1, "game.bin", image, imageSize);

        Array.Clear(image, 0, imageSize);
        Array.Copy(SyncPattern, 0, image, sectorSize * (150 + 16), SyncPattern.Length);
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("CD001"), 0, image, sectorSize * (150 + 16) + 25, 5);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 1);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 0, sectorSize);
        Assert.Equal(150, trackHandle!.TrackPregapSectors);
        Assert.Equal(24, trackHandle.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestDetermineSectorSizeSync2352()
    {
        TestDetermineSectorSizeSync(2352);
    }

    [Fact]
    public void TestDetermineSectorSizeSyncPrimaryVolumeDescriptor2352()
    {
        TestDetermineSectorSizeSyncPrimaryVolumeDescriptor(2352);
    }

    [Fact]
    public void TestDetermineSectorSizeSyncPrimaryVolumeDescriptorIndex02352()
    {
        TestDetermineSectorSizeSyncPrimaryVolumeDescriptorIndex0(2352);
    }

    [Fact]
    public void TestDetermineSectorSizeSync2336()
    {
        TestDetermineSectorSizeSync(2336);
    }

    [Fact]
    public void TestDetermineSectorSizeSyncPrimaryVolumeDescriptor2336()
    {
        TestDetermineSectorSizeSyncPrimaryVolumeDescriptor(2336);
    }

    [Fact]
    public void TestDetermineSectorSizeSyncPrimaryVolumeDescriptorIndex02336()
    {
        TestDetermineSectorSizeSyncPrimaryVolumeDescriptorIndex0(2336);
    }

    [Fact]
    public void TestDetermineSectorSizeSync2048()
    {
        var iterator = InitializeIterator();
        const int sectorSize = 2048;
        int imageSize = sectorSize * 32;
        byte[] image = new byte[imageSize];

        MockFilereader.MockFileText(0, "game.cue", CueSingleTrack);
        MockFilereader.MockFile(1, "game.bin", image, imageSize);

        Array.Clear(image, 0, imageSize);

        /* 2048 byte sectors don't have a sync pattern - will use mode specified in header */
        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 1);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 0, 2352);
        Assert.Equal(24, trackHandle!.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestDetermineSectorSizeSyncPrimaryVolumeDescriptor2048()
    {
        var iterator = InitializeIterator();
        const int sectorSize = 2048;
        int imageSize = sectorSize * 32;
        byte[] image = new byte[imageSize];

        MockFilereader.MockFileText(0, "game.cue", CueSingleTrack);
        MockFilereader.MockFile(1, "game.bin", image, imageSize);

        Array.Clear(image, 0, imageSize);
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("CD001"), 0, image, sectorSize * 16 + 1, 5);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 1);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 0, sectorSize);
        Assert.Equal(0, trackHandle!.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestDetermineSectorSizeSyncPrimaryVolumeDescriptorIndex02048()
    {
        string cue =
            "FILE \"game.bin\" BINARY\n" +
            "  TRACK 01 MODE1/2048\n" +
            "    INDEX 00 00:00:00\n" +
            "    INDEX 01 00:02:00\n";

        var iterator = InitializeIterator();
        const int sectorSize = 2048;
        int imageSize = sectorSize * 200;
        byte[] image = new byte[imageSize];

        /* the C modifies the cue in place (the mock holds a pointer to it) */
        cue = cue.Replace("MODE1/2048", "MODE1/" + sectorSize);

        MockFilereader.MockFileText(0, "game.cue", cue);
        MockFilereader.MockFile(1, "game.bin", image, imageSize);

        Array.Clear(image, 0, imageSize);
        Array.Copy(System.Text.Encoding.ASCII.GetBytes("CD001"), 0, image, sectorSize * (150 + 16) + 1, 5);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 1);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 0, sectorSize);
        Assert.Equal(150, trackHandle!.TrackPregapSectors);
        Assert.Equal(0, trackHandle.SectorHeaderSize);
        Assert.Equal(2048, trackHandle.RawDataSize);

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestAbsoluteSectorToTrackSectorCuePregap()
    {
        const string cue =
            "FILE \"game1.bin\" BINARY\n" + /* file contains 500 sectors of data [1176000 bytes] */
            "  TRACK 01 MODE2/2352\n" +
            "    INDEX 00 00:00:00\n" +    /* 150 pre-gap sectors */
            "    INDEX 01 00:02:00\n" +    /* 350 sectors of data */
            "FILE \"game2.bin\" BINARY\n" +
            "  TRACK 02 MODE2/2352\n" +
            "    INDEX 00 00:00:00\n" +    /* 150 pre-gap sectors */
            "    INDEX 01 00:02:00\n";

        var iterator = InitializeIterator();
        int imageSize = 60 * 200;
        byte[] image = new byte[imageSize];

        MockFilereader.MockFileText(0, "game.cue", cue);
        MockFilereader.MockEmptyFile(1, "game1.bin", 500 * 2352);
        MockFilereader.MockFile(2, "game2.bin", image, imageSize);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 2);
        Assert.NotNull(trackHandle);
        Assert.NotNull(trackHandle!.FileHandle);
        Assert.Equal("game2.bin", MockFilereader.GetMockFilename(trackHandle.FileHandle));

        /* pregap of second track starts at sector 500 */
        Assert.Equal(500, trackHandle.TrackFirstSector);
        Assert.Equal(150, trackHandle.TrackPregapSectors);

        /* data for second track starts at sector 650 */
        Assert.Equal(650u, iterator.Callbacks.Cdreader.FirstTrackSector!(trackHandle));

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestAbsoluteSectorToTrackSectorGdi()
    {
        var iterator = InitializeIterator();

        MockFilereader.MockFileText(0, "game.gdi", GdiManyTracks);
        MockFilereader.MockEmptyFile(1, "track26.bin", 1234567);

        CdromTrack? trackHandle = OpenTrack(iterator, "game.gdi", 26);
        Assert.NotNull(trackHandle);
        Assert.NotNull(trackHandle!.FileHandle);
        Assert.Equal("track26.bin", MockFilereader.GetMockFilename(trackHandle.FileHandle));
        Assert.Equal(548106, trackHandle.TrackFirstSector);

        Assert.Equal(548106u, iterator.Callbacks.Cdreader.FirstTrackSector!(trackHandle));

        CloseTrack(iterator, trackHandle);
    }

    [Fact]
    public void TestReadSector()
    {
        byte[] buffer = new byte[4096];
        var iterator = InitializeIterator();
        int imageSize = 2352 * 32;
        byte[] image = new byte[imageSize];
        int offset, i;

        MockFilereader.MockFileText(0, "game.cue", CueSingleTrack);
        MockFilereader.MockFile(1, "game.bin", image, imageSize);

        Array.Clear(image, 0, imageSize);
        Array.Copy(SyncPattern, 0, image, 2352 * 16, SyncPattern.Length);
        image[2352 * 16 + 12] = 0;
        image[2352 * 16 + 13] = 2;
        image[2352 * 16 + 14] = 0x16;
        image[2352 * 16 + 15] = 2;

        offset = 2352 * 1 + 16;
        for (i = 0; i < 26; i++)
        {
            for (int j = 0; j < 256; ++j)
                image[offset + j] = (byte)(i + 'A');
            offset += 256;

            if ((i % 8) == 7)
                offset += (2352 - 2048);
        }

        CdromTrack? trackHandle = OpenTrack(iterator, "game.cue", 1);
        Assert.NotNull(trackHandle);
        AssertTrackCommon(trackHandle, "game.bin", 0, 2352);
        Assert.Equal(0, trackHandle!.TrackPregapSectors);
        Assert.Equal(16, trackHandle.SectorHeaderSize);

        /* read across multiple sectors */
        Assert.Equal(4096, iterator.Callbacks.Cdreader.ReadSector!(trackHandle, 1, buffer, buffer.Length));

        Assert.Equal((byte)'A', buffer[0]);
        Assert.Equal((byte)'A', buffer[255]);
        Assert.Equal((byte)'B', buffer[256]);
        Assert.Equal((byte)'H', buffer[2047]);
        Assert.Equal((byte)'I', buffer[2048]);
        Assert.Equal((byte)'P', buffer[4095]);

        /* read of partial sector */
        Assert.Equal(10, iterator.Callbacks.Cdreader.ReadSector!(trackHandle, 2, buffer, 10));
        Assert.Equal((byte)'I', buffer[0]);
        Assert.Equal((byte)'I', buffer[9]);
        Assert.Equal((byte)'A', buffer[10]);

        CloseTrack(iterator, trackHandle);
    }
}
