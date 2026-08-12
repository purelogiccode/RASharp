// Ported from rcheevos (MIT) — src/rhash/hash_disc.c
// Disc-based hashing: PSX, Saturn, Sega CD, PCE-CD, PC-FX, 3DO, Jaguar CD,
// Neo Geo CD, Dreamcast, GameCube, Wii/WiiWare, plus the internal ISO9660
// mini-parser (rc_cd_find_file_sector) and the cd-file streaming helper
// (rc_hash_cd_file). Control flow, constants, and special cases are
// translated 1:1; do not "improve" behavior — parity is the requirement.
// PS2/PSP entry points land in Phase 4.

using System.Text;

namespace RASharp.Core;


using RASharp.Core.Models;
/// <summary>Ported from rcheevos (MIT) — src/rhash/hash_disc.c Disc-based hashing: PSX, Saturn, Sega CD, PCE-CD, PC-FX, 3DO, Jaguar CD, Neo Geo CD, Dreamcast, GameCube, Wii</summary>
public static class HashDisc
{
    /* ===================================================== */
    /* cdreader hook helpers (rc_cd_* wrappers)              */

    private static object? CdOpenTrack(RcHashIterator iterator, uint track)
    {
        if (iterator.Callbacks.Cdreader.OpenTrackIterator != null)
            return iterator.Callbacks.Cdreader.OpenTrackIterator(iterator.Path!, track, iterator);

        if (iterator.Callbacks.Cdreader.OpenTrack != null)
            return iterator.Callbacks.Cdreader.OpenTrack(iterator.Path!, track);

        RcHashCdreader? globalCdreader = HashEngine.GetGlobalCdreader();
        if (globalCdreader != null && globalCdreader.OpenTrack != null)
            return globalCdreader.OpenTrack(iterator.Path!, track);

        HashEngine.IteratorError(iterator, "no hook registered for cdreader_open_track");
        return null;
    }

    private static int CdReadSector(RcHashIterator iterator, object? trackHandle, uint sector, byte[] buffer, int requestedBytes)
    {
        if (iterator.Callbacks.Cdreader.ReadSector != null)
            return iterator.Callbacks.Cdreader.ReadSector(trackHandle!, sector, buffer, requestedBytes);

        RcHashCdreader? globalCdreader = HashEngine.GetGlobalCdreader();
        if (globalCdreader != null && globalCdreader.ReadSector != null)
            return globalCdreader.ReadSector(trackHandle!, sector, buffer, requestedBytes);

        HashEngine.IteratorError(iterator, "no hook registered for cdreader_read_sector");
        return 0;
    }

    private static uint CdFirstTrackSector(RcHashIterator iterator, object? trackHandle)
    {
        if (iterator.Callbacks.Cdreader.FirstTrackSector != null)
            return iterator.Callbacks.Cdreader.FirstTrackSector(trackHandle!);

        RcHashCdreader? globalCdreader = HashEngine.GetGlobalCdreader();
        if (globalCdreader != null && globalCdreader.FirstTrackSector != null)
            return globalCdreader.FirstTrackSector(trackHandle!);

        HashEngine.IteratorError(iterator, "no hook registered for cdreader_first_track_sector");
        return 0;
    }

    private static void CdCloseTrack(RcHashIterator iterator, object? trackHandle)
    {
        if (iterator.Callbacks.Cdreader.CloseTrack != null)
            iterator.Callbacks.Cdreader.CloseTrack(trackHandle!);
        else
        {
            RcHashCdreader? globalCdreader = HashEngine.GetGlobalCdreader();
            if (globalCdreader != null && globalCdreader.CloseTrack != null)
                globalCdreader.CloseTrack(trackHandle!);
            else
                HashEngine.IteratorError(iterator, "no hook registered for cdreader_close_track");
        }
    }

    /* rc_cd_find_file_sector - ISO9660 mini-parser: resolve path (backslash
     * separated, starting at the root) to a sector; optionally return the size */
/// <summary>rc_cd_find_file_sector - ISO9660 mini-parser: resolve path (backslash separated, starting at the root) to a sector; optionally return the size</summary>
/// <param name="iterator">the hash iterator</param>
/// <param name="trackHandle">the open track handle</param>
/// <param name="path">the file path</param>
/// <param name="size">the size</param>
/// <returns>the result</returns>
    internal static int CdFindFileSector(RcHashIterator iterator, object? trackHandle, string path, out uint size)
    {
        byte[] buffer = new byte[2048];
        int sector;
        uint numSectors = 0;
        int filenameLength;
        int slash;
        size = 0;

        if (trackHandle == null)
            return 0;

        /* we start at the root. don't need to explicitly find it */
        if (path.Length > 0 && path[0] == '\\')
            path = path.Substring(1);

        filenameLength = path.Length;
        slash = path.LastIndexOf('\\');
        if (slash >= 0)
        {
            /* find the directory record for the first part of the path */
            sector = CdFindFileSector(iterator, trackHandle, path.Substring(0, slash), out _);
            if (sector == 0)
                return 0;

            ++slash;
            filenameLength -= slash;
            path = path.Substring(slash);
        }
        else
        {
            uint logicalBlockSize;

            /* find the cd information */
            if (CdReadSector(iterator, trackHandle, CdFirstTrackSector(iterator, trackHandle) + 16, buffer, 256) == 0)
                return 0;

            /* the directory_record starts at 156, the sector containing the table of contents is 2 bytes into that.
             * https://www.cdroller.com/htm/readdata.html */
            sector = buffer[156 + 2] | (buffer[156 + 3] << 8) | (buffer[156 + 4] << 16);

            /* if the table of contents spans more than one sector, it's length of section will exceed it's logical block size */
            logicalBlockSize = (uint)(buffer[128] | (buffer[128 + 1] << 8)); /* logical block size */
            if (logicalBlockSize == 0)
            {
                numSectors = 1;
            }
            else
            {
                numSectors = (uint)(buffer[156 + 10] | (buffer[156 + 11] << 8) | (buffer[156 + 12] << 16) | (buffer[156 + 13] << 24)) / logicalBlockSize;
            }
        }

        /* fetch and process the directory record */
        if (CdReadSector(iterator, trackHandle, (uint)sector, buffer, buffer.Length) == 0)
            return 0;

        int tmp = 0;
        do
        {
            if (tmp >= buffer.Length || buffer[tmp] == 0)
            {
                /* end of this path table block. if the path table spans multiple sectors, keep scanning */
                if (numSectors > 1)
                {
                    --numSectors;
                    if (CdReadSector(iterator, trackHandle, (uint)(++sector), buffer, buffer.Length) != 0)
                    {
                        tmp = 0;
                        continue;
                    }
                }
                break;
            }

            /* filename is 33 bytes into the record and the format is "FILENAME;version" or "DIRECTORY" */
            if ((buffer[tmp + 32] == filenameLength ||
                 (tmp + 33 + filenameLength) < buffer.Length && buffer[tmp + 33 + filenameLength] == (byte)';') &&
                CdReader.CompareIgnoreCase(buffer, tmp + 33, path, filenameLength))
            {
                sector = buffer[tmp + 2] | (buffer[tmp + 3] << 8) | (buffer[tmp + 4] << 16);

                HashEngine.IteratorVerboseFormatted(iterator, "Found {0} at sector {1}", path, sector);

                size = (uint)(buffer[tmp + 10] | (buffer[tmp + 11] << 8) | (buffer[tmp + 12] << 16) | (buffer[tmp + 13] << 24));

                return sector;
            }

            /* the first byte of the record is the length of the record */
            tmp += buffer[tmp];
        } while (true);

        return 0;
    }

