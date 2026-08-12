// Ported from rcheevos (MIT) — test/rhash/data.c + test/rhash/test_hash_rom.c
// Synthetic cartridge image generators (Phase 2 subset).

namespace RASharp.Tests;

/// <summary>Ported from rcheevos (MIT) — test/rhash/data.c + test/rhash/test_hash_rom.c Synthetic cartridge image generators (Phase 2 subset).</summary>
public static class TestDataGen
{
    /* fill_image + generate_generic_file from data.c */
/// <summary>fill_image + generate_generic_file from data.c</summary>
/// <param name="size">the size</param>
/// <returns>the result</returns>
    public static byte[] GenerateGenericFile(int size)
    {
        byte[] image = new byte[size];
        FillImage(image, 0, size);
        return image;
    }

    /* fill a sub-range of the image (offset-based fill_image) */
/// <summary>fill a sub-range of the image (offset-based fill_image)</summary>
/// <param name="kb">the kb parameter</param>
/// <param name="withHeader">the with header parameter</param>
/// <param name="imageSize">the image size parameter</param>
/// <returns>the result</returns>
    public static byte[] GenerateAtari7800File(int kb, bool withHeader, out int imageSize)
    {
        int sizeNeeded = kb * 1024;
        if (withHeader)
            sizeNeeded += 128;

        byte[] image = new byte[sizeNeeded];
        if (withHeader)
        {
            byte[] header = new byte[]
            {
                3, (byte)'A', (byte)'T', (byte)'A', (byte)'R', (byte)'I', (byte)'7', (byte)'8', (byte)'0', (byte)'0', 0, 0, 0, 0, 0, 0, /* version + magic text */
                0, (byte)'G', (byte)'a', (byte)'m', (byte)'e', (byte)'N', (byte)'a', (byte)'m', (byte)'e', 0, 0, 0, 0, 0, 0, 0,   /* game name */
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                   /* game name (cont'd) */
                0, 0, 2, 0, 0, 0, 3, 1, 1, 0, 0, 0, 0, 0, 0, 0,                   /* attributes */
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                   /* unused */
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,                   /* unused */
                0, 0, 0, 0, (byte)'A', (byte)'C', (byte)'T', (byte)'U', (byte)'A', (byte)'L', (byte)' ', (byte)'C', (byte)'A', (byte)'R', (byte)'T', /* magic text*/
                (byte)'D', (byte)'A', (byte)'T', (byte)'A', (byte)' ', (byte)'S', (byte)'T', (byte)'A', (byte)'R', (byte)'T', (byte)'S', (byte)' ', (byte)'H', (byte)'E', (byte)'R', (byte)'E' /* magic text */
            };
            Array.Copy(header, image, header.Length);
            image[50] = (byte)(kb / 4); /* 4-byte value starting at address 49 is the ROM size without header */

            FillImage(image, 128, sizeNeeded - 128);
        }
        else
        {
            FillImage(image, 0, sizeNeeded);
        }

        imageSize = sizeNeeded;
        return image;
    }

    /* generate_nes_file from test_hash_rom.c */
/// <summary>generate_nes_file from test_hash_rom.c</summary>
/// <param name="kb">the kb parameter</param>
/// <param name="withHeader">the with header parameter</param>
/// <param name="imageSize">the image size parameter</param>
/// <returns>the result</returns>
    public static byte[] GenerateNesFile(int kb, bool withHeader, out int imageSize)
    {
        int sizeNeeded = kb * 1024;
        if (withHeader)
            sizeNeeded += 16;

        byte[] image = new byte[sizeNeeded];
        if (withHeader)
        {
            image[0] = (byte)'N';
            image[1] = (byte)'E';
            image[2] = (byte)'S';
            image[3] = 0x1A;
            image[4] = (byte)(kb / 16);

            FillImage(image, 16, sizeNeeded - 16);
        }
        else
        {
            FillImage(image, 0, sizeNeeded);
        }

        imageSize = sizeNeeded;
        return image;
    }

    /* generate_fds_file from test_hash_rom.c */
/// <summary>generate_fds_file from test_hash_rom.c</summary>
/// <param name="sides">the sides parameter</param>
/// <param name="withHeader">the with header parameter</param>
/// <param name="imageSize">the image size parameter</param>
/// <returns>the result</returns>
    public static byte[] GenerateFdsFile(int sides, bool withHeader, out int imageSize)
    {
        int sizeNeeded = sides * 65500;
        if (withHeader)
            sizeNeeded += 16;

        byte[] image = new byte[sizeNeeded];
        if (withHeader)
        {
            image[0] = (byte)'F';
            image[1] = (byte)'D';
            image[2] = (byte)'S';
            image[3] = 0x1A;
            image[4] = (byte)sides;

            FillImage(image, 16, sizeNeeded - 16);
        }
        else
        {
            FillImage(image, 0, sizeNeeded);
        }

        imageSize = sizeNeeded;
        return image;
    }

