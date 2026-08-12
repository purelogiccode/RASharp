// Ported from rcheevos (MIT) — test/rhash/data.c (Phase 3 subset)
// Synthetic disc image generators: GameCube, 3DO, Dreamcast, PCE-CD, PC-FX,
// ISO9660 (PSX), Jaguar CD, and the 2048->2352 converter.

using System.Text;

namespace RASharp.Tests;

/// <summary>Ported from rcheevos (MIT) — test/rhash/data.c (Phase 3 subset) Synthetic disc image generators: GameCube, 3DO, Dreamcast, PCE-CD, PC-FX, ISO9660 (PSX), Jaguar </summary>
public static class TestDataGenDisc
{
    private static void CopyString(byte[] image, int offset, string text, int length)
    {
        for (var i = 0; i < length && i < text.Length; ++i)
        {
            image[offset + i] = (byte)text[i];
        }
    }

    /* memcmp(&image[offset], text, length) — case sensitive */
    private static bool Matches(byte[] image, int offset, string text, int length)
    {
        for (var i = 0; i < length; ++i)
        {
            if (image[offset + i] != (byte)text[i])
                return false;
        }

        return true;
    }

    /* generate_gamecube_iso from data.c */
    /// <summary>generate_gamecube_iso from data.c</summary>
    /// <param name="mb">the mb parameter</param>
    /// <param name="imageSize">the image size parameter</param>
    /// <returns>the result</returns>
    public static byte[] GenerateGamecubeIso(int mb, out int imageSize)
    {
        var sizeNeeded = mb * 1024 * 1024;
        const uint apploaderSizesAddr = 0x2440 + 0x14;
        const uint dolOffsetAddr = 0x420;
        const uint dolSizesAddr = 0x3000;

        var image = new byte[sizeNeeded];
        TestDataGen.FillImage(image, 0, sizeNeeded);

        image[0x1c] = 0xC2;
        image[0x1d] = 0x33;
        image[0x1e] = 0x9F;
        image[0x1f] = 0x3D;

        for (var ix = 0; ix < 8; ix++)
        {
            /* 0x000000ff for both */
            image[apploaderSizesAddr + ix] = (ix % 4 == 3) ? (byte)0xff : (byte)0;
        }

        for (var ix = 0; ix < 4; ix++)
        {
            /* 0x00003000 */
            image[dolOffsetAddr + ix] = (ix % 4 == 2) ? (byte)0x30 : (byte)0;
        }

        for (var ix = 0; ix < 18 * 4; ix++)
        {
            /* offsets start at 0x00003100 and increment */
            image[dolSizesAddr + ix] = (ix % 4 == 2) ? (byte)(0x30 + 1 + ix / 4) : (byte)0;
            /* 0x000000ff for every other size */
            image[dolSizesAddr + 0x90 + ix] = (ix % 8 == 3) ? (byte)0xff : (byte)0;
        }

        imageSize = sizeNeeded;
        return image;
    }