    /* rc_hash_cd_file - stream a file's sectors into the running md5 */
    private static int CdFile(HashMd5 md5, RcHashIterator iterator, object? trackHandle, uint sector, string? name, uint size, string description)
    {
        byte[] buffer = new byte[2048];
        int numRead;

        numRead = CdReadSector(iterator, trackHandle, sector, buffer, buffer.Length);
        if (numRead < buffer.Length)
            return HashEngine.IteratorErrorFormatted(iterator, "Could not read {0}", description);

        if (size > HashEngine.MAX_BUFFER_SIZE)
            size = (uint)HashEngine.MAX_BUFFER_SIZE;

        if (name != null)
            HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} title ({1} bytes) and contents ({2} bytes) ", name, name.Length, size);
        else
            HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} contents ({1} bytes @ sector {2})", description, size, sector);

        if (size < (uint)numRead) /* we read a whole sector - only hash the part containing file data */
            numRead = (int)size;

        do
        {
            md5.Append(buffer, numRead);

            if (size <= (uint)numRead)
                break;
            size -= (uint)numRead;

            ++sector;
            if (size >= buffer.Length)
                numRead = CdReadSector(iterator, trackHandle, sector, buffer, buffer.Length);
            else
                numRead = CdReadSector(iterator, trackHandle, sector, buffer, (int)size);
        } while (numRead > 0);

        return 1;
    }

    /* ===================================================== */

