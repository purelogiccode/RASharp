// Ported from rcheevos (MIT) — src/rhash/hash_rom.c
// Cartridge hash algorithms: 7800, Arcade, Arduboy (Intel HEX text hash),
// Lynx, NES/FDS, N64 (byteswap variants), NDS/DSi (SuperCard header, arm9/
// arm7/icon blocks), PCE, SCV, SNES. Translated 1:1.

using System.Text;
using RetroAchievementsSharp.Models;

namespace RetroAchievementsSharp;

/// <summary>Ported from rcheevos (MIT) — src/rhash/hash_rom.c Cartridge hash algorithms: 7800, Arcade, Arduboy (Intel HEX text hash), Lynx, NES/FDS, N64 (byteswap variants)</summary>
public static class HashRom
{
    private static int UnheaderedIteratorBuffer(out string hash, RcHashIterator iterator, int headerSize)
    {
        return HashEngine.HashBuffer(out hash, iterator.Buffer!, headerSize, iterator.BufferSize - headerSize, iterator);
    }

    private static int IteratorBuffer(out string hash, RcHashIterator iterator)
    {
        return HashEngine.HashBuffer(out hash, iterator.Buffer!, 0, iterator.BufferSize, iterator);
    }

    private static bool MemEquals(byte[] buffer, int offset, string text)
    {
        if (offset + text.Length > buffer.Length)
            return false;

        for (var i = 0; i < text.Length; ++i)
        {
            if (buffer[offset + i] != (byte)text[i])
                return false;
        }

        return true;
    }