    /* generate_3do_bin from data.c */
    /// <summary>generate_3do_bin from data.c</summary>
    /// <param name="rootDirectorySectors">the root directory sectors parameter</param>
    /// <param name="binarySize">the binary size parameter</param>
    /// <param name="imageSize">the image size parameter</param>
    /// <returns>the result</returns>
    public static byte[] Generate3DoBin(uint rootDirectorySectors, uint binarySize, out int imageSize)
    {
        byte[] volumeHeader =
        [
            0x01, 0x5A, 0x5A, 0x5A, 0x5A, 0x5A, 0x01, 0x00, /* header */
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, /* comment */
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            (byte)'C', (byte)'D', (byte)'-', (byte)'R', (byte)'O', (byte)'M', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, /* label */
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0x2D, 0x79, 0x6E, 0x96, /* identifier */
            0x00, 0x00, 0x08, 0x00, /* block size */
            0x00, 0x00, 0x05, 0x00, /* block count */
            0x31, 0x5a, 0xf2, 0xe6, /* root directory identifier */
            0x00, 0x00, 0x00, 0x01, /* root directory size in blocks */
            0x00, 0x00, 0x08, 0x00, /* block size in root directory */
            0x00, 0x00, 0x00, 0x06, /* number of copies of root directory */
            0x00, 0x00, 0x00, 0x01, /* block location of root directory */
            0x00, 0x00, 0x00, 0x01, /* block location of first copy of root directory */
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x01 /* block location of last copy of root directory */
        ];

        byte[] directoryData =
        [
            0xFF, 0xFF, 0xFF, 0xFF, /* next block */
            0xFF, 0xFF, 0xFF, 0xFF, /* previous block */
            0x00, 0x00, 0x00, 0x00, /* flags */
            0x00, 0x00, 0x00, 0xA4, /* end of block */
            0x00, 0x00, 0x00, 0x14, /* start of block */

            0x00, 0x00, 0x00, 0x07, /* flags - directory */
            0x00, 0x00, 0x00, 0x00, /* identifier */
            0x00, 0x00, 0x00, 0x00, /* type */
            0x00, 0x00, 0x08, 0x00, /* block size */
            0x00, 0x00, 0x00, 0x00, /* length in bytes */
            0x00, 0x00, 0x00, 0x00, /* length in blocks */
            0x00, 0x00, 0x00, 0x00, /* burst */
            0x00, 0x00, 0x00, 0x00, /* gap */
            (byte)'f', (byte)'o', (byte)'l', (byte)'d', (byte)'e', (byte)'r', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, /* filename */
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0x00, 0x00, 0x00, 0x00, /* extra copies */
            0x00, 0x00, 0x00, 0x00, /* directory block address */

            0x00, 0x00, 0x00, 0x02, /* flags - file */
            0x00, 0x00, 0x00, 0x00, /* identifier */
            0x00, 0x00, 0x00, 0x00, /* type */
            0x00, 0x00, 0x08, 0x00, /* block size */
            0x00, 0x00, 0x00, 0x00, /* length in bytes */
            0x00, 0x00, 0x00, 0x00, /* length in blocks */
            0x00, 0x00, 0x00, 0x00, /* burst */
            0x00, 0x00, 0x00, 0x00, /* gap */
            (byte)'L', (byte)'a', (byte)'u', (byte)'n', (byte)'c', (byte)'h', (byte)'M', (byte)'e', 0, 0, 0, 0, 0, 0, 0, 0, /* filename */
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0x00, 0x00, 0x00, 0x00, /* extra copies */
            0x00, 0x00, 0x00, 0x02 /* directory block address */
        ];

        var sizeNeeded = (int)((rootDirectorySectors + 1 + ((binarySize + 2047) / 2048)) * 2048);
        var image = new byte[sizeNeeded];
        var offset = 2048;
        uint i;

        /* first sector - volume header */
        Array.Copy(volumeHeader, image, volumeHeader.Length);
        image[0x5B] = (byte)rootDirectorySectors;

        /* root directory sectors */
        for (i = 0; i < rootDirectorySectors; ++i)
        {
            Array.Copy(directoryData, 0, image, offset, directoryData.Length);
            if (i < rootDirectorySectors - 1)
            {
                image[offset + 0] = 0;
                image[offset + 1] = 0;
                image[offset + 2] = 0;
                image[offset + 3] = (byte)(i + 1);

                CopyString(image, offset + 0x14 + 0x48 + 0x20, "filename", 8);
            }
            else
            {
                image[offset + 0x14 + 0x48 + 0x11] = (byte)((binarySize >> 16) & 0xFF);
                image[offset + 0x14 + 0x48 + 0x12] = (byte)((binarySize >> 8) & 0xFF);
                image[offset + 0x14 + 0x48 + 0x13] = (byte)(binarySize & 0xFF);

                image[offset + 0x14 + 0x48 + 0x16] = (byte)((((binarySize + 2047) / 2048) >> 8) & 0xFF);
                image[offset + 0x14 + 0x48 + 0x17] = (byte)(((binarySize + 2047) / 2048) & 0xFF);

                image[offset + 0x14 + 0x48 + 0x47] = (byte)(i + 2);
            }

            if (i > 0)
            {
                image[offset + 4] = 0;
                image[offset + 5] = 0;
                image[offset + 6] = 0;
                image[offset + 7] = (byte)(i - 1);
            }

            offset += 2048;
        }

        /* binary data */
        TestDataGen.FillImage(image, offset, (int)binarySize);

        imageSize = sizeNeeded;
        return image;
    }

