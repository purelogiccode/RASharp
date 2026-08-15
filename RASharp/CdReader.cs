// Ported from rcheevos (MIT) — src/rhash/cdreader.c
// Default CD reader: .cue/.gdi/.bin track opening, sector-size heuristics,
// and the track -> sector read logic (rc_hash_cdrom_track_t is in Models/).
// Control flow, constants, and special cases are translated 1:1; do not
// "improve" behavior — parity is the requirement.

using System.Text;
using RASharp.Models;

namespace RASharp;

/// <summary>Ported from rcheevos (MIT) — src/rhash/cdreader.c Default CD reader: .cue/.gdi/.bin track opening, sector-size heuristics, and the track -&gt; sector read logic (r</summary>
public static class CdReader
{
    /* cdreader_get_sector - convert the MSF value in the sync header to a
     * sector index, and subtract 150 (2 seconds) per:
     *   For data and mixed mode media (those conforming to ISO/IEC 10149), logical
     *   block address zero shall be assigned to the block at MSF address 00/02/00 */
    private static int GetSector(byte[] header)
    {
        var minutes = (header[12] >> 4) * 10 + (header[12] & 0x0F);
        var seconds = (header[13] >> 4) * 10 + (header[13] & 0x0F);
        var frames = (header[14] >> 4) * 10 + (header[14] & 0x0F);

        return ((minutes * 60) + seconds) * 75 + frames - 150;
    }

    private static readonly byte[] SyncPattern =
    [
        0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00
    ];

    /* cdreader_determine_sector_size - Attempt to determine the sector and header sizes.
     * The CUE file may be lying. Look for the sync pattern using each of the supported
     * sector sizes. Then check for the presence of "CD001", which is guaranteed to be in
     * either the boot record or primary volume descriptor, one of which is always at sector 16. */
    private static void DetermineSectorSize(CdromTrack cdrom)
    {
        var header = new byte[32];
        var tocSector = 16 + cdrom.TrackPregapSectors;
        RcHashFilereader fileReader = cdrom.FileReader!;

        cdrom.SectorSize = 0;
        cdrom.SectorHeaderSize = 0;
        cdrom.RawDataSize = 2048;

        fileReader.Seek!(cdrom.FileHandle!, tocSector * 2352 + cdrom.FileTrackOffset, HashEngine.SeekSet);
        if (fileReader.Read!(cdrom.FileHandle!, header, header.Length) < header.Length)
            return;

        if (Matches(header, 0, SyncPattern, 12))
        {
            cdrom.SectorSize = 2352;

            if (Matches(header, 25, "CD001", 5))
            {
                cdrom.SectorHeaderSize = 24;
            }
            else
            {
                cdrom.SectorHeaderSize = 16;
            }

            cdrom.TrackFirstSector = GetSector(header) - tocSector;
        }
        else
        {
            fileReader.Seek!(cdrom.FileHandle!, tocSector * 2336 + cdrom.FileTrackOffset, HashEngine.SeekSet);
            fileReader.Read!(cdrom.FileHandle!, header, header.Length);

            if (Matches(header, 0, SyncPattern, 12))
            {
                cdrom.SectorSize = 2336;

                if (Matches(header, 25, "CD001", 5))
                {
                    cdrom.SectorHeaderSize = 24;
                }
                else
                {
                    cdrom.SectorHeaderSize = 16;
                }

                cdrom.TrackFirstSector = GetSector(header) - tocSector;
            }
            else
            {
                fileReader.Seek!(cdrom.FileHandle!, tocSector * 2048 + cdrom.FileTrackOffset, HashEngine.SeekSet);
                fileReader.Read!(cdrom.FileHandle!, header, header.Length);

                if (Matches(header, 1, "CD001", 5))
                {
                    cdrom.SectorSize = 2048;
                    cdrom.SectorHeaderSize = 0;
                }
            }
        }
    }