/// <summary>=====================================================</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHash3Do(out string hash, RcHashIterator iterator)
    {
        hash = "";
        byte[] buffer = new byte[2048];
        byte[] operafsIdentifier = { 0x01, 0x5A, 0x5A, 0x5A, 0x5A, 0x5A, 0x01 };
        object? trackHandle;
        HashMd5 md5;
        int sector;
        int blockSize, blockLocation;
        int offset, stop;
        long size = 0;

        trackHandle = CdOpenTrack(iterator, 1);
        if (trackHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open track");

        /* the Opera filesystem stores the volume information in the first 132 bytes of sector 0
         * https://github.com/barbeque/3dodump/blob/master/OperaFS-Format.md */
        if (CdReadSector(iterator, trackHandle, 0, buffer, 132) >= 132 &&
            CdReader.Matches(buffer, 0, operafsIdentifier, operafsIdentifier.Length))
        {
            HashEngine.IteratorVerboseFormatted(iterator, "Found 3DO CD, title={0}", CdReader.GetNulTerminatedString(buffer, 0x28, 32));

            /* include the volume header in the hash */
            md5 = new HashMd5();
            md5.Append(buffer, 132);

            /* the block size is at offset 0x4C (assume 0x4C is always 0) */
            blockSize = buffer[0x4D] * 65536 + buffer[0x4E] * 256 + buffer[0x4F];

            /* the root directory block location is at offset 0x64 (and duplicated several
             * times, but we just look at the primary record) (assume 0x64 is always 0) */
            blockLocation = buffer[0x65] * 65536 + buffer[0x66] * 256 + buffer[0x67];

            /* multiply the block index by the block size to get the real address */
            blockLocation *= blockSize;

            /* convert that to a sector and read it */
            sector = blockLocation / 2048;

            do
            {
                CdReadSector(iterator, trackHandle, (uint)sector, buffer, buffer.Length);

                /* offset to start of entries is at offset 0x10 (assume 0x10 and 0x11 are always 0) */
                offset = buffer[0x12] * 256 + buffer[0x13];

                /* offset to end of entries is at offset 0x0C (assume 0x0C is always 0) */
                stop = buffer[0x0D] * 65536 + buffer[0x0E] * 256 + buffer[0x0F];

                while (offset < stop)
                {
                    if (buffer[offset + 0x03] == 0x02) /* file */
                    {
                        if (CdReader.GetNulTerminatedString(buffer, offset + 0x20).Equals("LaunchMe", StringComparison.OrdinalIgnoreCase))
                        {
                            /* the block size is at offset 0x0C (assume 0x0C is always 0) */
                            blockSize = buffer[offset + 0x0D] * 65536 + buffer[offset + 0x0E] * 256 + buffer[offset + 0x0F];

                            /* the block location is at offset 0x44 (assume 0x44 is always 0) */
                            blockLocation = buffer[offset + 0x45] * 65536 + buffer[offset + 0x46] * 256 + buffer[offset + 0x47];
                            blockLocation *= blockSize;

                            /* the file size is at offset 0x10 (assume 0x10 is always 0) */
                            size = (long)buffer[offset + 0x11] * 65536 + buffer[offset + 0x12] * 256 + buffer[offset + 0x13];

                            HashEngine.IteratorVerboseFormatted(iterator, "Hashing header ({0} bytes) and {1} ({2} bytes) ", 132,
                                CdReader.GetNulTerminatedString(buffer, offset + 0x20, 32), size);

                            break;
                        }
                    }

                    /* the number of extra copies of the file is at offset 0x40 (assume 0x40-0x42 are always 0) */
                    offset += 0x48 + buffer[offset + 0x43] * 4;
                }

                if (size != 0)
                    break;

                /* did not find the file, see if the directory listing is continued in another sector */
                offset = buffer[0x02] * 256 + buffer[0x03];

                /* no more sectors to search */
                if (offset == 0xFFFF)
                    break;

                /* get next sector */
                offset *= blockSize;
                sector = (blockLocation + offset) / 2048;
            } while (true);

            if (size == 0)
            {
                CdCloseTrack(iterator, trackHandle);
                return HashEngine.IteratorError(iterator, "Could not find LaunchMe");
            }

            sector = blockLocation / 2048;

            while (size > 2048)
            {
                CdReadSector(iterator, trackHandle, (uint)sector, buffer, buffer.Length);
                md5.Append(buffer, buffer.Length);

                ++sector;
                size -= 2048;
            }

            CdReadSector(iterator, trackHandle, (uint)sector, buffer, (int)size);
            md5.Append(buffer, (int)size);
        }
        else
        {
            CdCloseTrack(iterator, trackHandle);
            return HashEngine.IteratorError(iterator, "Not a 3DO CD");
        }

        CdCloseTrack(iterator, trackHandle);

        return HashEngine.Finalize(iterator, md5, out hash);
    }

/// <summary>Hashes a Dreamcast disc (IP.BIN / track rules).</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashDreamcast(out string hash, RcHashIterator iterator)
    {
        hash = "";
        byte[] buffer = new byte[256];
        object? trackHandle;
        string exeFile = "";
        uint size;
        uint sector;
        int result = 0;
        HashMd5 md5;
        int i = 0;

        /* track 03 is the data track that contains the TOC and IP.BIN */
        trackHandle = CdOpenTrack(iterator, 3);
        if (trackHandle != null)
        {
            /* first 256 bytes from first sector should have IP.BIN structure that stores game meta information
             * https://mc.pp.se/dc/ip.bin.html */
            CdReadSector(iterator, trackHandle, CdFirstTrackSector(iterator, trackHandle), buffer, buffer.Length);
        }

        if (CdReader.Matches(buffer, 0, "SEGA SEGAKATANA ", 16) == false)
        {
            if (trackHandle != null)
                CdCloseTrack(iterator, trackHandle);

            /* not a gd-rom dreamcast file. check for mil-cd by looking for the marker in the first data track */
            trackHandle = CdOpenTrack(iterator, ConsoleIds.RC_HASH_CDTRACK_FIRST_DATA);
            if (trackHandle == null)
                return HashEngine.IteratorError(iterator, "Could not open track");

            CdReadSector(iterator, trackHandle, CdFirstTrackSector(iterator, trackHandle), buffer, buffer.Length);
            if (CdReader.Matches(buffer, 0, "SEGA SEGAKATANA ", 16) == false)
            {
                /* did not find marker on track 3 or first data track */
                CdCloseTrack(iterator, trackHandle);
                return HashEngine.IteratorError(iterator, "Not a Dreamcast CD");
            }
        }

        /* start the hash with the game meta information */
        md5 = new HashMd5();
        md5.Append(buffer, 256);

        {
            /* trim trailing spaces from the title region for the verbose message */
            int ptr = 0xFF;
            while (ptr > 0x80 && buffer[ptr - 1] == (byte)' ')
                --ptr;
            buffer[ptr] = 0;

            HashEngine.IteratorVerboseFormatted(iterator, "Found Dreamcast CD: {0} ({1})",
                CdReader.GetNulTerminatedString(buffer, 0x80, 128), CdReader.GetNulTerminatedString(buffer, 0x40, 16));
        }

        /* the boot filename is 96 bytes into the meta information (https://mc.pp.se/dc/ip0000.bin.html) */
        /* remove whitespace from bootfile */
        i = 0;
        while (i < 16 && !CdReader.IsSpace(buffer[96 + i]))
            ++i;

        /* sometimes boot file isn't present on meta information.
         * nothing can be done, as even the core doesn't run the game in this case. */
        if (i == 0)
        {
            CdCloseTrack(iterator, trackHandle);
            return HashEngine.IteratorError(iterator, "Boot executable not specified on IP.BIN");
        }

        exeFile = Encoding.ASCII.GetString(buffer, 96, i);

        sector = (uint)CdFindFileSector(iterator, trackHandle, exeFile, out size);
        if (sector == 0)
        {
            CdCloseTrack(iterator, trackHandle);
            return HashEngine.IteratorError(iterator, "Could not locate boot executable");
        }

        if (CdReadSector(iterator, trackHandle, sector, buffer, 1) != 0)
        {
            /* the boot executable is in the primary data track */
        }
        else
        {
            CdCloseTrack(iterator, trackHandle);

            /* the boot executable is normally in the last track */
            trackHandle = CdOpenTrack(iterator, ConsoleIds.RC_HASH_CDTRACK_LAST);
        }

        result = CdFile(md5, iterator, trackHandle, sector, null, size, "boot executable");
        CdCloseTrack(iterator, trackHandle);

        HashEngine.Finalize(iterator, md5, out hash);
        return result;
    }

    /* rc_hash_nintendo_disc_partition - GameCube/Wii partition hashing */
    private static int RcHashNintendoDiscPartition(HashMd5 md5, RcHashIterator iterator, object fileHandle, uint partOffset, int wiiShift)
    {
        const uint BASE_HEADER_SIZE = 0x2440;
        const uint MAX_HEADER_SIZE = 1024 * 1024;
        const int MAX_CHUNK_SIZE = 1024 * 1024;

        uint apploaderBodySize, apploaderTrailerSize, headerSize;

        byte[] quadBuffer = new byte[4];
        byte[] addrBuffer = new byte[0xD8];
        byte[]? buffer;

        ulong dolOffset;
        ulong[] dolOffsets = new ulong[18];
        ulong[] dolSizes = new ulong[18];

        int ix;
        ulong remainingSize;

        /* GetApploaderSize */
        HashEngine.FileSeek(iterator, fileHandle, partOffset + BASE_HEADER_SIZE + 0x14, HashEngine.SEEK_SET);
        const uint apploaderHeaderSize = 0x20;
        HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
        apploaderBodySize =
            ((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3];
        HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
        apploaderTrailerSize =
            ((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3];
        headerSize = BASE_HEADER_SIZE + apploaderHeaderSize + apploaderBodySize + apploaderTrailerSize;
        if (headerSize > MAX_HEADER_SIZE) headerSize = MAX_HEADER_SIZE;

        /* Hash headers */
        buffer = new byte[headerSize];
        HashEngine.FileSeek(iterator, fileHandle, partOffset, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, buffer, (int)headerSize);
        HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} byte partition header", headerSize);
        md5.Append(buffer, (int)headerSize);

        /* GetBootDOLOffset
         * Base header size is guaranteed larger than 0x423 therefore buffer contains dol_offset right now */
        dolOffset = (((ulong)buffer[0x420] << 24) |
            ((ulong)buffer[0x421] << 16) |
            ((ulong)buffer[0x422] << 8) |
            (ulong)buffer[0x423]) << wiiShift;

        /* Find offsets and sizes for the 7 main.dol code segments and 11 main.dol data segments */
        HashEngine.FileSeek(iterator, fileHandle, partOffset + (uint)dolOffset, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, addrBuffer, 0xD8);
        for (ix = 0; ix < 18; ix++)
        {
            dolOffsets[ix] = (((ulong)addrBuffer[ix * 4] << 24) |
                ((ulong)addrBuffer[ix * 4 + 1] << 16) |
                ((ulong)addrBuffer[ix * 4 + 2] << 8) |
                (ulong)addrBuffer[ix * 4 + 3]) << wiiShift;
            dolSizes[ix] = (((ulong)addrBuffer[0x90 + ix * 4] << 24) |
                ((ulong)addrBuffer[0x90 + ix * 4 + 1] << 16) |
                ((ulong)addrBuffer[0x90 + ix * 4 + 2] << 8) |
                (ulong)addrBuffer[0x90 + ix * 4 + 3]) << wiiShift;
        }

        /* Iterate through the 18 main.dol segments and hash each */
        buffer = new byte[MAX_CHUNK_SIZE];

        for (ix = 0; ix < 18; ix++)
        {
            if (dolSizes[ix] == 0)
                continue;

            HashEngine.FileSeek(iterator, fileHandle, partOffset + (uint)dolOffsets[ix], HashEngine.SEEK_SET);
            if (ix < 7)
                HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} byte main.dol code segment {1}", dolSizes[ix], ix);
            else
                HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} byte main.dol data segment {1}", dolSizes[ix], ix - 7);

            remainingSize = dolSizes[ix];
            while (remainingSize > MAX_CHUNK_SIZE)
            {
                HashEngine.FileRead(iterator, fileHandle, buffer, MAX_CHUNK_SIZE);
                md5.Append(buffer, MAX_CHUNK_SIZE);
                remainingSize -= MAX_CHUNK_SIZE;
            }
            HashEngine.FileRead(iterator, fileHandle, buffer, (int)remainingSize);
            md5.Append(buffer, (int)remainingSize);
        }

        return 1;
    }