    private static uint ReadLe32(byte[] buffer, int offset)
    {
        return (uint)(buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24));
    }

    /* ===================================================== */

    /// <summary>=====================================================</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHash7800(out string hash, RcHashIterator iterator)
    {
        /* if the file contains a header, ignore it */
        if (iterator.BufferSize > 128 && MemEquals(iterator.Buffer!, 1, "ATARI7800"))
        {
            HashEngine.IteratorVerbose(iterator, "Ignoring 7800 header");
            return UnheaderedIteratorBuffer(out hash, iterator, 128);
        }

        return IteratorBuffer(out hash, iterator);
    }

    /// <summary>Hashes an arcade romset by filename (FBNeo semantics).</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashArcade(out string hash, RcHashIterator iterator)
    {
        /* arcade hash is just the hash of the filename (no extension) - the cores are pretty stringent about having the right ROM data */
        var filename = HashEngine.PathGetFilename(iterator.Path!);
        var ext = HashEngine.PathGetExtension(filename);
        var filenameLength = filename.Length - ext.Length - 1;

        /* fbneo supports loading subsystems by using specific folder names.
         * if one is found, include it in the hash.
         * https://github.com/libretro/FBNeo/blob/master/src/burner/libretro/README.md#emulating-consoles-and-computers
         */
        var filenameIndex = iterator.Path!.Length - filename.Length;
        if (filenameIndex > 1)
        {
            var includeFolder = false;

            /* walk back from the separator before the filename to the separator
             * before the parent folder (C: folder = filename - 1, walk while
             * folder[-1] is not a separator and folder > path) */
            var folder = filenameIndex - 1;
            while (folder > 0 && iterator.Path[folder - 1] != '/' && iterator.Path[folder - 1] != '\\')
            {
                --folder;
            }

            var parentFolderLength = filenameIndex - folder - 1;
            var folderName = "";
            if (parentFolderLength < 16)
            {
                folderName = iterator.Path.Substring(folder, parentFolderLength).ToLowerInvariant();
            }

            switch (parentFolderLength)
            {
                case 3:
                    if (folderName is "nes" or "fds" or "sms" or "msx" or "ngp" or "pce" or "chf" or "sgx")
                    {
                        includeFolder = true;
                    }

                    break;
                case 4:
                    if (folderName is "tg16" or "msx1")
                    {
                        includeFolder = true;
                    }

                    break;
                case 5:
                    if (string.Equals(folderName, "neocd", StringComparison.Ordinal))
                    {
                        includeFolder = true;
                    }

                    break;
                case 6:
                    if (folderName is "coleco" or "sg1000")
                    {
                        includeFolder = true;
                    }

                    break;
                case 7:
                    if (string.Equals(folderName, "genesis", StringComparison.Ordinal))
                    {
                        includeFolder = true;
                    }

                    break;
                case 8:
                    if (folderName is "gamegear" or "megadriv" or "pcengine" or "channelf" or "spectrum")
                    {
                        includeFolder = true;
                    }

                    break;
                case 9:
                    if (string.Equals(folderName, "megadrive", StringComparison.Ordinal))
                    {
                        includeFolder = true;
                    }

                    break;
                case 10:
                    if (folderName is "supergrafx" or "zxspectrum")
                    {
                        includeFolder = true;
                    }

                    break;
                case 12:
                    if (folderName is "mastersystem" or "colecovision")
                    {
                        includeFolder = true;
                    }

                    break;
            }

            if (includeFolder)
            {
                if (parentFolderLength + filenameLength + 1 < 128)
                {
                    /* buffer[parent_folder_length] = '_'; copy filename after it */
                    var combined = Encoding.UTF8.GetBytes(folderName + "_" + filename.Substring(0, filenameLength));
                    return HashEngine.HashBuffer(out hash, combined, 0, combined.Length, iterator);
                }
            }
        }

        var nameBytes = Encoding.UTF8.GetBytes(filename.Substring(0, filenameLength));
        return HashEngine.HashBuffer(out hash, nameBytes, 0, filenameLength, iterator);
    }

    /* rc_hash_text — line-normalized text hash (Arduboy Intel HEX) */
    private static readonly byte[] LineEnding = [(byte)'\n'];

    private static int RcHashText(out string hash, RcHashIterator iterator)
    {
        var md5 = new HashMd5();
        var buffer = iterator.Buffer!;
        var scan = 0;
        var stop = iterator.BufferSize;

        do
        {
            var line = scan;

            /* find end of line */
            while (scan < stop && buffer[scan] != (byte)'\r' && buffer[scan] != (byte)'\n')
            {
                ++scan;
            }

            md5.Append(buffer, line, scan - line);

            /* include a normalized line ending */
            /* NOTE: this causes a line ending to be hashed at the end of the file, even if one was not present */
            md5.Append(LineEnding, 0, 1);

            /* skip newline */
            if (scan < stop && buffer[scan] == (byte)'\r')
            {
                ++scan;
            }

            if (scan < stop && buffer[scan] == (byte)'\n')
            {
                ++scan;
            }
        } while (scan < stop);

        return HashEngine.Finalize(iterator, md5, out hash);
    }

    /* rc_hash_neogeo_cart (rcheevos 12.4.0): Geolith .neo cart format.
     * The first 4096 bytes are a header (magic, ROM section sizes, metadata
     * text fields that can differ between conversion tools), so only the
     * decrypted ROM data after the header participates in the hash. */
    /// <summary>rc_hash_neogeo_cart (rcheevos 12.4.0): Geolith .neo cart format. The first 4096 bytes are a header (magic, ROM section sizes, metadata text fields that can diff</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashNeogeoCart(out string hash, RcHashIterator iterator)
    {
        const int headerSize = 4096;
        const int chunkSize = 65536;
        hash = "";

        if (iterator.Buffer != null)
        {
            if (iterator.BufferSize < headerSize || !MemEquals(iterator.Buffer, 0, "NEO\x01"))
                return HashEngine.IteratorError(iterator, "Not a valid .neo file");

            HashEngine.IteratorVerbose(iterator, "Ignoring NEO header");
            return UnheaderedIteratorBuffer(out hash, iterator, headerSize);
        }

        var fileHandle = HashEngine.FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open file");

        var header = new byte[4];
        HashEngine.FileSeek(iterator, fileHandle, 0, HashEngine.SeekSet);
        if (HashEngine.FileRead(iterator, fileHandle, header, 4) != 4 || !MemEquals(header, 0, "NEO\x01"))
        {
            HashEngine.FileClose(iterator, fileHandle);
            return HashEngine.IteratorError(iterator, "Not a valid .neo file");
        }

        HashEngine.FileSeek(iterator, fileHandle, 0, HashEngine.SeekEnd);
        var size = HashEngine.FileTell(iterator, fileHandle);
        if (size <= headerSize)
        {
            HashEngine.FileClose(iterator, fileHandle);
            return HashEngine.IteratorError(iterator, "Not a valid .neo file");
        }

        size -= headerSize;

        long remaining;
        if (size > HashEngine.MaxBufferSize)
        {
            HashEngine.IteratorVerboseFormatted(iterator, "Hashing first {0} bytes (of {1} bytes) of {2} after 4096 byte header",
                (uint)HashEngine.MaxBufferSize, (uint)size, HashEngine.PathGetFilename(iterator.Path!));
            remaining = HashEngine.MaxBufferSize;
        }
        else
        {
            HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} ({1} bytes after 4096 byte header)",
                HashEngine.PathGetFilename(iterator.Path!), (uint)size);
            remaining = size;
        }

        var md5 = new HashMd5();
        var buffer = new byte[chunkSize];

        HashEngine.FileSeek(iterator, fileHandle, headerSize, HashEngine.SeekSet);
        while (remaining >= chunkSize)
        {
            HashEngine.FileRead(iterator, fileHandle, buffer, chunkSize);
            md5.Append(buffer, chunkSize);
            remaining -= chunkSize;
        }

        if (remaining > 0)
        {
            HashEngine.FileRead(iterator, fileHandle, buffer, (int)remaining);
            md5.Append(buffer, (int)remaining);
        }

        var result = HashEngine.Finalize(iterator, md5, out hash);
        HashEngine.FileClose(iterator, fileHandle);
        return result;
    }

    /// <summary>Hashes an Arduboy image (zip or Intel HEX text).</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashArduboy(out string hash, RcHashIterator iterator)
    {
        if (iterator.Path != null && HashEngine.PathCompareExtension(iterator.Path, "arduboy") != 0)
        {
            return HashZip.RcHashArduboyFx(out hash, iterator);
        }

        if (iterator.Buffer == null)
            return HashEngine.BufferedFile(out hash, ConsoleIds.RcConsoleArduboy, iterator);

        /* https://en.wikipedia.org/wiki/Intel_HEX */
        return RcHashText(out hash, iterator);
    }

    /// <summary>Hashes an Atari Lynx cartridge (64-byte header stripped when present).</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashLynx(out string hash, RcHashIterator iterator)
    {
        /* if the file contains a header, ignore it */
        /* NOTE: memcmp against "LYNX" compares 5 bytes (includes the NUL terminator) */
        if (iterator.BufferSize > 64 && MemEquals(iterator.Buffer!, 0, "LYNX\0"))
        {
            HashEngine.IteratorVerbose(iterator, "Ignoring LYNX header");
            return UnheaderedIteratorBuffer(out hash, iterator, 64);
        }

        return IteratorBuffer(out hash, iterator);
    }

    /// <summary>Hashes a NES/FDS image (iNES/FDS header stripped when present).</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashNes(out string hash, RcHashIterator iterator)
    {
        switch (iterator.BufferSize)
        {
            /* if the file contains a header, ignore it */
            case > 16 when MemEquals(iterator.Buffer!, 0, "NES\x1A"):
                HashEngine.IteratorVerbose(iterator, "Ignoring NES header");
                return UnheaderedIteratorBuffer(out hash, iterator, 16);
            case > 16 when MemEquals(iterator.Buffer!, 0, "FDS\x1A"):
                HashEngine.IteratorVerbose(iterator, "Ignoring FDS header");
                return UnheaderedIteratorBuffer(out hash, iterator, 16);
            default:
                return IteratorBuffer(out hash, iterator);
        }
    }

    /// <summary>Hashes a Nintendo 64 cartridge (byte-swap + 1 MiB cap).</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashN64(out string hash, RcHashIterator iterator)
    {
        const int bufferSize = 65536;
        var md5 = new HashMd5();
        var buffer = new byte[bufferSize];
        var isV64 = false;
        var isN64 = false;
        hash = "";

        var fileHandle = HashEngine.FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open file");

        /* read first byte so we can detect endianness */
        HashEngine.FileSeek(iterator, fileHandle, 0, HashEngine.SeekSet);
        HashEngine.FileRead(iterator, fileHandle, buffer, 1);

        switch (buffer[0])
        {
            /* z64 format (big endian [native]) */
            case 0x80:
                break;
            /* v64 format (byteswapped) */
            case 0x37:
                HashEngine.IteratorVerbose(iterator, "converting v64 to z64");
                isV64 = true;
                break;
            /* n64 format (little endian) */
            case 0x40:
                HashEngine.IteratorVerbose(iterator, "converting n64 to z64");
                isN64 = true;
                break;
            case 0xE8:
            /* ndd format (don't byteswap) */
            case 0x22:
                break;
            default:
                HashEngine.IteratorVerbose(iterator, "Not a Nintendo 64 ROM");
                return 0;
        }

        /* calculate total file size */
        HashEngine.FileSeek(iterator, fileHandle, 0, HashEngine.SeekEnd);
        var remaining = HashEngine.FileTell(iterator, fileHandle);
        if (remaining > HashEngine.MaxBufferSize)
        {
            remaining = HashEngine.MaxBufferSize;
        }

        HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} bytes", (uint)remaining);

        /* begin hashing */
        HashEngine.FileSeek(iterator, fileHandle, 0, HashEngine.SeekSet);
        while (remaining >= bufferSize)
        {
            HashEngine.FileRead(iterator, fileHandle, buffer, bufferSize);

            if (isV64)
                HashEngine.Byteswap16(buffer, bufferSize);
            else if (isN64)
                HashEngine.Byteswap32(buffer, bufferSize);

            md5.Append(buffer, bufferSize);
            remaining -= bufferSize;
        }

        if (remaining > 0)
        {
            HashEngine.FileRead(iterator, fileHandle, buffer, (int)remaining);

            if (isV64)
                HashEngine.Byteswap16(buffer, (int)remaining);
            else if (isN64)
                HashEngine.Byteswap32(buffer, (int)remaining);

            md5.Append(buffer, (int)remaining);
        }

        /* cleanup */
        HashEngine.FileClose(iterator, fileHandle);

        return HashEngine.Finalize(iterator, md5, out hash);
    }

    /// <summary>Hashes a Nintendo DS/DSi image (SuperCard variant supported).</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashNintendoDs(out string hash, RcHashIterator iterator)
    {
        var header = new byte[512];
        long offset = 0;
        hash = "";

        var fileHandle = HashEngine.FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open file");

        HashEngine.FileSeek(iterator, fileHandle, 0, HashEngine.SeekSet);
        if (HashEngine.FileRead(iterator, fileHandle, header, 512) != 512)
        {
            HashEngine.FileClose(iterator, fileHandle);
            return HashEngine.IteratorError(iterator, "Failed to read header");
        }

        if (header[0] == 0x2E && header[1] == 0x00 && header[2] == 0x00 && header[3] == 0xEA &&
            header[0xB0] == 0x44 && header[0xB1] == 0x46 && header[0xB2] == 0x96 && header[0xB3] == 0)
        {
            /* SuperCard header detected, ignore it */
            HashEngine.IteratorVerbose(iterator, "Ignoring SuperCard header");

            offset = 512;
            HashEngine.FileSeek(iterator, fileHandle, offset, HashEngine.SeekSet);
            HashEngine.FileRead(iterator, fileHandle, header, 512);
        }

        var arm9Addr = ReadLe32(header, 0x20);
        var arm9Size = ReadLe32(header, 0x2C);
        var arm7Addr = ReadLe32(header, 0x30);
        var arm7Size = ReadLe32(header, 0x3C);
        var iconAddr = ReadLe32(header, 0x68);

        if (arm9Size + arm7Size > 16 * 1024 * 1024)
        {
            /* sanity check - code blocks are typically less than 1MB each - assume not a DS ROM */
            HashEngine.FileClose(iterator, fileHandle);
            return HashEngine.IteratorErrorFormatted(iterator, "arm9 code size ({0}) + arm7 code size ({1}) exceeds 16MB", arm9Size, arm7Size);
        }

        uint hashSize = 0xA00;
        if (arm9Size > hashSize)
        {
            hashSize = arm9Size;
        }

        if (arm7Size > hashSize)
        {
            hashSize = arm7Size;
        }

        var hashBuffer = new byte[hashSize];
        var md5 = new HashMd5();

        HashEngine.IteratorVerbose(iterator, "Hashing 352 byte header");
        md5.Append(header, 0, 0x160);

        HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} byte arm9 code (at {1:X8})", arm9Size, arm9Addr);

        HashEngine.FileSeek(iterator, fileHandle, arm9Addr + offset, HashEngine.SeekSet);
        HashEngine.FileRead(iterator, fileHandle, hashBuffer, (int)arm9Size);
        md5.Append(hashBuffer, (int)arm9Size);

        HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} byte arm7 code (at {1:X8})", arm7Size, arm7Addr);

        HashEngine.FileSeek(iterator, fileHandle, arm7Addr + offset, HashEngine.SeekSet);
        HashEngine.FileRead(iterator, fileHandle, hashBuffer, (int)arm7Size);
        md5.Append(hashBuffer, (int)arm7Size);

        HashEngine.IteratorVerboseFormatted(iterator, "Hashing 2560 byte icon and labels data (at {0:X8})", iconAddr);

        HashEngine.FileSeek(iterator, fileHandle, iconAddr + offset, HashEngine.SeekSet);
        var numRead = HashEngine.FileRead(iterator, fileHandle, hashBuffer, 0xA00);
        if (numRead < 0xA00)
        {
            /* some homebrew games don't provide a full icon block, and no data after the icon block.
             * if we didn't get a full icon block, fill the remaining portion with 0s
             */
            HashEngine.IteratorVerboseFormatted(iterator,
                "Warning: only got {0} bytes for icon and labels data, 0-padding to 2560 bytes", (uint)numRead);

            Array.Clear(hashBuffer, numRead, 0xA00 - numRead);
        }

        md5.Append(hashBuffer, 0xA00);

        HashEngine.FileClose(iterator, fileHandle);

        return HashEngine.Finalize(iterator, md5, out hash);
    }

    /// <summary>Hashes a PC Engine HuCard.</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashPce(out string hash, RcHashIterator iterator)
    {
        /* The PCE header doesn't bear any distinguishable marks, so we have to detect
         * it by looking at the file size. The core looks for anything that's 512 bytes
         * more than a multiple of 8KB, so we'll do that too.
         * https://github.com/libretro/beetle-pce-libretro/blob/af28fb0385d00e0292c4703b3aa7e72762b564d2/mednafen/pce/huc.cpp#L196-L202
         */
        if ((iterator.BufferSize & 512) != 0)
        {
            HashEngine.IteratorVerbose(iterator, "Ignoring PCE header");
            return UnheaderedIteratorBuffer(out hash, iterator, 512);
        }

        return IteratorBuffer(out hash, iterator);
    }

    /// <summary>Hashes a Super Cassette Vision cartridge (32-byte header stripped).</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashScv(out string hash, RcHashIterator iterator)
    {
        /* if the file contains a header, ignore it */
        /* https://gitlab.com/MaaaX-EmuSCV/libretro-emuscv/-/blob/master/readme.txt#L211 */
        if (MemEquals(iterator.Buffer!, 0, "EmuSCV"))
        {
            HashEngine.IteratorVerbose(iterator, "Ignoring SCV header");
            return UnheaderedIteratorBuffer(out hash, iterator, 32);
        }

        return IteratorBuffer(out hash, iterator);
    }

    /// <summary>Hashes the image for the console.</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashSnes(out string hash, RcHashIterator iterator)
    {
        /* if the file contains a header, ignore it */
        var calcSize = ((long)iterator.BufferSize / 0x2000) * 0x2000;
        if (iterator.BufferSize - calcSize == 512)
        {
            HashEngine.IteratorVerbose(iterator, "Ignoring SNES header");
            return UnheaderedIteratorBuffer(out hash, iterator, 512);
        }

        return IteratorBuffer(out hash, iterator);
    }
}