    /* generate_dreamcast_bin from data.c */
    /// <summary>generate_dreamcast_bin from data.c</summary>
    /// <param name="trackFirstSector">the track first sector parameter</param>
    /// <param name="binarySize">the binary size parameter</param>
    /// <param name="imageSize">the image size parameter</param>
    /// <returns>the result</returns>
    public static byte[] GenerateDreamcastBin(uint trackFirstSector, uint binarySize, out int imageSize)
    {
        /* https://mc.pp.se/dc/ip0000.bin.html */
        const string volumeHeader = "SEGA SEGAKATANA " +
                                    "SEGA ENTERPRISES" +
                                    "5966 GD-ROM1/1  " + /* device info */
                                    " U      918FA01 " + /* region and peripherals */
                                    "X-1234N   V1.001" + /* product number and version */
                                    "20200910        " + /* release date */
                                    "1ST_READ.BIN    " + /* boot file */
                                    "RETROACHIEVEMENT" + /* company name */
                                    "UNIT TEST       " + /* product name */
                                    "                " +
                                    "                " +
                                    "                " +
                                    "                " +
                                    "                " +
                                    "                " +
                                    "                ";

        byte[] directoryData =
        [
            0x30, /* length of directory record */
            0x00, /* extended attribute record length */
            0xD9, 0xAF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /* first logical block of file */
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /* length in bytes */
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /* date/time */
            0x00, 0x00, 0x00, /* flags, unit size, gap size */
            0x00, 0x00, 0x00, 0x00, /* sequence number */
            0x0E, /* length of file identifier */
            (byte)'1', (byte)'S', (byte)'T', (byte)'_', (byte)'R', (byte)'E', (byte)'A', (byte)'D', (byte)'.', (byte)'B', (byte)'I', (byte)'N', (byte)';', (byte)'1' /* file identifier */
        ];

        var binarySectors = (binarySize + 2047) / 2048;
        var sizeNeeded = (int)((binarySectors + 18) * 2048);
        var image = new byte[sizeNeeded];

        /* volume header goes in sector 0 */
        CopyString(image, 0, volumeHeader, 256);

        /* directory information goes in sectors 16 and 17 */
        CopyString(image, 2048 * 16, "1CD001", 6);
        image[2048 * 16 + 156 + 2] = 45017 & 0xFF;
        image[2048 * 16 + 156 + 3] = (45017 >> 8) & 0xFF;
        image[2048 * 16 + 156 + 4] = (45017 >> 16) & 0xFF;
        Array.Copy(directoryData, 0, image, 2048 * 17, directoryData.Length);

        trackFirstSector += 18;
        image[2048 * 17 + 2] = (byte)(trackFirstSector & 0xFF);
        image[2048 * 17 + 3] = (byte)((trackFirstSector >> 8) & 0xFF);
        image[2048 * 17 + 4] = (byte)((trackFirstSector >> 16) & 0xFF);
        image[2048 * 17 + 10] = (byte)(binarySize & 0xFF);
        image[2048 * 17 + 11] = (byte)((binarySize >> 8) & 0xFF);
        image[2048 * 17 + 12] = (byte)((binarySize >> 16) & 0xFF);
        image[2048 * 17 + 13] = (byte)((binarySize >> 24) & 0xFF);

        /* binary data */
        TestDataGen.FillImage(image, 2048 * 18, (int)(binarySectors * 2048));

        imageSize = sizeNeeded;
        return image;
    }

