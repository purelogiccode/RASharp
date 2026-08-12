// Ported from rcheevos (MIT) — src/rhash/cdreader.c
// struct rc_hash_cdrom_track_t — the track state for the default CD reader.

namespace RASharp.Core.Models;

/* struct rc_hash_cdrom_track_t */
public sealed class CdromTrack
{
    public RcHashFilereader? FileReader;
    public object? FileHandle;
    public long FileTrackOffset;
    public int TrackPregapSectors;
    public int TrackFirstSector;
    public int SectorSize;
    public int SectorHeaderSize;
    public int RawDataSize = 2048;
}
