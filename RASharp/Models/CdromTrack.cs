// Ported from rcheevos (MIT) — src/rhash/cdreader.c
// struct rc_hash_cdrom_track_t — the track state for the default CD reader.

namespace RASharp.Models;

/* struct rc_hash_cdrom_track_t */
/// <summary>struct rc_hash_cdrom_track_t</summary>
public sealed class CdromTrack
{
    /// <summary>The filereader used to read the track's data file.</summary>
    public RcHashFilereader? FileReader;

    /// <summary>The open file handle of the track's data file.</summary>
    public object? FileHandle;

    /// <summary>Byte offset of the first sector of the track within its data file.</summary>
    public long FileTrackOffset;

    /// <summary>Number of pregap sectors that precede the track.</summary>
    public int TrackPregapSectors;

    /// <summary>Absolute first sector number of the track on the disc.</summary>
    public int TrackFirstSector;

    /// <summary>Size in bytes of one raw sector on the media.</summary>
    public int SectorSize;

    /// <summary>Size in bytes of the header that precedes each sector's data.</summary>
    public int SectorHeaderSize;

    /// <summary>Number of data bytes carried by each sector.</summary>
    public int RawDataSize = 2048;
}