    /* generate_pce_cd_bin from data.c */
    /// <summary>generate_pce_cd_bin from data.c</summary>
    /// <param name="binarySectors">the binary sectors parameter</param>
    /// <param name="imageSize">the image size parameter</param>
    /// <returns>the result</returns>
    public static byte[] GeneratePceCdBin(uint binarySectors, out int imageSize)
    {
        byte[] volumeHeader =
        [
            0x00, 0x00, 0x02, /* first sector of boot code */
            0x14, /* number of sectors for boot code */
            0x00, 0x40, /* program load address */
            0x00, 0x40, /* program execute address  */
            0, 1, 2, 3, 4, /* IPLMPR */
            0, /* open mode */
            0, 0, 0, 0, 0, 0, /* GRPBLK and addr */
            0, 0, 0, 0, 0, /* ADPBLK and rate */
            0, 0, 0, 0, 0, 0, 0, /* reserved */
            (byte)'P', (byte)'C', (byte)' ', (byte)'E', (byte)'n', (byte)'g', (byte)'i', (byte)'n', (byte)'e', (byte)' ', (byte)'C', (byte)'D', (byte)'-', (byte)'R', (byte)'O', (byte)'M',
            (byte)' ', (byte)'S', (byte)'Y', (byte)'S', (byte)'T', (byte)'E', (byte)'M', 0, (byte)'C', (byte)'o', (byte)'p', (byte)'y', (byte)'r', (byte)'i', (byte)'g', (byte)'h',
            (byte)'t', (byte)' ', (byte)'H', (byte)'U', (byte)'D', (byte)'S', (byte)'O', (byte)'N', (byte)' ', (byte)'S', (byte)'O', (byte)'F', (byte)'T', (byte)' ', (byte)'/', (byte)' ',
            (byte)'N', (byte)'E', (byte)'C', (byte)' ', (byte)'H', (byte)'o', (byte)'m', (byte)'e', (byte)' ', (byte)'E', (byte)'l', (byte)'e', (byte)'c', (byte)'t', (byte)'r', (byte)'o',
            (byte)'n', (byte)'i', (byte)'c', (byte)'s', (byte)',', (byte)'L', (byte)'t', (byte)'d', (byte)'.', 0, (byte)'G', (byte)'A', (byte)'M', (byte)'E', (byte)'N', (byte)'A',
            (byte)'M', (byte)'E', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' ', (byte)' '
        ];

        var sizeNeeded = (int)((binarySectors + 2) * 2048);
        var image = new byte[sizeNeeded];

        /* volume header goes in second sector */
        Array.Copy(volumeHeader, 0, image, 2048, volumeHeader.Length);
        image[2048 + 0x03] = (byte)binarySectors;

        /* binary data */
        TestDataGen.FillImage(image, 4096, (int)(binarySectors * 2048));

        imageSize = sizeNeeded;
        return image;
    }

    /* generate_pcfx_bin from data.c */
    /// <summary>generate_pcfx_bin from data.c</summary>
    /// <param name="binarySectors">the binary sectors parameter</param>
    /// <param name="imageSize">the image size parameter</param>
    /// <returns>the result</returns>
    public static byte[] GeneratePcfxBin(uint binarySectors, out int imageSize)
    {
        byte[] volumeHeader =
        [
            (byte)'G', (byte)'A', (byte)'M', (byte)'E', (byte)'N', (byte)'A', (byte)'M', (byte)'E', 0, 0, 0, 0, 0, 0, 0, 0, /* title (32-bytes) */
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0x02, 0x00, 0x00, 0x00, /* first sector of boot code */
            0x14, 0x00, 0x00, 0x00, /* number of sectors for boot code */
            0x00, 0x80, 0x00, 0x00, /* program load address */
            0x00, 0x80, 0x00, 0x00, /* program execute address  */
            (byte)'N', (byte)'/', (byte)'A', 0, /* maker id */
            (byte)'r', (byte)'c', (byte)'h', (byte)'e', (byte)'e', (byte)'v', (byte)'o', (byte)'s', (byte)'t', (byte)'e', (byte)'s', (byte)'t', 0, 0, 0, 0, /* maker name (60-bytes) */
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0x00, 0x00, 0x00, 0x00, /* volume number */
            0x00, 0x01, /* version */
            0x01, 0x00, /* country */
            (byte)'2', (byte)'0', (byte)'2', (byte)'0', (byte)'X', (byte)'X', (byte)'X', (byte)'X' /* date */
        ];

        var sizeNeeded = (int)((binarySectors + 2) * 2048);
        var image = new byte[sizeNeeded];

        /* volume header goes in second sector */
        CopyString(image, 0, "PC-FX:Hu_CD-ROM", 15);
        Array.Copy(volumeHeader, 0, image, 2048, volumeHeader.Length);
        image[2048 + 0x24] = (byte)binarySectors;

        /* binary data */
        TestDataGen.FillImage(image, 4096, (int)(binarySectors * 2048));

        imageSize = sizeNeeded;
        return image;
    }