    /* generate_nds_file from test_hash_rom.c */
/// <summary>generate_nds_file from test_hash_rom.c</summary>
/// <param name="mb">the mb parameter</param>
/// <param name="arm9Size">the arm9 size parameter</param>
/// <param name="arm7Size">the arm7 size parameter</param>
/// <param name="imageSize">the image size parameter</param>
/// <returns>the result</returns>
    public static byte[] GenerateNdsFile(int mb, uint arm9Size, uint arm7Size, out int imageSize)
    {
        int sizeNeeded = mb * 1024 * 1024;

        byte[] image = new byte[sizeNeeded];
        uint arm9Addr = 65536;
        uint arm7Addr = arm9Addr + arm9Size;
        uint iconAddr = arm7Addr + arm7Size;

        FillImage(image, 0, sizeNeeded);

        image[0x20] = (byte)(arm9Addr & 0xFF);
        image[0x21] = (byte)((arm9Addr >> 8) & 0xFF);
        image[0x22] = (byte)((arm9Addr >> 16) & 0xFF);
        image[0x23] = (byte)((arm9Addr >> 24) & 0xFF);
        image[0x2C] = (byte)(arm9Size & 0xFF);
        image[0x2D] = (byte)((arm9Size >> 8) & 0xFF);
        image[0x2E] = (byte)((arm9Size >> 16) & 0xFF);
        image[0x2F] = (byte)((arm9Size >> 24) & 0xFF);

        image[0x30] = (byte)(arm7Addr & 0xFF);
        image[0x31] = (byte)((arm7Addr >> 8) & 0xFF);
        image[0x32] = (byte)((arm7Addr >> 16) & 0xFF);
        image[0x33] = (byte)((arm7Addr >> 24) & 0xFF);
        image[0x3C] = (byte)(arm7Size & 0xFF);
        image[0x3D] = (byte)((arm7Size >> 8) & 0xFF);
        image[0x3E] = (byte)((arm7Size >> 16) & 0xFF);
        image[0x3F] = (byte)((arm7Size >> 24) & 0xFF);

        image[0x68] = (byte)(iconAddr & 0xFF);
        image[0x69] = (byte)((iconAddr >> 8) & 0xFF);
        image[0x6A] = (byte)((iconAddr >> 16) & 0xFF);
        image[0x6B] = (byte)((iconAddr >> 24) & 0xFF);

        imageSize = sizeNeeded;
        return image;
    }

    /* fill a sub-range of the image (offset-based fill_image) */
/// <summary>fill a sub-range of the image (offset-based fill_image)</summary>
/// <param name="image">the image parameter</param>
/// <param name="start">the start parameter</param>
/// <param name="size">the size</param>
    internal static void FillImage(byte[] image, int start, int size)
    {
        int seed = unchecked((int)(((ulong)size ^ ((ulong)size >> 8) ^ ((ulong)(size - 1) * 25387)) & 0xFFFFFFFF));
        int count;
        byte value;
        int remaining = size;
        int pos = start;

        while (remaining > 0)
        {
            switch (seed & 0xFF)
            {
                case 0:
                    count = (((seed >> 8) & 0x3F) & ~(remaining & 0x0F));
                    if (count == 0)
                        count = 1;
                    value = 0;
                    break;

                case 1:
                    count = ((seed >> 8) & 0x07) + 1;
                    value = (byte)((seed >> 16) & 0xFF);
                    break;

                case 2:
                    count = ((seed >> 8) & 0x03) + 1;
                    value = (byte)(((seed >> 16) & 0xFF) ^ 0xFF);
                    break;

                case 3:
                    count = ((seed >> 8) & 0x03) + 1;
                    value = (byte)(((seed >> 16) & 0xFF) ^ 0xA5);
                    break;

                case 4:
                    count = ((seed >> 8) & 0x03) + 1;
                    value = (byte)(((seed >> 16) & 0xFF) ^ 0xC3);
                    break;

                case 5:
                    count = ((seed >> 8) & 0x03) + 1;
                    value = (byte)(((seed >> 16) & 0xFF) ^ 0x96);
                    break;

                case 6:
                    count = ((seed >> 8) & 0x03) + 1;
                    value = (byte)(((seed >> 16) & 0xFF) ^ 0x78);
                    break;

                case 7:
                    count = ((seed >> 8) & 0x03) + 1;
                    value = (byte)(((seed >> 16) & 0xFF) ^ 0x78);
                    break;

                default:
                    count = 1;
                    value = (byte)(((seed >> 8) ^ (seed >> 16)) & 0xFF);
                    break;
            }

            do
            {
                image[pos++] = value;
                --remaining;
            } while (remaining > 0 && --count > 0);

            /* state mutation from psuedo-random number generator */
            seed = unchecked((int)((seed * 0x41C64E6DL + 12345) & 0x7FFFFFFF));
        }
    }
}