/// <summary>Hashes a GameCube disc (partition reading).</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashGamecube(out string hash, RcHashIterator iterator)
    {
        hash = "";
        HashMd5 md5 = new();
        object? fileHandle;

        byte[] quadBuffer = new byte[4];
        int success;

        fileHandle = HashEngine.FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open file");

        /* Check Magic Word */
        HashEngine.FileSeek(iterator, fileHandle, 0x1c, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
        if (quadBuffer[0] == 0xC2 && quadBuffer[1] == 0x33 && quadBuffer[2] == 0x9F && quadBuffer[3] == 0x3D)
            success = RcHashNintendoDiscPartition(md5, iterator, fileHandle!, 0, 0);
        else
            success = HashEngine.IteratorError(iterator, "Not a Gamecube disc");

        /* Finalize */
        HashEngine.FileClose(iterator, fileHandle);

        if (success != 0)
            return HashEngine.Finalize(iterator, md5, out hash);

        return 0;
    }

    /* helper variable only used for testing */
    internal static string? JaguarCdHomebrewHash = null;

/// <summary>Hashes a Atari Jaguar CD image.</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashJaguarCd(out string hash, RcHashIterator iterator)
    {
        hash = "";
        byte[] buffer = new byte[2352];
        object? trackHandle;
        HashMd5 md5;
        int byteswapped = 0;
        uint size = 0;
        uint offset = 0;
        uint sector = 0;
        uint remaining;
        uint i;

        /* Jaguar CD header is in the first sector of the first data track OF THE SECOND SESSION.
         * The first track must be an audio track, but may be a warning message or actual game audio */
        trackHandle = CdOpenTrack(iterator, ConsoleIds.RC_HASH_CDTRACK_FIRST_OF_SECOND_SESSION);
        if (trackHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open track");

        /* The header is an unspecified distance into the first sector, but usually two bytes in.
         * It consists of 64 bytes of "TAIR" or "ATRI" repeating, depending on whether or not the data
         * is byteswapped. Then another 32 byte that reads "ATARI APPROVED DATA HEADER ATRI "
         * (possibly byteswapped). Then a big-endian 32-bit value for the address where the boot code
         * should be loaded, and a second big-endian 32-bit value for the size of the boot code. */
        sector = CdFirstTrackSector(iterator, trackHandle);
        CdReadSector(iterator, trackHandle, sector, buffer, buffer.Length);

        for (i = 64; i < buffer.Length - 32 - 4 * 3; i++)
        {
            if (CdReader.Matches(buffer, (int)i, "TARA IPARPVODED TA AEHDAREA RT I", 32))
            {
                byteswapped = 1;
                offset = i + 32 + 4;
                size = ((uint)buffer[offset] << 16) | ((uint)buffer[offset + 1] << 24) | (uint)buffer[offset + 2] | ((uint)buffer[offset + 3] << 8);
                break;
            }
            else if (CdReader.Matches(buffer, (int)i, "ATARI APPROVED DATA HEADER ATRI ", 32))
            {
                byteswapped = 0;
                offset = i + 32 + 4;
                size = ((uint)buffer[offset] << 24) | ((uint)buffer[offset + 1] << 16) | ((uint)buffer[offset + 2] << 8) | (uint)buffer[offset + 3];
                break;
            }
        }

        if (size == 0) /* did not see ATARI APPROVED DATA HEADER */
        {
            CdCloseTrack(iterator, trackHandle);
            return HashEngine.IteratorError(iterator, "Not a Jaguar CD");
        }

        i = 0; /* only loop once */
        do
        {
            md5 = new HashMd5();

            offset += 4;

            HashEngine.IteratorVerboseFormatted(iterator, "Hashing boot executable ({0} bytes starting at {1} bytes into sector {2})", size, offset, sector);

            if (size > HashEngine.MAX_BUFFER_SIZE)
                size = (uint)HashEngine.MAX_BUFFER_SIZE;

            do
            {
                if (byteswapped != 0)
                    HashEngine.Byteswap16(buffer, buffer.Length);

                remaining = (uint)buffer.Length - offset;
                if (remaining >= size)
                {
                    md5.Append(buffer, (int)offset, (int)size);
                    size = 0;
                    break;
                }

                md5.Append(buffer, (int)offset, (int)remaining);
                size -= remaining;
                offset = 0;
            } while (CdReadSector(iterator, trackHandle, ++sector, buffer, buffer.Length) == buffer.Length);

            CdCloseTrack(iterator, trackHandle);

            if (size > 0)
                return HashEngine.IteratorError(iterator, "Not enough data");

            HashEngine.Finalize(iterator, md5, out hash);

            /* homebrew games all seem to have the same boot executable and store the actual game code in track 2.
             * if we generated something other than the homebrew hash, return it. assume all homebrews are byteswapped. */
            if (string.CompareOrdinal(hash, "254487b59ab21bc005338e85cbf9fd2f") != 0 || byteswapped == 0)
            {
                if (JaguarCdHomebrewHash == null || string.CompareOrdinal(hash, JaguarCdHomebrewHash) != 0)
                    return 1;
            }

            /* if we've already been through the loop a second time, just return the hash */
            if (i == 1)
                return 1;
            ++i;

            HashEngine.IteratorVerboseFormatted(iterator, "Potential homebrew at sector {0}, checking for KART data in track 2", sector);

            trackHandle = CdOpenTrack(iterator, 2);
            if (trackHandle == null)
                return HashEngine.IteratorError(iterator, "Could not open track");

            /* track 2 of the homebrew code has the 64 bytes or ATRI followed by 32 bytes of "ATARI APPROVED DATA HEADER ATRI!",
             * then 64 bytes of KART repeating. */
            sector = CdFirstTrackSector(iterator, trackHandle);
            CdReadSector(iterator, trackHandle, sector, buffer, buffer.Length);
            if (CdReader.Matches(buffer, 0x5E, "RT!IRTKA", 8) == false)
                return HashEngine.IteratorError(iterator, "Homebrew executable not found in track 2");

            /* found KART data */
            HashEngine.IteratorVerbose(iterator, "Found KART data in track 2");

            offset = 0xA6;
            size = ((uint)buffer[offset] << 16) | ((uint)buffer[offset + 1] << 24) | (uint)buffer[offset + 2] | ((uint)buffer[offset + 3] << 8);
        } while (true);
    }

/// <summary>Hashes a Neo Geo CD disc (IPL.TXT executables).</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashNeogeoCd(out string hash, RcHashIterator iterator)
    {
        hash = "";
        byte[] buffer = new byte[1024];
        object? trackHandle;
        uint sector;
        uint size;
        HashMd5 md5;

        trackHandle = CdOpenTrack(iterator, 1);
        if (trackHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open track");

        /* https://wiki.neogeodev.org/index.php?title=IPL_file, https://wiki.neogeodev.org/index.php?title=PRG_file
         * IPL file specifies data to be loaded before the game starts. PRG files are the executable code */
        sector = (uint)CdFindFileSector(iterator, trackHandle, "IPL.TXT", out size);
        if (sector == 0)
        {
            CdCloseTrack(iterator, trackHandle);
            return HashEngine.IteratorError(iterator, "Not a NeoGeo CD game disc");
        }

        if (CdReadSector(iterator, trackHandle, sector, buffer, buffer.Length) == 0)
        {
            CdCloseTrack(iterator, trackHandle);
            return 0;
        }

        md5 = new HashMd5();

        int ptr = 0;
        do
        {
            int start = ptr;
            while (ptr < buffer.Length && buffer[ptr] != (byte)'.' && buffer[ptr] != 0)
                ++ptr;

            if (CdReader.StartsWithIgnoreCase(buffer, ptr, buffer.Length, ".PRG"))
            {
                ptr += 4;

                string filename = Encoding.ASCII.GetString(buffer, start, ptr - start);

                sector = (uint)CdFindFileSector(iterator, trackHandle, filename, out size);
                if (sector == 0 || CdFile(md5, iterator, trackHandle, sector, null, size, filename) == 0)
                {
                    CdCloseTrack(iterator, trackHandle);
                    return HashEngine.IteratorErrorFormatted(iterator, "Could not read {0}", Truncate16(filename));
                }
            }

            while (ptr < buffer.Length && buffer[ptr] != (byte)'\n' && buffer[ptr] != 0)
                ++ptr;
            if (ptr >= buffer.Length || buffer[ptr] != (byte)'\n')
                break;
            ++ptr;
        } while (ptr < buffer.Length && buffer[ptr] != 0 && buffer[ptr] != 0x1a);

        CdCloseTrack(iterator, trackHandle);
        return HashEngine.Finalize(iterator, md5, out hash);
    }

    private static string Truncate16(string text)
    {
        return text.Length > 16 ? text.Substring(0, 16) : text;
    }

    /* rc_hash_pce_track */
    private static int RcHashPceTrack(out string hash, object? trackHandle, RcHashIterator iterator)
    {
        hash = "";
        byte[] buffer = new byte[2048];
        HashMd5 md5;
        uint sector, numSectors;
        uint size;

        /* the PC-Engine uses the second sector to specify boot information and program name.
         * the string "PC Engine CD-ROM SYSTEM" should exist at 32 bytes into the sector
         * http://shu.sheldows.com/shu/download/pcedocs/pce_cdrom.html */
        if (CdReadSector(iterator, trackHandle, CdFirstTrackSector(iterator, trackHandle) + 1, buffer, 128) < 128)
            return HashEngine.IteratorError(iterator, "Not a PC Engine CD");

        /* normal PC Engine CD will have a header block in sector 1 */
        if (CdReader.Matches(buffer, 32, "PC Engine CD-ROM SYSTEM", 23))
        {
            /* the title of the disc is the last 22 bytes of the header */
            md5 = new HashMd5();
            md5.Append(buffer, 106, 22);

            HashEngine.IteratorVerboseFormatted(iterator, "Found PC Engine CD, title={0}", CdReader.GetNulTerminatedString(buffer, 106, 22));

            /* the first three bytes specify the sector of the program data, and the fourth byte
             * is the number of sectors. */
            sector = (uint)((buffer[0] << 16) + (buffer[1] << 8) + buffer[2]);
            numSectors = buffer[3];

            HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} sectors starting at sector {1}", numSectors, sector);

            sector += CdFirstTrackSector(iterator, trackHandle);
            while (numSectors > 0)
            {
                CdReadSector(iterator, trackHandle, sector, buffer, buffer.Length);
                md5.Append(buffer, buffer.Length);

                ++sector;
                --numSectors;
            }
        }
        /* GameExpress CDs use a standard Joliet filesystem - locate and hash the BOOT.BIN */
        else if ((sector = (uint)CdFindFileSector(iterator, trackHandle, "BOOT.BIN", out size)) != 0 && size < HashEngine.MAX_BUFFER_SIZE)
        {
            md5 = new HashMd5();
            while (size > buffer.Length)
            {
                CdReadSector(iterator, trackHandle, sector, buffer, buffer.Length);
                md5.Append(buffer, buffer.Length);

                ++sector;
                size -= (uint)buffer.Length;
            }

            if (size > 0)
            {
                CdReadSector(iterator, trackHandle, sector, buffer, (int)size);
                md5.Append(buffer, (int)size);
            }
        }
        else
        {
            return HashEngine.IteratorError(iterator, "Not a PC Engine CD");
        }

        return HashEngine.Finalize(iterator, md5, out hash);
    }

/// <summary>Hashes a PC Engine CD disc.</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashPceCd(out string hash, RcHashIterator iterator)
    {
        hash = "";
        int result;
        object? trackHandle = CdOpenTrack(iterator, ConsoleIds.RC_HASH_CDTRACK_FIRST_DATA);
        if (trackHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open track");

        result = RcHashPceTrack(out hash, trackHandle, iterator);

        CdCloseTrack(iterator, trackHandle);

        return result;
    }

/// <summary>Hashes a PC-FX image.</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashPcfxCd(out string hash, RcHashIterator iterator)
    {
        hash = "";
        byte[] buffer = new byte[2048];
        object? trackHandle;
        HashMd5 md5;
        int sector, numSectors;

        /* PC-FX executable can be in any track. Assume it's in the largest data track and check there first */
        trackHandle = CdOpenTrack(iterator, ConsoleIds.RC_HASH_CDTRACK_LARGEST);
        if (trackHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open track");

        /* PC-FX CD will have a header marker in sector 0 */
        sector = (int)CdFirstTrackSector(iterator, trackHandle);
        CdReadSector(iterator, trackHandle, (uint)sector, buffer, 32);
        if (CdReader.Matches(buffer, 0, "PC-FX:Hu_CD-ROM", 15) == false)
        {
            CdCloseTrack(iterator, trackHandle);

            /* not found in the largest data track, check track 2 */
            trackHandle = CdOpenTrack(iterator, 2);
            if (trackHandle == null)
                return HashEngine.IteratorError(iterator, "Could not open track");

            sector = (int)CdFirstTrackSector(iterator, trackHandle);
            CdReadSector(iterator, trackHandle, (uint)sector, buffer, 32);
        }

        if (CdReader.Matches(buffer, 0, "PC-FX:Hu_CD-ROM", 15))
        {
            /* PC-FX boot header fills the first two sectors of the disc
             * https://bitbucket.org/trap15/pcfxtools/src/master/pcfx-cdlink.c
             * the important stuff is the first 128 bytes of the second sector (title being the first 32) */
            CdReadSector(iterator, trackHandle, (uint)(sector + 1), buffer, 128);

            md5 = new HashMd5();
            md5.Append(buffer, 128);

            HashEngine.IteratorVerboseFormatted(iterator, "Found PC-FX CD, title={0}", CdReader.GetNulTerminatedString(buffer, 0, 32));

            /* the program sector is in bytes 33-36 (assume byte 36 is 0) */
            sector = (buffer[34] << 16) + (buffer[33] << 8) + buffer[32];

            /* the number of sectors the program occupies is in bytes 37-40 (assume byte 40 is 0) */
            numSectors = (buffer[38] << 16) + (buffer[37] << 8) + buffer[36];

            HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} sectors starting at sector {1}", numSectors, sector);

            sector += (int)CdFirstTrackSector(iterator, trackHandle);
            while (numSectors > 0)
            {
                CdReadSector(iterator, trackHandle, (uint)sector, buffer, buffer.Length);
                md5.Append(buffer, buffer.Length);

                ++sector;
                --numSectors;
            }
        }
        else
        {
            int result = 0;
            CdReadSector(iterator, trackHandle, (uint)(sector + 1), buffer, 128);

            /* some PC-FX CDs still identify as PCE CDs */
            if (CdReader.Matches(buffer, 32, "PC Engine CD-ROM SYSTEM", 23))
                result = RcHashPceTrack(out hash, trackHandle, iterator);

            CdCloseTrack(iterator, trackHandle);
            if (result != 0)
                return result;

            return HashEngine.IteratorError(iterator, "Not a PC-FX CD");
        }

        CdCloseTrack(iterator, trackHandle);

        return HashEngine.Finalize(iterator, md5, out hash);
    }

    /* rc_hash_find_playstation_executable - parse SYSTEM.CNF for the boot key
     * and resolve the executable via the ISO9660 mini-parser */
    private static int FindPlaystationExecutable(RcHashIterator iterator, object? trackHandle,
                                                 string bootKey, string cdromPrefix,
                                                 out string exeName, out uint exeSize)
    {
        byte[] buffer = new byte[2048];
        uint size;
        int sector;

        exeName = "";
        exeSize = 0;

        sector = CdFindFileSector(iterator, trackHandle, "SYSTEM.CNF", out _);
        if (sector == 0)
            return 0;

        size = (uint)CdReadSector(iterator, trackHandle, (uint)sector, buffer, buffer.Length - 1);
        /* buffer[size] = '\0' — implied: the C# buffer is zero-filled */

        sector = 0;
        for (int ptr = 0; ptr < size; ++ptr)
        {
            if (CdReader.StartsWith(buffer, ptr, bootKey))
            {
                ptr += bootKey.Length;
                while (ptr < size && CdReader.IsSpace(buffer[ptr]))
                    ++ptr;

                if (ptr < size && buffer[ptr] == (byte)'=')
                {
                    ++ptr;
                    while (ptr < size && CdReader.IsSpace(buffer[ptr]))
                        ++ptr;

                    if (CdReader.StartsWith(buffer, ptr, cdromPrefix))
                        ptr += cdromPrefix.Length;
                    while (ptr < size && buffer[ptr] == (byte)'\\')
                        ++ptr;

                    int start = ptr;
                    while (ptr < size && !CdReader.IsSpace(buffer[ptr]) && buffer[ptr] != (byte)';')
                        ++ptr;

                    int nameLen = ptr - start;
                    if (nameLen >= 64)
                        nameLen = 64 - 1;

                    exeName = Encoding.ASCII.GetString(buffer, start, nameLen);

                    HashEngine.IteratorVerboseFormatted(iterator, "Looking for boot executable: {0}", exeName);

                    sector = CdFindFileSector(iterator, trackHandle, exeName, out exeSize);
                    break;
                }
            }

            /* advance to end of line */
            while (ptr < size && buffer[ptr] != (byte)'\n')
                ++ptr;
        }

        return sector;
    }

/// <summary>Hashes a PlayStation disc (SYSTEM.CNF boot executable).</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashPsx(out string hash, RcHashIterator iterator)
    {
        hash = "";
        byte[] buffer = new byte[32];
        string exeName = "";
        object? trackHandle;
        uint sector;
        uint size;
        int result = 0;
        HashMd5 md5;

        trackHandle = CdOpenTrack(iterator, 1);
        if (trackHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open track");

        sector = (uint)FindPlaystationExecutable(iterator, trackHandle, "BOOT", "cdrom:", out exeName, out size);
        if (sector == 0)
        {
            sector = (uint)CdFindFileSector(iterator, trackHandle, "PSX.EXE", out size);
            if (sector != 0)
                exeName = "PSX.EXE";
        }

        if (sector == 0)
        {
            HashEngine.IteratorError(iterator, "Could not locate primary executable");
        }
        else if (CdReadSector(iterator, trackHandle, sector, buffer, buffer.Length) < buffer.Length)
        {
            HashEngine.IteratorError(iterator, "Could not read primary executable");
        }
        else
        {
            if (CdReader.Matches(buffer, 0, "PS-X EXE", 7) == false)
            {
                HashEngine.IteratorVerboseFormatted(iterator, "{0} did not contain PS-X EXE marker", exeName);
            }
            else
            {
                /* the PS-X EXE header specifies the executable size as a 4-byte value 28 bytes into the header, which doesn't
                 * include the header itself. We want to include the header in the hash, so append another 2048 to that value. */
                size = (((uint)buffer[31] << 24) | ((uint)buffer[30] << 16) | ((uint)buffer[29] << 8) | buffer[28]) + 2048;
            }

            /* there's a few games that use a singular engine and only differ via their data files. luckily, they have unique
             * serial numbers, and use the serial number as the boot file in the standard way. include the boot file name in the hash. */
            md5 = new HashMd5();
            byte[] nameBytes = Encoding.ASCII.GetBytes(exeName);
            md5.Append(nameBytes, nameBytes.Length);

            result = CdFile(md5, iterator, trackHandle, sector, exeName, size, "primary executable");
            HashEngine.Finalize(iterator, md5, out hash);
        }

        CdCloseTrack(iterator, trackHandle);

        return result;
    }

/// <summary>Hashes a PlayStation 2 disc (BOOT2 ELF via ISO9660).</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashPs2(out string hash, RcHashIterator iterator)
    {
        hash = "";
        byte[] buffer = new byte[4];
        string exeName = "";
        object? trackHandle;
        uint sector;
        uint size;
        int result = 0;
        HashMd5 md5;

        trackHandle = CdOpenTrack(iterator, 1);
        if (trackHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open track");

        sector = (uint)FindPlaystationExecutable(iterator, trackHandle, "BOOT2", "cdrom0:", out exeName, out size);
        if (sector == 0)
        {
            HashEngine.IteratorError(iterator, "Could not locate primary executable");
        }
        else if (CdReadSector(iterator, trackHandle, sector, buffer, buffer.Length) < buffer.Length)
        {
            HashEngine.IteratorError(iterator, "Could not read primary executable");
        }
        else
        {
            if (CdReader.Matches(buffer, 0, new byte[] { 0x7F, (byte)'E', (byte)'L', (byte)'F' }, 4) == false)
                HashEngine.IteratorVerboseFormatted(iterator, "{0} did not contain ELF marker", exeName);

            /* there's a few games that use a singular engine and only differ via their data files. luckily, they have unique
             * serial numbers, and use the serial number as the boot file in the standard way. include the boot file name in the hash.
             */
            md5 = new HashMd5();
            byte[] nameBytes = Encoding.ASCII.GetBytes(exeName);
            md5.Append(nameBytes, nameBytes.Length);

            result = CdFile(md5, iterator, trackHandle, sector, exeName, size, "primary executable");
            HashEngine.Finalize(iterator, md5, out hash);
        }

        CdCloseTrack(iterator, trackHandle);

        return result;
    }

/// <summary>Hashes a PSP disc (PARAM.SFO + EBOOT.BIN).</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashPsp(out string hash, RcHashIterator iterator)
    {
        hash = "";
        object? trackHandle;
        uint sector;
        uint size;
        HashMd5 md5;

        /* https://www.psdevwiki.com/psp/PBP
         * A PBP file is an archive containing the PARAM.SFO, primary executable, and a bunch of metadata.
         * While we could extract the PARAM.SFO and primary executable to mimic the normal PSP hashing logic,
         * it's easier to just hash the entire file. This also helps alleviate issues where the primary
         * executable is just a game engine and the only differentiating data would be the metadata. */
        if (HashEngine.PathCompareExtension(iterator.Path!, "pbp") != 0)
            return HashEngine.WholeFile(out hash, iterator);

        trackHandle = CdOpenTrack(iterator, 1);
        if (trackHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open track");

        /* http://www.romhacking.net/forum/index.php?topic=30899.0
         * PSP_GAME/PARAM.SFO contains key/value pairs identifying the game for the system (i.e. serial number,
         * name, version). PSP_GAME/SYSDIR/EBOOT.BIN is the encrypted primary executable. */
        sector = (uint)CdFindFileSector(iterator, trackHandle, "PSP_GAME\\PARAM.SFO", out size);
        if (sector == 0)
        {
            CdCloseTrack(iterator, trackHandle);
            return HashEngine.IteratorError(iterator, "Not a PSP game disc");
        }

        md5 = new HashMd5();
        if (CdFile(md5, iterator, trackHandle, sector, null, size, "PSP_GAME\\PARAM.SFO") == 0)
        {
            CdCloseTrack(iterator, trackHandle);
            return 0;
        }

        sector = (uint)CdFindFileSector(iterator, trackHandle, "PSP_GAME\\SYSDIR\\EBOOT.BIN", out size);
        if (sector == 0)
        {
            CdCloseTrack(iterator, trackHandle);
            return HashEngine.IteratorError(iterator, "Could not find primary executable");
        }

        if (CdFile(md5, iterator, trackHandle, sector, null, size, "PSP_GAME\\SYSDIR\\EBOOT.BIN") == 0)
        {
            CdCloseTrack(iterator, trackHandle);
            return 0;
        }

        CdCloseTrack(iterator, trackHandle);
        return HashEngine.Finalize(iterator, md5, out hash);
    }

/// <summary>Hashes a Sega CD / Saturn disc (first 512 bytes of sector 0).</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashSegaCd(out string hash, RcHashIterator iterator)
    {
        hash = "";
        byte[] buffer = new byte[512];
        object? trackHandle;

        trackHandle = CdOpenTrack(iterator, 1);
        if (trackHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open track");

        /* the first 512 bytes of sector 0 are a volume header and ROM header that uniquely identify the game.
         * After that is an arbitrary amount of code that ensures the game is being run in the correct region.
         * Then more arbitrary code follows that actually starts the boot process. Somewhere in there, the
         * primary executable is loaded. In many cases, a single game will have multiple executables, so even
         * if we could determine the primary one, it's just the tip of the iceberg. As such, we've decided that
         * hashing the volume and ROM headers is sufficient for identifying the game, and we'll have to trust
         * that our players aren't modifying anything else on the disc. */
        CdReadSector(iterator, trackHandle, 0, buffer, buffer.Length);
        CdCloseTrack(iterator, trackHandle);

        if (CdReader.Matches(buffer, 0, "SEGADISCSYSTEM  ", 16) == false && /* Sega CD */
            CdReader.Matches(buffer, 0, "SEGA SEGASATURN ", 16) == false)  /* Sega Saturn */
        {
            return HashEngine.IteratorError(iterator, "Not a Sega CD");
        }

        return HashEngine.HashBuffer(out hash, buffer, buffer.Length, iterator);
    }

    /* rc_hash_wii_disc */
    private static int RcHashWiiDisc(HashMd5 md5, RcHashIterator iterator, object fileHandle)
    {
        const uint MAIN_HEADER_SIZE = 0x80;
        const ulong REGION_CODE_ADDRESS = 0x4E000;
        const uint CLUSTER_SIZE = 0x7C00;
        const uint MAX_CLUSTER_COUNT = 1024;

        uint[] partitionInfoTable = new uint[8];
        uint totalPartitionCount = 0;
        uint[] partitionTable;
        ulong tmdOffset;
        uint tmdSize;
        ulong partOffset;
        ulong partSize;
        uint clusterCount;

        byte[] quadBuffer = new byte[4];
        byte[] buffer;

        uint ix, jx;
        int encrypted;

        /* Check encryption byte - if 0x61 is 0, disc is encrypted */
        HashEngine.FileSeek(iterator, fileHandle, 0x61, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, quadBuffer, 1);
        encrypted = (quadBuffer[0] == 0) ? 1 : 0;

        /* Hash main headers */
        buffer = new byte[CLUSTER_SIZE];

        /* (the C prints the buffer contents before reading it — uninitialized in C;
         * the C# buffer is zero-filled, verbose output only) */
        HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} byte main header for [{1}{2}{3}{4}{5}{6}]",
            MAIN_HEADER_SIZE, (char)buffer[0], (char)buffer[1], (char)buffer[2], (char)buffer[3], (char)buffer[4], (char)buffer[5]);
        HashEngine.FileSeek(iterator, fileHandle, 0, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, buffer, (int)MAIN_HEADER_SIZE);
        md5.Append(buffer, (int)MAIN_HEADER_SIZE);

        /* Hash region code */
        HashEngine.FileSeek(iterator, fileHandle, (long)REGION_CODE_ADDRESS, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
        md5.Append(quadBuffer, 4);

        /* Scan partition table */
        HashEngine.FileSeek(iterator, fileHandle, 0x40000, HashEngine.SEEK_SET);
        for (ix = 0; ix < 8; ix++)
        {
            HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
            partitionInfoTable[ix] =
                ((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3];
            if (ix % 2 == 0)
                totalPartitionCount += partitionInfoTable[ix];
        }

        if (totalPartitionCount == 0)
        {
            return HashEngine.IteratorError(iterator, "No partitions found");
        }

        partitionTable = new uint[totalPartitionCount * 2];
        uint kx = 0;
        for (jx = 0; jx < 8; jx += 2)
        {
            HashEngine.FileSeek(iterator, fileHandle, (long)(((ulong)partitionInfoTable[jx + 1]) << 2), HashEngine.SEEK_SET);
            for (ix = 0; ix < partitionInfoTable[jx]; ix++)
            {
                HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
                partitionTable[kx++] =
                    ((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3];
                HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
                partitionTable[kx++] =
                    ((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3];
            }
        }

        /* Read each partition */
        for (jx = 0; jx < totalPartitionCount * 2; jx += 2)
        {
            /* Don't hash Update partition */
            if (partitionTable[jx + 1] == 1)
                continue;

            /* Hash title metadata */
            HashEngine.FileSeek(iterator, fileHandle, (long)(((ulong)partitionTable[jx] << 2) + 0x2A4), HashEngine.SEEK_SET);
            HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
            tmdSize =
                ((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3];
            HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
            tmdOffset =
                ((ulong)(((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3])) << 2;

            if (tmdSize > CLUSTER_SIZE)
                tmdSize = CLUSTER_SIZE;

            HashEngine.FileSeek(iterator, fileHandle, (long)(((ulong)partitionTable[jx] << 2) + tmdOffset), HashEngine.SEEK_SET);
            HashEngine.FileRead(iterator, fileHandle, buffer, (int)tmdSize);
            HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} byte title metadata (partition type {1})",
                tmdSize, partitionTable[jx + 1]);
            md5.Append(buffer, (int)tmdSize);

            /* Hash partition */
            HashEngine.FileSeek(iterator, fileHandle, (long)(((ulong)partitionTable[jx] << 2) + 0x2B8), HashEngine.SEEK_SET);
            HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
            partOffset =
                ((ulong)(((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3])) << 2;
            HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
            partSize =
                ((ulong)(((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3])) << 2;

            if (encrypted != 0)
            {
                clusterCount = (partSize / 0x8000 > MAX_CLUSTER_COUNT) ? MAX_CLUSTER_COUNT : (uint)(partSize / 0x8000);
                HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} encrypted clusters ({1} bytes)",
                    clusterCount, clusterCount * CLUSTER_SIZE);
                for (ix = 0; ix < clusterCount; ix++)
                {
                    HashEngine.FileSeek(iterator, fileHandle, (long)(partOffset + (ix * 0x8000) + 0x400), HashEngine.SEEK_SET);
                    HashEngine.FileRead(iterator, fileHandle, buffer, (int)CLUSTER_SIZE);
                    md5.Append(buffer, (int)CLUSTER_SIZE);
                }
            }
            else /* Decrypted */
            {
                if (RcHashNintendoDiscPartition(md5, iterator, fileHandle, (uint)partOffset, 2) == 0)
                {
                    return HashEngine.IteratorError(iterator, "Failed to hash Wii partition");
                }
            }
        }

        return 1;
    }

    /* rc_hash_wiiware */
    private static int RcHashWiiware(HashMd5 md5, RcHashIterator iterator, object fileHandle)
    {
        uint certChainSize, ticketSize, tmdSize;
        uint tmdStartAddr, contentCount, contentAddr, contentSize, bufferSize;
        uint ix;

        byte[] quadBuffer = new byte[4];
        byte[] buffer;

        HashEngine.FileSeek(iterator, fileHandle, 0x08, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
        certChainSize =
            ((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3];
        /* Each content is individually aligned to a 0x40-byte boundary. */
        certChainSize = (certChainSize + 0x3F) & ~0x3Fu;
        HashEngine.FileSeek(iterator, fileHandle, 0x10, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
        ticketSize =
            ((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3];
        ticketSize = (ticketSize + 0x3F) & ~0x3Fu;
        HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
        tmdSize =
            ((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3];
        tmdSize = (tmdSize + 0x3F) & ~0x3Fu;
        if (tmdSize > HashEngine.MAX_BUFFER_SIZE)
            tmdSize = (uint)HashEngine.MAX_BUFFER_SIZE;

        tmdStartAddr = 0x40 + certChainSize + ticketSize;

        /* Hash TMD */
        buffer = new byte[tmdSize];
        HashEngine.FileSeek(iterator, fileHandle, tmdStartAddr, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, buffer, (int)tmdSize);
        HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} byte TMD", tmdSize);
        md5.Append(buffer, (int)tmdSize);

        /* Get count of content sections */
        HashEngine.FileSeek(iterator, fileHandle, (long)tmdStartAddr + 0x1de, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, quadBuffer, 2);
        contentCount = ((uint)quadBuffer[0] << 8) | quadBuffer[1];
        HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} content sections", contentCount);
        contentAddr = tmdStartAddr + tmdSize;
        for (ix = 0; ix < contentCount; ix++)
        {
            /* Get content section size */
            HashEngine.FileSeek(iterator, fileHandle, (long)tmdStartAddr + 0x1e4 + 8 + ix * 0x24, HashEngine.SEEK_SET);
            HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
            if (quadBuffer[0] == 0x00 && quadBuffer[1] == 0x00 && quadBuffer[2] == 0x00 && quadBuffer[3] == 0x00)
            {
                HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
                contentSize =
                    ((uint)quadBuffer[0] << 24) | ((uint)quadBuffer[1] << 16) | ((uint)quadBuffer[2] << 8) | quadBuffer[3];
                /* Padding between content should be ignored. But because the content data is encrypted,
                the size to hash for each content should be rounded up to the size of an AES block (16 bytes). */
                contentSize = (contentSize + 0x0F) & ~0x0Fu;
            }
            else
            {
                /* size > 4GB, just assume MAX_BUFFER_SIZE */
                contentSize = (uint)HashEngine.MAX_BUFFER_SIZE;
            }
            bufferSize = (contentSize > HashEngine.MAX_BUFFER_SIZE) ? (uint)HashEngine.MAX_BUFFER_SIZE : contentSize;

            /* Hash content */
            buffer = new byte[bufferSize];
            HashEngine.FileSeek(iterator, fileHandle, contentAddr, HashEngine.SEEK_SET);
            HashEngine.FileRead(iterator, fileHandle, buffer, (int)bufferSize);
            md5.Append(buffer, (int)bufferSize);
            contentAddr += contentSize;
            contentAddr = (contentAddr + 0x3F) & ~0x3Fu;
        }

        return 1;
    }

/// <summary>Hashes a Wii disc (partition path).</summary>
/// <param name="hash">the generated 32-char hash</param>
/// <param name="iterator">the hash iterator</param>
/// <returns>the result</returns>
    public static int RcHashWii(out string hash, RcHashIterator iterator)
    {
        hash = "";
        HashMd5 md5 = new();
        object? fileHandle;

        byte[] quadBuffer = new byte[4];
        int success;

        fileHandle = HashEngine.FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open file");

        /* Check Magic Words */
        HashEngine.FileSeek(iterator, fileHandle, 0x18, HashEngine.SEEK_SET);
        HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
        if (quadBuffer[0] == 0x5D && quadBuffer[1] == 0x1C && quadBuffer[2] == 0x9E && quadBuffer[3] == 0xA3)
        {
            success = RcHashWiiDisc(md5, iterator, fileHandle);
        }
        else
        {
            HashEngine.FileSeek(iterator, fileHandle, 0x04, HashEngine.SEEK_SET);
            HashEngine.FileRead(iterator, fileHandle, quadBuffer, 4);
            if (quadBuffer[0] == (byte)'I' && quadBuffer[1] == (byte)'s' && quadBuffer[2] == 0x00 && quadBuffer[3] == 0x00)
                success = RcHashWiiware(md5, iterator, fileHandle);
            else
                success = HashEngine.IteratorError(iterator, "Not a supported Wii file");
        }

        /* Finalize */
        HashEngine.FileClose(iterator, fileHandle);

        if (success != 0)
            return HashEngine.Finalize(iterator, md5, out hash);

        return 0;
    }
}