    /* cdreader_open_bin_track - raw .bin file (no cue sheet) */
    private static CdromTrack? OpenBinTrack(string path, uint track, RcHashIterator iterator)
    {
        if (track > 1)
        {
            HashEngine.IteratorVerbose(iterator, "Cannot locate secondary tracks without a cue sheet");
            return null;
        }

        var fileHandle = iterator.Callbacks.Filereader.Open!(path);
        if (fileHandle == null)
            return null;

        var cdrom = new CdromTrack
        {
            FileReader = iterator.Callbacks.Filereader,
            FileHandle = fileHandle
        };

        DetermineSectorSize(cdrom);

        if (cdrom.SectorSize == 0)
        {
            iterator.Callbacks.Filereader.Seek!(fileHandle, 0, HashEngine.SeekEnd);
            var size = iterator.Callbacks.Filereader.Tell!(fileHandle);

            if ((size % 2352) == 0)
            {
                /* raw tracks use all 2352 bytes and have a 24 byte header */
                cdrom.SectorSize = 2352;
                cdrom.SectorHeaderSize = 24;
            }
            else if ((size % 2048) == 0)
            {
                /* cooked tracks eliminate all header/footer data */
                cdrom.SectorSize = 2048;
                cdrom.SectorHeaderSize = 0;
            }
            else if ((size % 2336) == 0)
            {
                /* MODE 2 format without 16-byte sync data */
                cdrom.SectorSize = 2336;
                cdrom.SectorHeaderSize = 8;
            }
            else
            {
                if (iterator.Callbacks.Filereader.Close != null)
                    iterator.Callbacks.Filereader.Close(fileHandle);

                HashEngine.IteratorVerbose(iterator, "Could not determine sector size");

                return null;
            }
        }

        return cdrom;
    }

    /* cdreader_open_bin - open the bin file for a track (from a cue sheet) */
    private static bool OpenBin(CdromTrack cdrom, string path, string mode)
    {
        cdrom.FileHandle = cdrom.FileReader!.Open!(path);
        if (cdrom.FileHandle == null)
            return false;

        /* determine sector size */
        DetermineSectorSize(cdrom);

        /* could not determine, which means we'll probably have more issues later
         * but use the CUE provided information anyway */
        if (cdrom.SectorSize == 0)
        {
            /* All of these modes have 2048 byte payloads. In MODE1/2352 and MODE2/2352
             * modes, the mode can actually be specified per sector to change the payload
             * size, but that reduces the ability to recover from errors when the disc
             * is damaged, so it's seldomly used, and when it is, it's mostly for audio
             * or video data where a blip or two probably won't be noticed by the user.
             * So, while we techincally support all of the following modes, we only do
             * so with 2048 byte payloads.
             * http://totalsonicmastering.com/cuesheetsyntax.htm
             * MODE1/2048 - CDROM Mode1 Data (cooked) [no header, no footer]
             * MODE1/2352 - CDROM Mode1 Data (raw)    [16 byte header, 288 byte footer]
             * MODE2/2336 - CDROM-XA Mode2 Data       [8 byte header, 280 byte footer]
             * MODE2/2352 - CDROM-XA Mode2 Data       [24 byte header, 280 byte footer]
             */
            if (StartsWith(mode, "MODE2/2352"))
            {
                cdrom.SectorSize = 2352;
                cdrom.SectorHeaderSize = 24;
            }
            else if (StartsWith(mode, "MODE1/2048"))
            {
                cdrom.SectorSize = 2048;
                cdrom.SectorHeaderSize = 0;
            }
            else if (StartsWith(mode, "MODE2/2336"))
            {
                cdrom.SectorSize = 2336;
                cdrom.SectorHeaderSize = 8;
            }
            else if (StartsWith(mode, "MODE1/2352"))
            {
                cdrom.SectorSize = 2352;
                cdrom.SectorHeaderSize = 16;
            }
            else if (StartsWith(mode, "AUDIO"))
            {
                cdrom.SectorSize = 2352;
                cdrom.SectorHeaderSize = 0;
                cdrom.RawDataSize = 2352; /* no header or footer data on audio tracks */
            }
        }

        return cdrom.SectorSize != 0;
    }

    /* cdreader_get_bin_path - cue_dir + bin_name */
    private static string GetBinPath(string cuePath, string binName)
    {
        var filename = HashEngine.PathGetFilename(cuePath);
        var cuePathLen = cuePath.Length - filename.Length;
        return cuePath.Substring(0, cuePathLen) + binName;
    }

    private static long GetBinSize(string cuePath, string binName, RcHashIterator iterator)
    {
        long size = 0;
        var binFilename = GetBinPath(cuePath, binName);
        var handle = iterator.Callbacks.Filereader.Open!(binFilename);
        if (handle != null)
        {
            iterator.Callbacks.Filereader.Seek!(handle, 0, HashEngine.SeekEnd);
            size = iterator.Callbacks.Filereader.Tell!(handle);

            if (iterator.Callbacks.Filereader.Close != null)
                iterator.Callbacks.Filereader.Close(handle);
        }

        return size;
    }