    /* generate_iso9660_bin from data.c */
    /// <summary>generate_iso9660_bin from data.c</summary>
    /// <param name="numSectors">the num sectors parameter</param>
    /// <param name="volumeLabel">the volume label parameter</param>
    /// <param name="imageSize">the image size parameter</param>
    /// <returns>the result</returns>
    public static byte[] GenerateIso9660Bin(uint numSectors, string volumeLabel, out int imageSize)
    {
        byte[] identifier = [0x01, (byte)'C', (byte)'D', (byte)'0', (byte)'0', (byte)'1', 0x01, 0x00];

        imageSize = (int)(numSectors * 2048);
        var image = new byte[imageSize];

        const int volumeDescriptor = 16 * 2048;

        /* CD001 identifier */
        Array.Copy(identifier, 0, image, volumeDescriptor, identifier.Length);

        /* volume label */
        CopyString(image, volumeDescriptor + 40, volumeLabel, volumeLabel.Length);

        /* number of sectors (little endian, then big endian) */
        image[volumeDescriptor + 80] = image[87] = (byte)(numSectors & 0xFF);
        image[volumeDescriptor + 81] = image[86] = (byte)((numSectors >> 8) & 0xFF);
        image[volumeDescriptor + 82] = image[85] = (byte)((numSectors >> 16) & 0xFF);
        image[volumeDescriptor + 83] = image[84] = (byte)((numSectors >> 24) & 0xFF);

        /* size of each sector */
        image[volumeDescriptor + 128] = 2048 & 0xFF;
        image[volumeDescriptor + 129] = (2048 >> 8) & 0xFF;

        /* root directory record location */
        image[volumeDescriptor + 158] = 17;

        /* helper for tracking next free sector - not actually part of iso9660 spec */
        image[17 * 2048 - 4] = 18;

        return image;
    }

    /* generate_iso9660_file from data.c — returns the contents start offset */
    /// <summary>generate_iso9660_file from data.c — returns the contents start offset</summary>
    /// <param name="image">the image parameter</param>
    /// <param name="filename">the filename parameter</param>
    /// <param name="contents">the contents parameter</param>
    /// <param name="contentsSize">the contents size parameter</param>
    /// <returns>the result</returns>
    public static int GenerateIso9660File(byte[] image, string filename, byte[]? contents, int contentsSize)
    {
        const int rootDirectoryRecordOffset = 17 * 2048;
        var fileEntryStart = rootDirectoryRecordOffset;
        int filenameLen;
        var nextFreeSector = image[rootDirectoryRecordOffset - 4] |
                             (image[rootDirectoryRecordOffset - 3] << 8) | (image[rootDirectoryRecordOffset - 2] << 16);

        /* we start at the root. ignore explicit root path */
        if (filename.Length > 0 && filename[0] == '\\')
        {
            filename = filename.Substring(1);
        }

        /* handle subdirectories */
        do
        {
            var separator = filename.IndexOf('\\');
            if (separator < 0)
                break;

            filenameLen = separator;
            var found = 0;
            while (image[fileEntryStart] != 0)
            {
                if (image[fileEntryStart + 25] != 0 && /* is directory */
                    image[fileEntryStart + 33 + filenameLen] == 0 && Matches(image, fileEntryStart + 33, filename, filenameLen))
                {
                    int directorySector = image[fileEntryStart + 2];
                    fileEntryStart = directorySector * 2048;
                    found = 1;
                    break;
                }

                fileEntryStart += image[fileEntryStart];
            }

            if (found == 0)
            {
                /* entry size */
                image[fileEntryStart] = (byte)((filenameLen & 0xFF) + 48);

                /* directory sector */
                image[fileEntryStart + 2] = (byte)(nextFreeSector & 0xFF);
                image[fileEntryStart + 3] = (byte)((nextFreeSector >> 8) & 0xFF);

                /* is directory */
                image[fileEntryStart + 25] = 1;

                /* directory name */
                image[fileEntryStart + 32] = (byte)(filenameLen & 0xFF);
                CopyString(image, fileEntryStart + 33, filename, filenameLen);
                image[fileEntryStart + 33 + filenameLen] = 0;

                /* advance to next sector */
                fileEntryStart = nextFreeSector * 2048;
                nextFreeSector++;
            }

            filename = filename.Substring(separator + 1);
        } while (true);

        /* skip over any items already in the directory */
        while (image[fileEntryStart] != 0)
        {
            fileEntryStart += image[fileEntryStart];
        }

        /* create the new entry */

        /* entry size */
        filenameLen = filename.Length;
        image[fileEntryStart] = (byte)((filenameLen & 0xFF) + 48);

        /* file sector */
        image[fileEntryStart + 2] = (byte)(nextFreeSector & 0xFF);
        image[fileEntryStart + 3] = (byte)((nextFreeSector >> 8) & 0xFF);

        /* file size */
        image[fileEntryStart + 10] = (byte)(contentsSize & 0xFF);
        image[fileEntryStart + 11] = (byte)((contentsSize >> 8) & 0xFF);
        image[fileEntryStart + 12] = (byte)((contentsSize >> 16) & 0xFF);

        /* file name */
        image[fileEntryStart + 32] = (byte)((filenameLen + 2) & 0xFF);
        CopyString(image, fileEntryStart + 33, filename, filenameLen);
        image[fileEntryStart + 33 + filenameLen] = (byte)';';
        image[fileEntryStart + 34 + filenameLen] = (byte)'1';

        /* contents */
        var fileContentsStart = nextFreeSector * 2048;

        if (contents != null)
            Array.Copy(contents, 0, image, fileContentsStart, contentsSize);
        else
            TestDataGen.FillImage(image, fileContentsStart, contentsSize);

        /* update next free sector */
        nextFreeSector += (contentsSize + 2047) / 2048;
        image[rootDirectoryRecordOffset - 4] = (byte)(nextFreeSector & 0xFF);
        image[rootDirectoryRecordOffset - 3] = (byte)((nextFreeSector >> 8) & 0xFF);
        image[rootDirectoryRecordOffset - 2] = (byte)((nextFreeSector >> 16) & 0xFF);

        /* return pointer to contents so caller can modify if desired */
        return fileContentsStart;
    }