    /// <summary>is space.</summary>
    /// <param name="b">the b parameter</param>
    /// <returns>true on success; otherwise false</returns>
    internal static bool IsSpace(byte b)
    {
        return b == ' ' || b == '\t' || b == '\n' || b == '\v' || b == '\f' || b == '\r';
    }

    private static bool IsDigit(byte b)
    {
        return b >= '0' && b <= '9';
    }

    /* C atoi: skip leading whitespace, parse an optional sign, then digits */
    private static int Atoi(byte[] buffer, ref int pos)
    {
        var len = buffer.Length;
        while (pos < len && IsSpace(buffer[pos]))
        {
            ++pos;
        }

        var sign = 1;
        if (pos < len && (buffer[pos] == (byte)'-' || buffer[pos] == (byte)'+'))
        {
            if (buffer[pos] == (byte)'-')
            {
                sign = -1;
            }

            ++pos;
        }

        var result = 0;
        while (pos < len && IsDigit(buffer[pos]))
        {
            result = result * 10 + (buffer[pos] - '0');
            ++pos;
        }

        return result * sign;
    }

    /* strncasecmp(ptr, str, len) — case-insensitive ASCII compare of len bytes */
    /// <summary>strncasecmp(ptr, str, len) — case-insensitive ASCII compare of len bytes</summary>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="pos">the pos parameter</param>
    /// <param name="len">the len parameter</param>
    /// <param name="str">the str parameter</param>
    /// <returns>true on success; otherwise false</returns>
    internal static bool StartsWithIgnoreCase(byte[] buffer, int pos, int len, string str)
    {
        if (pos + str.Length > len)
            return false;

        for (var i = 0; i < str.Length; ++i)
        {
            var b = buffer[pos + i];
            if (b >= 'A' && b <= 'Z')
            {
                b = (byte)(b + ('a' - 'A'));
            }

            var c = str[i];
            if (c is >= 'A' and <= 'Z')
            {
                c = (char)(c + ('a' - 'A'));
            }

            if (b != (byte)c)
                return false;
        }

        return true;
    }

    /* memcmp(ptr, str, strlen(str)) */
    /// <summary>memcmp(ptr, str, strlen(str))</summary>
    /// <param name="text">the text parameter</param>
    /// <param name="prefix">the prefix parameter</param>
    /// <returns>true on success; otherwise false</returns>
    internal static bool StartsWith(string text, string prefix)
    {
        if (prefix.Length > text.Length)
            return false;

        for (var i = 0; i < prefix.Length; ++i)
        {
            if (text[i] != prefix[i])
                return false;
        }

        return true;
    }

    /* memcmp(ptr, str, strlen(str)) */
    /// <summary>memcmp(ptr, str, strlen(str))</summary>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="pos">the pos parameter</param>
    /// <param name="str">the str parameter</param>
    /// <returns>true on success; otherwise false</returns>
    internal static bool StartsWith(byte[] buffer, int pos, string str)
    {
        if (pos + str.Length > buffer.Length)
            return false;

        for (var i = 0; i < str.Length; ++i)
        {
            if (buffer[pos + i] != (byte)str[i])
                return false;
        }

        return true;
    }

    /// <summary>Compares a buffer region with a pattern byte-for-byte.</summary>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="offset">the byte offset</param>
    /// <param name="pattern">the wildcard pattern</param>
    /// <param name="length">the length parameter</param>
    /// <returns>true on success; otherwise false</returns>
    internal static bool Matches(byte[] buffer, int offset, byte[] pattern, int length)
    {
        for (var i = 0; i < length; ++i)
        {
            if (buffer[offset + i] != pattern[i])
                return false;
        }

        return true;
    }

    /// <summary>Compares a buffer region with a pattern byte-for-byte.</summary>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="offset">the byte offset</param>
    /// <param name="pattern">the wildcard pattern</param>
    /// <param name="length">the length parameter</param>
    /// <returns>true on success; otherwise false</returns>
    internal static bool Matches(byte[] buffer, int offset, string pattern, int length)
    {
        for (var i = 0; i < length; ++i)
        {
            if (buffer[offset + i] != (byte)pattern[i])
                return false;
        }

        return true;
    }