    /* generate_jaguarcd_bin from data.c */
    /// <summary>generate_jaguarcd_bin from data.c</summary>
    /// <param name="headerOffset">the header offset parameter</param>
    /// <param name="binarySize">the binary size parameter</param>
    /// <param name="byteswapped">the byteswapped parameter</param>
    /// <param name="imageSize">the image size parameter</param>
    /// <returns>the result</returns>
    public static byte[] GenerateJaguarcdBin(uint headerOffset, uint binarySize, int byteswapped, out int imageSize)
    {
        var sizeNeeded = (int)((((binarySize + 64 + 32 + 8) + 2351) / 2352) * 2352);
        var image = new byte[sizeNeeded];
        uint i;

        /* header is 64 bytes of ATRI repeating followed by approved data message, load address, and binary size */
        for (i = 0; i < 64; i += 4)
            CopyString(image, (int)(headerOffset + i), "ATRI", 4);
        CopyString(image, (int)(headerOffset + 64), "ATARI APPROVED DATA HEADER ATRI ", 32);
        image[headerOffset + 64 + 32 + 2] = 0xA0;
        image[headerOffset + 64 + 32 + 4 + 1] = (byte)((binarySize >> 16) & 0xFF);
        image[headerOffset + 64 + 32 + 4 + 2] = (byte)((binarySize >> 8) & 0xFF);
        image[headerOffset + 64 + 32 + 4 + 3] = (byte)(binarySize & 0xFF);

        /* binary data */
        TestDataGen.FillImage(image, (int)(headerOffset + 64 + 32 + 8), sizeNeeded - (int)(headerOffset + 64 + 32 + 8));

        if (byteswapped != 0)
        {
            for (i = 0; i < sizeNeeded; i += 2)
            {
                (image[i], image[i + 1]) = (image[i + 1], image[i]);
            }
        }

        imageSize = sizeNeeded;
        return image;
    }

    /* generate_psx_bin from data.c */
    /// <summary>generate_psx_bin from data.c</summary>
    /// <param name="binaryName">the binary name parameter</param>
    /// <param name="binarySize">the binary size parameter</param>
    /// <param name="imageSize">the image size parameter</param>
    /// <returns>the result</returns>
    public static byte[] GeneratePsxBin(string binaryName, uint binarySize, out int imageSize)
    {
        var sectorsNeeded = (((binarySize + 2047) / 2048) + 20);
        var systemCnf = $"BOOT=cdrom:\\{binaryName};1\nTCB=4\nEVENT=10\nSTACK=801FFFF0\n";
        var cnfBytes = Encoding.ASCII.GetBytes(systemCnf);

        var image = GenerateIso9660Bin(sectorsNeeded, "TEST", out imageSize);
        GenerateIso9660File(image, "SYSTEM.CNF", cnfBytes, cnfBytes.Length);

        /* binary data */
        var exe = GenerateIso9660File(image, binaryName, null, (int)binarySize);
        CopyString(image, exe, "PS-X EXE", 8);

        binarySize -= 2048;
        image[exe + 28] = (byte)(binarySize & 0xFF);
        image[exe + 29] = (byte)((binarySize >> 8) & 0xFF);
        image[exe + 30] = (byte)((binarySize >> 16) & 0xFF);
        image[exe + 31] = (byte)((binarySize >> 24) & 0xFF);

        return image;
    }

    /* generate_ps2_bin from data.c */
    /// <summary>generate_ps2_bin from data.c</summary>
    /// <param name="binaryName">the binary name parameter</param>
    /// <param name="binarySize">the binary size parameter</param>
    /// <param name="imageSize">the image size parameter</param>
    /// <returns>the result</returns>
    public static byte[] GeneratePs2Bin(string binaryName, uint binarySize, out int imageSize)
    {
        var sectorsNeeded = ((binarySize + 2047) / 2048) + 20;
        var systemCnf = $"BOOT2 = cdrom0:\\{binaryName};1\nVER = 1.0\nVMODE = NTSC\n";
        var cnfBytes = Encoding.ASCII.GetBytes(systemCnf);

        var image = GenerateIso9660Bin(sectorsNeeded, "TEST", out imageSize);
        GenerateIso9660File(image, "SYSTEM.CNF", cnfBytes, cnfBytes.Length);

        /* binary data */
        var exe = GenerateIso9660File(image, binaryName, null, (int)binarySize);
        image[exe + 0] = 0x7F;
        image[exe + 1] = (byte)'E';
        image[exe + 2] = (byte)'L';
        image[exe + 3] = (byte)'F';

        return image;
    }

    /* convert_to_2352 from data.c */
    /// <summary>convert_to_2352 from data.c</summary>
    /// <param name="input">the input parameter</param>
    /// <param name="size">the size</param>
    /// <param name="firstSector">the first sector parameter</param>
    /// <returns>the result</returns>
    public static byte[] ConvertTo2352(byte[] input, ref int size, uint firstSector)
    {
        var numSectors = (uint)((size + 2047) / 2048);
        var outputSize = (int)(numSectors * 2352);
        byte[] syncPattern =
        [
            0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00
        ];
        var output = new byte[outputSize];
        var inputPtr = 0;
        var ptr = 0;
        uint i;

        firstSector += 150;
        var frames = (byte)(firstSector % 75);
        firstSector /= 75;
        var seconds = (byte)(firstSector % 60);
        var minutes = (byte)(firstSector / 60);

        for (i = 0; i < numSectors; i++)
        {
            /* 16 - byte sync header */
            Array.Copy(syncPattern, 0, output, ptr, 12);
            ptr += 12;
            output[ptr++] = (byte)(((minutes / 10) << 4) | (minutes % 10));
            output[ptr++] = (byte)(((seconds / 10) << 4) | (seconds % 10));
            output[ptr++] = (byte)(((frames / 10) << 4) | (frames % 10));
            if (++frames == 75)
            {
                frames = 0;
                if (++seconds == 60)
                {
                    seconds = 0;
                    ++minutes;
                }
            }

            output[ptr++] = 2;

            /* 2048 bytes data */
            Array.Copy(input, inputPtr, output, ptr, 2048);
            inputPtr += 2048;

            /* 288 bytes parity / checksums */
            ptr += 2352 - 16;
        }

        size = outputSize;
        return output;
    }
}