    /* strncasecmp(ptr, str, length) with the length given explicitly */
    /// <summary>strncasecmp(ptr, str, length) with the length given explicitly</summary>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="pos">the pos parameter</param>
    /// <param name="str">the str parameter</param>
    /// <param name="length">the length parameter</param>
    /// <returns>true on success; otherwise false</returns>
    internal static bool CompareIgnoreCase(byte[] buffer, int pos, string str, int length)
    {
        for (var i = 0; i < length; ++i)
        {
            var b = buffer[pos + i];
            if (b >= 'A' && b <= 'Z')
            {
                b = (byte)(b + ('a' - 'A'));
            }

            var c = str[i];
            if (c is >= 'A' and <= 'Z')
            {
                c = (char)(c + ('a' - 'A'));
            }

            if (b != (byte)c)
                return false;
        }

        return true;
    }

    /* decode a NUL-terminated ASCII string from the buffer (strcmp-style reads) */
    /// <summary>decode a NUL-terminated ASCII string from the buffer (strcmp-style reads)</summary>
    /// <param name="buffer">the buffer holding the data</param>
    /// <param name="offset">the byte offset</param>
    /// <param name="maxLength">the maximum number of bytes to read</param>
    /// <returns>the generated value</returns>
    internal static string GetNulTerminatedString(byte[] buffer, int offset, int maxLength = int.MaxValue)
    {
        var end = offset;
        var boundedMax = Math.Min(maxLength, buffer.Length);
        var limit = Math.Min(buffer.Length, offset + boundedMax);
        while (end < limit && buffer[end] != 0)
        {
            ++end;
        }

        return Encoding.ASCII.GetString(buffer, offset, end - offset);
    }

    /* struct track_t in cdreader_open_cue_track */
    private sealed class CueTrack
    {
        public uint Id;
        public int SectorSize;
        public int SectorCount;
        public int FirstSector;
        public int PregapSectors;
        public int IsData;
        public long FileTrackOffset;
        public long FileFirstSector;
        public string Mode = "";
        public string Filename = "";

        /// <summary>copy from.</summary>
        /// <param name="other">the other parameter</param>
        public void CopyFrom(CueTrack other)
        {
            Id = other.Id;
            SectorSize = other.SectorSize;
            SectorCount = other.SectorCount;
            FirstSector = other.FirstSector;
            PregapSectors = other.PregapSectors;
            IsData = other.IsData;
            FileTrackOffset = other.FileTrackOffset;
            FileFirstSector = other.FileFirstSector;
            Mode = other.Mode;
            Filename = other.Filename;
        }

        /// <summary>reset.</summary>
        public void Reset()
        {
            Id = 0;
            SectorSize = 0;
            SectorCount = 0;
            FirstSector = 0;
            PregapSectors = 0;
            IsData = 0;
            FileTrackOffset = 0;
            FileFirstSector = 0;
            Mode = "";
            Filename = "";
        }
    }

    private static CdromTrack? OpenCueTrack(string path, uint track, RcHashIterator iterator)
    {
        RcHashFilereader filereader = iterator.Callbacks.Filereader;
        CueTrack currentTrack = new();
        CueTrack previousTrack = new();
        CueTrack largestTrack = new();
        var session = 1;
        var done = false;
        CdromTrack? cdrom = null;

        var cueHandle = filereader.Open!(path);
        if (cueHandle == null)
            return null;

        /* read the entire cue file (the C reads it in 1023-byte chunks, re-seeked
         * to the start of each unprocessed line; scanning the whole file line by
         * line yields identical tokenization for every line) */
        var allBytes = new List<byte>();
        var chunk = new byte[1024];
        int numRead;
        do
        {
            numRead = filereader.Read!(cueHandle, chunk, chunk.Length - 1);
            if (numRead == 0)
                break;

            for (var i = 0; i < numRead; ++i)
                allBytes.Add(chunk[i]);
        } while (numRead == chunk.Length - 1);

        if (filereader.Close != null)
            filereader.Close(cueHandle);

        byte[] buffer = [.. allBytes];
        var len = buffer.Length;
        var pos = 0;

        while (pos < len)
        {
            while (pos < len && buffer[pos] == (byte)' ')
            {
                ++pos;
            }

            if (pos >= len)
                break;

            if (StartsWithIgnoreCase(buffer, pos, len, "INDEX "))
            {
                pos += 6;
                var index = Atoi(buffer, ref pos);

                while (pos < len && buffer[pos] != (byte)' ' && buffer[pos] != (byte)'\n')
                {
                    ++pos;
                }

                while (pos < len && buffer[pos] == (byte)' ')
                {
                    ++pos;
                }

                /* convert mm:ss:ff to sector count */
                ParseMsf(buffer, ref pos, len, out var m, out var s, out var f);
                var sectorOffset = ((m * 60) + s) * 75 + f;

                if (currentTrack.FirstSector == -1)
                {
                    currentTrack.FirstSector = sectorOffset;
                    if (string.Equals(currentTrack.Filename, previousTrack.Filename, StringComparison.Ordinal))
                    {
                        previousTrack.SectorCount = currentTrack.FirstSector - previousTrack.FirstSector;
                        currentTrack.FileTrackOffset += previousTrack.SectorCount * previousTrack.SectorSize;
                    }

                    /* if looking for the largest data track, determine previous track size */
                    if (track == ConsoleIds.RcHashCdtrackLargest && previousTrack.SectorCount > largestTrack.SectorCount &&
                        previousTrack.IsData != 0)
                    {
                        largestTrack.CopyFrom(previousTrack);
                    }
                }

                if (index == 1)
                {
                    currentTrack.PregapSectors = sectorOffset - currentTrack.FirstSector;

                    /* it's undesirable to truncate offset to 32-bits, but %lld isn't defined in c89. */
                    HashEngine.IteratorVerboseFormatted(iterator, "Found {0} track {1} (first sector {2}, sector size {3}, {4} pregap sectors)",
                        currentTrack.Mode, currentTrack.Id, currentTrack.FirstSector, currentTrack.SectorSize, currentTrack.PregapSectors);

                    if (currentTrack.Id == track)
                    {
                        done = true;
                        break;
                    }

                    if ((track == ConsoleIds.RcHashCdtrackFirstData && currentTrack.IsData != 0) || (track == ConsoleIds.RcHashCdtrackFirstOfSecondSession && session == 2))
                    {
                        track = currentTrack.Id;
                        done = true;
                        break;
                    }
                }
            }
            else if (StartsWithIgnoreCase(buffer, pos, len, "TRACK "))
            {
                if (currentTrack.SectorSize != 0)
                    previousTrack.CopyFrom(currentTrack);

                pos += 6;
                currentTrack.Id = (uint)Atoi(buffer, ref pos);

                currentTrack.PregapSectors = -1;
                currentTrack.FirstSector = -1;

                while (pos < len && buffer[pos] != (byte)' ')
                {
                    ++pos;
                }

                while (pos < len && buffer[pos] == (byte)' ')
                {
                    ++pos;
                }

                /* mode is truncated at the first whitespace (the C truncates it when
                 * reporting the track, and only ever compares the leading token) */
                var modeStart = pos;
                while (pos < len && !IsSpace(buffer[pos]))
                {
                    ++pos;
                }

                var mode = Encoding.ASCII.GetString(buffer, modeStart, pos - modeStart);
                currentTrack.Mode = mode;
                currentTrack.IsData = StartsWith(mode, "MODE") ? 1 : 0;

                if (currentTrack.IsData != 0)
                {
                    var sizePos = modeStart + 6;
                    currentTrack.SectorSize = Atoi(buffer, ref sizePos);
                }
                else
                {
                    /* assume AUDIO */
                    currentTrack.SectorSize = 2352;
                }
            }
            else if (StartsWithIgnoreCase(buffer, pos, len, "FILE "))
            {
                if (currentTrack.SectorSize != 0)
                {
                    previousTrack.CopyFrom(currentTrack);

                    if (previousTrack.SectorCount == 0)
                    {
                        var binSize = GetBinSize(path, previousTrack.Filename, iterator);
                        var fileSectorCount = (uint)(binSize / previousTrack.SectorSize);
                        previousTrack.SectorCount = (int)fileSectorCount - previousTrack.FirstSector;
                    }

                    /* if looking for the largest data track, check to see if this one is larger */
                    if (track == ConsoleIds.RcHashCdtrackLargest && previousTrack.IsData != 0 &&
                        previousTrack.SectorCount > largestTrack.SectorCount)
                    {
                        largestTrack.CopyFrom(previousTrack);
                    }
                }

                currentTrack.Reset();

                currentTrack.FileFirstSector = previousTrack.FileFirstSector +
                                               previousTrack.FirstSector + previousTrack.SectorCount;

                pos += 5;
                if (pos < len && buffer[pos] == (byte)'"')
                {
                    ++pos;
                    var fileStart = pos;
                    while (pos < len && buffer[pos] != (byte)'\n' && buffer[pos] != (byte)'"')
                    {
                        ++pos;
                    }

                    currentTrack.Filename = Encoding.ASCII.GetString(buffer, fileStart, pos - fileStart);
                }
                else
                {
                    var fileStart = pos;
                    while (pos < len && buffer[pos] != (byte)'\n' && buffer[pos] != (byte)' ')
                    {
                        ++pos;
                    }

                    currentTrack.Filename = Encoding.ASCII.GetString(buffer, fileStart, pos - fileStart);
                }
            }
            else if (StartsWithIgnoreCase(buffer, pos, len, "REM "))
            {
                pos += 4;
                while (pos < len && buffer[pos] == (byte)' ')
                {
                    ++pos;
                }

                if (StartsWithIgnoreCase(buffer, pos, len, "SESSION "))
                {
                    pos += 8;
                    while (pos < len && buffer[pos] == (byte)' ')
                    {
                        ++pos;
                    }

                    session = Atoi(buffer, ref pos);
                }
            }

            while (pos < len && buffer[pos] != (byte)'\n')
            {
                ++pos;
            }

            if (pos < len)
            {
                ++pos;
            }
        }

        switch (track)
        {
            case ConsoleIds.RcHashCdtrackLargest:
            {
                if (currentTrack.SectorSize != 0 && currentTrack.IsData != 0)
                {
                    var binSize = GetBinSize(path, currentTrack.Filename, iterator);
                    var fileSectorCount = (uint)(binSize / currentTrack.SectorSize);
                    currentTrack.SectorCount = (int)fileSectorCount - currentTrack.FirstSector;

                    if (largestTrack.SectorCount > currentTrack.SectorCount)
                        currentTrack.CopyFrom(largestTrack);
                }
                else
                {
                    currentTrack.CopyFrom(largestTrack);
                }

                track = currentTrack.Id;
                break;
            }
            case ConsoleIds.RcHashCdtrackLast when !done:
                track = currentTrack.Id;
                break;
        }

        if (currentTrack.Id == track)
        {
            cdrom = new CdromTrack
            {
                FileReader = filereader,
                FileTrackOffset = currentTrack.FileTrackOffset,
                TrackPregapSectors = currentTrack.PregapSectors,
                TrackFirstSector = (int)(currentTrack.FileFirstSector + currentTrack.FirstSector)
            };

            /* verify existance of bin file */
            var binFilename = GetBinPath(path, currentTrack.Filename);
            if (OpenBin(cdrom, binFilename, currentTrack.Mode))
            {
                if (cdrom.TrackPregapSectors != 0)
                    HashEngine.IteratorVerboseFormatted(iterator, "Opened track {0} (sector size {1}, {2} pregap sectors)",
                        track, cdrom.SectorSize, cdrom.TrackPregapSectors);
                else
                    HashEngine.IteratorVerboseFormatted(iterator, "Opened track {0} (sector size {1})", track, cdrom.SectorSize);
            }
            else
            {
                if (cdrom.FileHandle != null)
                {
                    filereader.Close!(cdrom.FileHandle!);
                    HashEngine.IteratorErrorFormatted(iterator, "Could not determine sector size for {0} track", currentTrack.Mode);
                }
                else
                {
                    HashEngine.IteratorErrorFormatted(iterator, "Could not open {0}", binFilename);
                }

                cdrom = null;
            }
        }

        return cdrom;
    }

    private static CdromTrack? OpenGdiTrack(string path, uint track, RcHashIterator iterator)
    {
        RcHashFilereader filereader = iterator.Callbacks.Filereader;
        var sectorSize = "";
        var file = "";
        uint currentTrack = 0;
        var lba = 0;

        uint largestTrack = 0;
        long largestTrackSize = 0;
        var largestTrackFile = "";
        var largestTrackSectorSize = "";
        var largestTrackLba = 0;

        var fileHandle = filereader.Open!(path);
        if (fileHandle == null)
            return null;

        /* read the entire gdi file (same chunk/line reasoning as the cue parser) */
        var allBytes = new List<byte>();
        var chunk = new byte[1024];
        int numRead;
        do
        {
            numRead = filereader.Read!(fileHandle, chunk, chunk.Length - 1);
            if (numRead == 0)
                break;

            for (var i = 0; i < numRead; ++i)
                allBytes.Add(chunk[i]);
        } while (numRead == chunk.Length - 1);

        if (filereader.Close != null)
            filereader.Close(fileHandle);

        byte[] buffer = [.. allBytes];
        var len = buffer.Length;
        var pos = 0;

        /* the first line contains the number of tracks, so we can get the last track index from it */
        if (track == ConsoleIds.RcHashCdtrackLast)
        {
            track = (uint)Atoi(buffer, ref pos);
        }

        /* first line contains the number of tracks and will be skipped */
        while (pos < len)
        {
            /* skip until next newline */
            while (pos < len && buffer[pos] != (byte)'\n')
            {
                ++pos;
            }

            /* skip newlines */
            while (pos < len && (buffer[pos] == (byte)'\n' || buffer[pos] == (byte)'\r'))
            {
                ++pos;
            }

            /* line format: [trackid] [lba] [type] [sectorsize] [file] [?] */
            while (pos < len && IsSpace(buffer[pos]))
            {
                ++pos;
            }

            currentTrack = (uint)Atoi(buffer, ref pos);
            if (track != 0 && currentTrack != track && track != ConsoleIds.RcHashCdtrackFirstData)
                continue;

            while (pos < len && IsDigit(buffer[pos]))
            {
                ++pos;
            }

            ++pos;

            while (pos < len && IsSpace(buffer[pos]))
            {
                ++pos;
            }

            lba = Atoi(buffer, ref pos);
            while (pos < len && IsDigit(buffer[pos]))
            {
                ++pos;
            }

            ++pos;

            while (pos < len && IsSpace(buffer[pos]))
            {
                ++pos;
            }

            var trackType = Atoi(buffer, ref pos);
            while (pos < len && IsDigit(buffer[pos]))
            {
                ++pos;
            }

            ++pos;

            while (pos < len && IsSpace(buffer[pos]))
            {
                ++pos;
            }

            var sizeStart = pos;
            while (pos < len && IsDigit(buffer[pos]))
            {
                ++pos;
            }

            sectorSize = Encoding.ASCII.GetString(buffer, sizeStart, pos - sizeStart);
            ++pos;

            while (pos < len && IsSpace(buffer[pos]))
            {
                ++pos;
            }

            if (pos < len && buffer[pos] == (byte)'"')
            {
                ++pos;
                var fileStart = pos;
                while (pos < len && buffer[pos] != (byte)'"')
                {
                    ++pos;
                }

                if (pos >= len)
                {
                    HashEngine.IteratorError(iterator, "Quoted string without closing quote");
                    return null;
                }

                file = Encoding.ASCII.GetString(buffer, fileStart, pos - fileStart);
                ++pos;
            }
            else
            {
                var fileStart = pos;
                while (pos < len && buffer[pos] != (byte)' ' && buffer[pos] != (byte)'\n')
                {
                    ++pos;
                }

                file = Encoding.ASCII.GetString(buffer, fileStart, pos - fileStart);
            }

            if (file.Length >= 256)
            {
                HashEngine.IteratorErrorFormatted(iterator, "Cannot copy {0} byte filename into {1} byte buffer", file.Length, 256);
                return null;
            }

            if (track == currentTrack)
            {
                break;
            }
            else if (track == ConsoleIds.RcHashCdtrackFirstData && trackType == 4)
            {
                break;
            }
            else if (track == ConsoleIds.RcHashCdtrackLargest && trackType == 4)
            {
                var trackSize = GetBinSize(path, file, iterator);
                if (trackSize > largestTrackSize)
                {
                    largestTrackSize = trackSize;
                    largestTrack = currentTrack;
                    largestTrackLba = lba;
                    largestTrackFile = file;
                    largestTrackSectorSize = sectorSize;
                }
            }
        }

        var cdrom = new CdromTrack
        {
            FileReader = filereader
        };

        /* if we were tracking the largest track, make it the current track.
         * otherwise, currentTrack will be the requested track, or last track. */
        if (largestTrack != 0 && largestTrack != currentTrack)
        {
            currentTrack = largestTrack;
            file = largestTrackFile;
            sectorSize = largestTrackSectorSize;
            lba = largestTrackLba;
        }

        /* open the bin file for the track - construct mode parameter from sector_size */
        var mode = "MODE1/" + sectorSize.TrimEnd('"');

        var binPath = GetBinPath(path, file);
        if (OpenBin(cdrom, binPath, mode))
        {
            cdrom.TrackPregapSectors = 0;
            cdrom.TrackFirstSector = lba;

            HashEngine.IteratorVerboseFormatted(iterator, "Opened track {0} (sector size {1})", currentTrack, cdrom.SectorSize);
        }
        else
        {
            HashEngine.IteratorErrorFormatted(iterator, "Could not open {0}", binPath);

            cdrom = null;
        }

        return cdrom;
    }

    private static void ParseMsf(byte[] buffer, ref int pos, int len, out int m, out int s, out int f)
    {
        /* sscanf_s(ptr, "%d:%d:%d", &m, &s, &f) — fields that fail to parse stay 0 */
        m = 0;
        s = 0;
        f = 0;

        m = Atoi(buffer, ref pos);
        if (pos < len && buffer[pos] == (byte)':')
        {
            ++pos;
            s = Atoi(buffer, ref pos);
            if (pos < len && buffer[pos] == (byte)':')
            {
                ++pos;
                f = Atoi(buffer, ref pos);
            }
        }
    }

    /* cdreader_open_track_iterator */
    private static object? OpenTrackIterator(string path, uint track, RcHashIterator iterator)
    {
        /* backwards compatibility - 0 used to mean largest */
        if (track == 0)
        {
            track = ConsoleIds.RcHashCdtrackLargest;
        }

        if (HashEngine.PathCompareExtension(path, "cue") != 0)
            return OpenCueTrack(path, track, iterator);
        if (HashEngine.PathCompareExtension(path, "gdi") != 0)
            return OpenGdiTrack(path, track, iterator);

        return OpenBinTrack(path, track, iterator);
    }

    /* cdreader_read_sector */
    private static int ReadSector(object? trackHandle, uint sector, byte[] buffer, int requestedBytes)
    {
        int numRead;
        var totalRead = 0;
        var bufferPtr = 0;

        var cdrom = (CdromTrack?)trackHandle;
        if (cdrom == null)
            return 0;

        if (sector < (uint)cdrom.TrackFirstSector)
            return 0;

        var sectorStart = (sector - cdrom.TrackFirstSector) * cdrom.SectorSize +
                          cdrom.SectorHeaderSize + cdrom.FileTrackOffset;

        /* the filereader interface writes at the start of the destination buffer,
         * so read into a scratch buffer and copy to the requested offset (the C
         * writes directly at buffer_ptr) */
        var temp = new byte[cdrom.RawDataSize];

        while (requestedBytes > cdrom.RawDataSize)
        {
            cdrom.FileReader!.Seek!(cdrom.FileHandle!, sectorStart, HashEngine.SeekSet);
            numRead = cdrom.FileReader.Read!(cdrom.FileHandle!, temp, cdrom.RawDataSize);
            Array.Copy(temp, 0, buffer, bufferPtr, numRead);
            totalRead += numRead;

            if (numRead < cdrom.RawDataSize)
                return totalRead;

            bufferPtr += cdrom.RawDataSize;
            sectorStart += cdrom.SectorSize;
            requestedBytes -= cdrom.RawDataSize;
        }

        cdrom.FileReader!.Seek!(cdrom.FileHandle!, sectorStart, HashEngine.SeekSet);
        numRead = cdrom.FileReader.Read!(cdrom.FileHandle!, temp, requestedBytes);
        Array.Copy(temp, 0, buffer, bufferPtr, numRead);
        totalRead += numRead;

        return totalRead;
    }

    /* cdreader_close_track */
    private static void CloseTrack(object? trackHandle)
    {
        var cdrom = (CdromTrack?)trackHandle;
        if (cdrom != null)
        {
            if (cdrom.FileHandle != null && cdrom.FileReader!.Close != null)
                cdrom.FileReader.Close(cdrom.FileHandle!);
        }
    }

    /* cdreader_first_track_sector */
    private static uint FirstTrackSector(object? trackHandle)
    {
        var cdrom = (CdromTrack?)trackHandle;
        if (cdrom != null)
            return (uint)(cdrom.TrackFirstSector + cdrom.TrackPregapSectors);

        return 0;
    }

    /* rc_hash_get_default_cdreader */
    /// <summary>rc_hash_get_default_cdreader</summary>
    /// <param name="cdreader">the cdreader parameter</param>
    public static void GetDefaultCdreader(RcHashCdreader cdreader)
    {
        cdreader.OpenTrack = null;
        cdreader.ReadSector = ReadSector;
        cdreader.CloseTrack = CloseTrack;
        cdreader.FirstTrackSector = FirstTrackSector;
        cdreader.OpenTrackIterator = OpenTrackIterator;
    }
}
