// HashEncrypted — port of rcheevos hash_encrypted.c (MIT).
// rc_hash_nintendo_3ds: NCSD/NCCH/CIA/3DSX/ELF detection + hashing, with the
// 3DS key material supplied through the encryption callbacks (registered by
// Hash3DS, mirroring RAHasher's Hash3DS.cpp).

using System.Text;
using RetroAchievementsSharp.Models;

namespace RetroAchievementsSharp;

/// <summary>HashEncrypted — port of rcheevos hash_encrypted.c (MIT). rc_hash_nintendo_3ds: NCSD/NCCH/CIA/3DSX/ELF detection + hashing, with the 3DS key material supplied th</summary>
public static class HashEncrypted
{
    /* rc_hash_nintendo_3ds_ncch */
    private static int RcHashNintendo3DsNcch(HashMd5 md5, object fileHandle, byte[] header, byte[]? ciaTitleKey, RcHashIterator iterator)
    {
        const uint maxBufferSize = 64 * 1024 * 1024; /* MAX_BUFFER_SIZE */

        var primaryKey = new byte[AesHelper.KeyLen];
        var secondaryKey = new byte[AesHelper.KeyLen];
        uint i;
        var primaryKeyY = new byte[AesHelper.KeyLen];
        var programId = new byte[8];
        var iv = new byte[AesHelper.BlockLen];
        var ciaIv = new byte[AesHelper.BlockLen];
        var exefsSectionName = new byte[8];

        long exefsOffset = ((uint)header[0x1A3] << 24) | (uint)(header[0x1A2] << 16) | (uint)(header[0x1A1] << 8) | header[0x1A0];
        long exefsRealSize = ((uint)header[0x1A7] << 24) | (uint)(header[0x1A6] << 16) | (uint)(header[0x1A5] << 8) | header[0x1A4];

        /* Offset and size are in "media units" (1 media unit = 0x200 bytes) */
        exefsOffset *= 0x200;
        exefsRealSize *= 0x200;

        var exefsBufferSize = exefsRealSize > maxBufferSize ? maxBufferSize : (uint)exefsRealSize;

        /* This region is technically optional, but it should always be present for executable content (i.e. games) */
        if (exefsOffset == 0 || exefsRealSize == 0)
            return HashEngine.IteratorError(iterator, "ExeFS was not available");

        /* NCCH flag 7 is a bitfield of various crypto related flags */
        var fixedKeyFlag = (byte)(header[0x188 + 7] & 0x01);
        var noCryptoFlag = (byte)(header[0x188 + 7] & 0x04);
        var seedCryptoFlag = (byte)(header[0x188 + 7] & 0x20);

        var ncchVersion = (ushort)((header[0x113] << 8) | header[0x112]);

        if (noCryptoFlag == 0)
        {
            HashEngine.IteratorVerbose(iterator, "Encrypted NCCH detected");

            if (fixedKeyFlag != 0)
            {
                /* Fixed crypto key means all 0s for both keys */
                Array.Clear(primaryKey, 0, primaryKey.Length);
                Array.Clear(secondaryKey, 0, secondaryKey.Length);
                HashEngine.IteratorVerbose(iterator, "Using fixed key crypto");
            }
            else
            {
                if (iterator.Callbacks.Encryption.Get3DsNcchNormalKeys == null)
                    return HashEngine.IteratorError(iterator, "An encrypted NCCH was detected, but the NCCH normal keys callback was not set");

                /* Primary key y is just the first 16 bytes of the header */
                Array.Copy(header, 0, primaryKeyY, 0, primaryKeyY.Length);

                /* NCCH flag 3 indicates which secondary key x slot is used */
                var cryptoMethod = header[0x188 + 3];

                byte secondaryKeyXSlot;
                switch (cryptoMethod)
                {
                    case 0x00:
                        HashEngine.IteratorVerbose(iterator, "Using NCCH crypto method v1");
                        secondaryKeyXSlot = 0x2C;
                        break;
                    case 0x01:
                        HashEngine.IteratorVerbose(iterator, "Using NCCH crypto method v2");
                        secondaryKeyXSlot = 0x25;
                        break;
                    case 0x0A:
                        HashEngine.IteratorVerbose(iterator, "Using NCCH crypto method v3");
                        secondaryKeyXSlot = 0x18;
                        break;
                    case 0x0B:
                        HashEngine.IteratorVerbose(iterator, "Using NCCH crypto method v4");
                        secondaryKeyXSlot = 0x1B;
                        break;
                    default:
                        return HashEngine.IteratorErrorFormatted(iterator, "Invalid crypto method {0:X2}", cryptoMethod);
                }

                /* We only need the program id if we're doing seed crypto */
                if (seedCryptoFlag != 0)
                {
                    HashEngine.IteratorVerbose(iterator, "Using seed crypto");
                    Array.Copy(header, 0x118, programId, 0, programId.Length);
                }

                if (iterator.Callbacks.Encryption.Get3DsNcchNormalKeys(primaryKeyY, secondaryKeyXSlot, seedCryptoFlag != 0 ? programId : null, primaryKey, secondaryKey) == 0)
                    return HashEngine.IteratorError(iterator, "Could not obtain NCCH normal keys");
            }

            switch (ncchVersion)
            {
                case 0:
                case 2:
                    HashEngine.IteratorVerbose(iterator, "Detected NCCH version 0/2");
                    for (i = 0; i < 8; i++)
                    {
                        /* First 8 bytes is the partition id in reverse byte order */
                        iv[7 - i] = header[0x108 + i];
                    }

                    /* Magic number for ExeFS */
                    iv[8] = 2;

                    /* Rest of the bytes are 0 */
                    Array.Clear(iv, 9, iv.Length - 9);
                    break;

                case 1:
                    HashEngine.IteratorVerbose(iterator, "Detected NCCH version 1");
                    for (i = 0; i < 8; i++)
                    {
                        /* First 8 bytes is the partition id in normal byte order */
                        iv[i] = header[0x108 + i];
                    }

                    /* Next 4 bytes are 0 */
                    Array.Clear(iv, 8, 4);

                    /* Last 4 bytes is the ExeFS byte offset in big endian */
                    iv[12] = (byte)((exefsOffset >> 24) & 0xFF);
                    iv[13] = (byte)((exefsOffset >> 16) & 0xFF);
                    iv[14] = (byte)((exefsOffset >> 8) & 0xFF);
                    iv[15] = (byte)(exefsOffset & 0xFF);
                    break;

                default:
                    return HashEngine.IteratorErrorFormatted(iterator, "Invalid NCCH version {0:X4}", ncchVersion);
            }
        }

        /* ASSERT: file position must be +0x200 from start of NCCH (i.e. end of header) */
        exefsOffset -= 0x200;

        if (ciaTitleKey != null)
        {
            /* CBC decryption works by setting the IV to the encrypted previous block.
             * Normally this means we would need to decrypt the data between the header and the ExeFS so the CIA AES state is correct.
             * However, we can abuse how CBC decryption works and just set the IV to last block we would otherwise decrypt.
             * We don't care about the data betweeen the header and ExeFS, so this works fine. */

            HashEngine.FileSeek(iterator, fileHandle, exefsOffset - AesHelper.BlockLen, 1 /* SEEK_CUR */);
            if (HashEngine.FileRead(iterator, fileHandle, ciaIv, AesHelper.BlockLen) != AesHelper.BlockLen)
                return HashEngine.IteratorError(iterator, "Could not read NCCH data");
        }
        else
        {
            /* No encryption present, just skip over the in-between data */
            HashEngine.FileSeek(iterator, fileHandle, exefsOffset, 1 /* SEEK_CUR */);
        }

        var hashBuffer = new byte[exefsBufferSize];

        /* Clear out crypto flags to ensure we get the same hash for decrypted and encrypted ROMs */
        Array.Clear(header, 0x114, 4);
        header[0x188 + 3] = 0;
        header[0x188 + 7] = (byte)(header[0x188 + 7] & ~(0x20 | 0x04 | 0x01));

        HashEngine.IteratorVerbose(iterator, "Hashing 512 byte NCCH header");
        md5.Append(header, 0x200);

        HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} bytes for ExeFS (at NCCH offset {1:X8}{2:X8})",
            exefsBufferSize, (uint)(exefsOffset >> 32), (uint)exefsOffset);

        if (HashEngine.FileRead(iterator, fileHandle, hashBuffer, (int)exefsBufferSize) != exefsBufferSize)
            return HashEngine.IteratorError(iterator, "Could not read ExeFS data");

        if (ciaTitleKey != null)
        {
            HashEngine.IteratorVerbose(iterator, "Performing CIA decryption for ExeFS");
            AesHelper.AesCbcDecrypt(hashBuffer, 0, (int)exefsBufferSize, ciaTitleKey, ciaIv);
        }

        if (noCryptoFlag == 0)
        {
            HashEngine.IteratorVerbose(iterator, "Performing NCCH decryption for ExeFS");

            var counter = new byte[AesHelper.BlockLen];
            Array.Copy(iv, 0, counter, 0, AesHelper.BlockLen);
            var currentKey = primaryKey;

            AesHelper.AesCtrXcrypt(hashBuffer, 0, 0x200, currentKey, counter);

            for (i = 0; i < 8; i++)
            {
                Array.Copy(hashBuffer, (int)(i * 16), exefsSectionName, 0, exefsSectionName.Length);
                ulong exefsSectionOffset = ((uint)hashBuffer[i * 16 + 11] << 24) | (uint)(hashBuffer[i * 16 + 10] << 16) | (uint)(hashBuffer[i * 16 + 9] << 8) | hashBuffer[i * 16 + 8];
                ulong exefsSectionSize = ((uint)hashBuffer[i * 16 + 15] << 24) | (uint)(hashBuffer[i * 16 + 14] << 16) | (uint)(hashBuffer[i * 16 + 13] << 8) | hashBuffer[i * 16 + 12];

                /* 0 size indicates an unused section */
                if (exefsSectionSize == 0)
                    continue;

                /* Offsets must be aligned by a media unit */
                if ((exefsSectionOffset & 0x1FF) != 0)
                    return HashEngine.IteratorError(iterator, "ExeFS section offset is misaligned");

                /* Offset is relative to the end of the header */
                exefsSectionOffset += 0x200;

                /* Check against malformed sections */
                if (exefsSectionOffset + ((exefsSectionSize + 0x1FF) & ~(ulong)0x1FF) > (ulong)exefsRealSize)
                    return HashEngine.IteratorError(iterator, "ExeFS section would overflow");

                if (StartsWith(exefsSectionName, "icon", 4) || StartsWith(exefsSectionName, "banner", 6))
                {
                    /* Align size up by a media unit */
                    exefsSectionSize = (exefsSectionSize + 0x1FF) & ~(ulong)0x1FF;
                    currentKey = primaryKey;
                }
                else
                {
                    /* We don't align size up here, as the padding bytes will use the primary key rather than the secondary key */
                    currentKey = secondaryKey;
                }

                /* In theory, the section offset + size could be greater than the buffer size */
                /* In practice, this likely never occurs, but just in case it does, ignore the section or constrict the size */
                if (exefsSectionOffset + exefsSectionSize > exefsBufferSize)
                {
                    if (exefsSectionOffset >= exefsBufferSize)
                        continue;

                    exefsSectionSize = exefsBufferSize - exefsSectionOffset;
                }

                exefsSectionName[7] = 0;
                HashEngine.IteratorVerboseFormatted(iterator, "Decrypting ExeFS file {0} at ExeFS offset {1:X8} with size {2:X8}",
                    GetNulTerminatedString(exefsSectionName), (uint)exefsSectionOffset, (uint)exefsSectionSize);

                AesHelper.AesCtrXcrypt(hashBuffer, (int)exefsSectionOffset, (int)(exefsSectionSize & ~(ulong)0xF), currentKey, counter);

                if ((exefsSectionSize & 0x1FF) != 0)
                {
                    /* Handle padding bytes, these always use the primary key */
                    exefsSectionOffset += exefsSectionSize;
                    exefsSectionSize = 0x200 - (exefsSectionSize & 0x1FF);

                    HashEngine.IteratorVerboseFormatted(iterator, "Decrypting ExeFS padding at ExeFS offset {0:X8} with size {1:X8}",
                        (uint)exefsSectionOffset, (uint)exefsSectionSize);

                    /* Align our decryption start to an AES block boundary */
                    if ((exefsSectionSize & 0xF) != 0)
                    {
                        /* We're a little evil here re-using the IV like this, but this seems to be the best way to deal with this... */
                        var savedCounter = new byte[AesHelper.BlockLen];
                        Array.Copy(counter, 0, savedCounter, 0, AesHelper.BlockLen);
                        exefsSectionOffset &= ~(ulong)0xF;

                        /* First decrypt these last bytes using the secondary key */
                        AesHelper.AesCtrXcrypt(hashBuffer, (int)exefsSectionOffset, 0x10 - (int)(exefsSectionSize & 0xF), currentKey, counter);

                        /* Now re-encrypt these bytes using the primary key */
                        Array.Copy(savedCounter, 0, counter, 0, AesHelper.BlockLen);
                        currentKey = primaryKey;
                        AesHelper.AesCtrXcrypt(hashBuffer, (int)exefsSectionOffset, 0x10 - (int)(exefsSectionSize & 0xF), currentKey, counter);

                        /* All of the padding can now be decrypted using the primary key */
                        Array.Copy(savedCounter, 0, counter, 0, AesHelper.BlockLen);
                        exefsSectionSize += 0x10 - (exefsSectionSize & 0xF);
                    }

                    currentKey = primaryKey;
                    AesHelper.AesCtrXcrypt(hashBuffer, (int)exefsSectionOffset, (int)exefsSectionSize, currentKey, counter);
                }
            }
        }

        md5.Append(hashBuffer, 0, (int)exefsBufferSize);
        return 1;
    }

    private static bool StartsWith(byte[] buffer, string pattern, int length)
    {
        for (var i = 0; i < length; i++)
        {
            if (buffer[i] != (byte)pattern[i])
                return false;
        }

        return true;
    }

    private static string GetNulTerminatedString(byte[] buffer)
    {
        var length = 0;
        while (length < buffer.Length && buffer[length] != 0)
        {
            ++length;
        }

        return Encoding.ASCII.GetString(buffer, 0, length);
    }

    /* rc_hash_nintendo_3ds_cia_signature_size */
    private static uint RcHashNintendo3DsCiaSignatureSize(byte[] header, RcHashIterator iterator)
    {
        var signatureType = ((uint)header[0] << 24) | (uint)(header[1] << 16) | (uint)(header[2] << 8) | header[3];
        switch (signatureType)
        {
            case 0x010000:
            case 0x010003:
                return 0x200 + 0x3C;

            case 0x010001:
            case 0x010004:
                return 0x100 + 0x3C;

            case 0x010002:
            case 0x010005:
                return 0x3C + 0x40;

            default:
                HashEngine.IteratorErrorFormatted(iterator, "Invalid signature type {0:X8}", signatureType);
                return 0;
        }
    }

    /* rc_hash_nintendo_3ds_cia */
    private static int RcHashNintendo3DsCia(HashMd5 md5, object fileHandle, byte[] header, RcHashIterator iterator)
    {
        const uint ciaHeaderSize = 0x2020; /* Yes, this is larger than the header[0x200], but we only use the beginning of the header */
        const long ciaAlignmentMask = 64 - 1; /* sizes are aligned by 64 bytes */
        var iv = new byte[AesHelper.BlockLen];
        var normalKey = new byte[AesHelper.KeyLen];
        var titleKey = new byte[AesHelper.KeyLen];
        var titleId = new byte[8];
        uint i;

        var certSize = ((uint)header[0x0B] << 24) | (uint)(header[0x0A] << 16) | (uint)(header[0x09] << 8) | header[0x08];
        var tikSize = ((uint)header[0x0F] << 24) | (uint)(header[0x0E] << 16) | (uint)(header[0x0D] << 8) | header[0x0C];
        var tmdSize = ((uint)header[0x13] << 24) | (uint)(header[0x12] << 16) | (uint)(header[0x11] << 8) | header[0x10];

        const long certOffset = (ciaHeaderSize + ciaAlignmentMask) & ~ciaAlignmentMask;
        var tikOffset = (certOffset + certSize + ciaAlignmentMask) & ~ciaAlignmentMask;
        var tmdOffset = (tikOffset + tikSize + ciaAlignmentMask) & ~ciaAlignmentMask;
        var contentOffset = (tmdOffset + tmdSize + ciaAlignmentMask) & ~ciaAlignmentMask;

        /* Check if this CIA is encrypted, if it isn't, we can hash it right away */
        HashEngine.FileSeek(iterator, fileHandle, tmdOffset, 0 /* SEEK_SET */);
        if (HashEngine.FileRead(iterator, fileHandle, header, 4) != 4)
            return HashEngine.IteratorError(iterator, "Could not read TMD signature type");

        var signatureSize = RcHashNintendo3DsCiaSignatureSize(header, iterator);
        if (signatureSize == 0)
            return 0; /* RcHashNintendo3DsCiaSignatureSize will call IteratorError, so we don't need to do so here */

        HashEngine.FileSeek(iterator, fileHandle, signatureSize + 0x9E, 1 /* SEEK_CUR */);
        if (HashEngine.FileRead(iterator, fileHandle, header, 2) != 2)
            return HashEngine.IteratorError(iterator, "Could not read TMD content count");

        var contentCount = (ushort)((header[0] << 8) | header[1]);

        HashEngine.FileSeek(iterator, fileHandle, 0x9C4 - 0x9E - 2, 1 /* SEEK_CUR */);
        for (i = 0; i < contentCount; i++)
        {
            if (HashEngine.FileRead(iterator, fileHandle, header, 0x30) != 0x30)
                return HashEngine.IteratorError(iterator, "Could not read TMD content chunk");

            /* Content index 0 is the main content (i.e. the 3DS executable)  */
            if (((header[4] << 8) | header[5]) == 0)
                break;

            contentOffset += ((uint)header[0xC] << 24) | (uint)(header[0xD] << 16) | (uint)(header[0xE] << 8) | header[0xF];
        }

        if (i == contentCount)
            return HashEngine.IteratorError(iterator, "Could not find main content chunk in TMD");

        if ((header[7] & 1) == 0)
        {
            /* Not encrypted, we can hash the NCCH immediately */
            HashEngine.FileSeek(iterator, fileHandle, contentOffset, 0 /* SEEK_SET */);
            if (HashEngine.FileRead(iterator, fileHandle, header, 0x200) != 0x200)
                return HashEngine.IteratorError(iterator, "Could not read NCCH header");

            if (!StartsWith(header, "NCCH", 4, 0x100))
                return HashEngine.IteratorErrorFormatted(iterator, "NCCH header was not at {0:X8}{1:X8}", (uint)(contentOffset >> 32), (uint)contentOffset);

            return RcHashNintendo3DsNcch(md5, fileHandle, header, null, iterator);
        }

        if (iterator.Callbacks.Encryption.Get3DsCiaNormalKey == null)
            return HashEngine.IteratorError(iterator, "An encrypted CIA was detected, but the CIA normal key callback was not set");

        /* Acquire the encrypted title key, title id, and common key index from the ticket */
        /* These will be needed to decrypt the title key, and that will be needed to decrypt the CIA */

        HashEngine.FileSeek(iterator, fileHandle, tikOffset, 0 /* SEEK_SET */);
        if (HashEngine.FileRead(iterator, fileHandle, header, 4) != 4)
            return HashEngine.IteratorError(iterator, "Could not read ticket signature type");

        signatureSize = RcHashNintendo3DsCiaSignatureSize(header, iterator);
        if (signatureSize == 0)
            return 0;

        HashEngine.FileSeek(iterator, fileHandle, signatureSize, 1 /* SEEK_CUR */);
        if (HashEngine.FileRead(iterator, fileHandle, header, 0xB2) != 0xB2)
            return HashEngine.IteratorError(iterator, "Could not read ticket data");

        Array.Copy(header, 0x7F, titleKey, 0, titleKey.Length);
        Array.Copy(header, 0x9C, titleId, 0, titleId.Length);
        var commonKeyIndex = header[0xB1];

        if (commonKeyIndex > 5)
            return HashEngine.IteratorErrorFormatted(iterator, "Invalid common key index {0:X2}", commonKeyIndex);

        if (iterator.Callbacks.Encryption.Get3DsCiaNormalKey(commonKeyIndex, normalKey) == 0)
            return HashEngine.IteratorErrorFormatted(iterator, "Could not obtain common key {0:X2}", commonKeyIndex);

        Array.Clear(iv, 0, iv.Length);
        Array.Copy(titleId, 0, iv, 0, titleId.Length);
        AesHelper.AesCbcDecrypt(titleKey, 0, titleKey.Length, normalKey, iv);

        /* Now we can hash the NCCH */

        HashEngine.FileSeek(iterator, fileHandle, contentOffset, 0 /* SEEK_SET */);
        if (HashEngine.FileRead(iterator, fileHandle, header, 0x200) != 0x200)
            return HashEngine.IteratorError(iterator, "Could not read NCCH header");

        Array.Clear(iv, 0, iv.Length); /* Content index is iv (which is always 0 for main content) */
        AesHelper.AesCbcDecrypt(header, 0, 0x200, titleKey, iv);

        if (!StartsWith(header, "NCCH", 4, 0x100))
            return HashEngine.IteratorErrorFormatted(iterator, "NCCH header was not at {0:X8}{1:X8}", (uint)(contentOffset >> 32), (uint)contentOffset);

        return RcHashNintendo3DsNcch(md5, fileHandle, header, titleKey, iterator);
    }

    /* rc_hash_nintendo_3ds_3dsx */
    private static int RcHashNintendo3Ds3Dsx(HashMd5 md5, object fileHandle, byte[] header, RcHashIterator iterator)
    {
        const uint maxBufferSize = 64 * 1024 * 1024; /* MAX_BUFFER_SIZE */

        var headerSize = (uint)((header[5] << 8) | header[4]);
        var relocHeaderSize = (uint)((header[7] << 8) | header[6]);
        var codeSize = ((uint)header[0x13] << 24) | (uint)(header[0x12] << 16) | (uint)(header[0x11] << 8) | header[0x10];

        /* 3 relocation headers are in-between the 3DSX header and code segment */
        long codeOffset = headerSize + relocHeaderSize * 3;

        if (codeSize > maxBufferSize)
        {
            codeSize = maxBufferSize;
        }

        var hashBuffer = new byte[codeSize];

        HashEngine.FileSeek(iterator, fileHandle, codeOffset, 0 /* SEEK_SET */);

        HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} bytes for 3DSX (at {1:X8})", codeSize, (uint)codeOffset);

        if (HashEngine.FileRead(iterator, fileHandle, hashBuffer, (int)codeSize) != codeSize)
            return HashEngine.IteratorError(iterator, "Could not read 3DSX code segment");

        md5.Append(hashBuffer, 0, (int)codeSize);
        return 1;
    }

    private static bool StartsWith(byte[] buffer, string pattern, int length, int offset)
    {
        for (var i = 0; i < length; i++)
        {
            if (buffer[offset + i] != (byte)pattern[i])
                return false;
        }

        return true;
    }

    /* rc_hash_nintendo_3ds */
    /// <summary>rc_hash_nintendo_3ds</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashNintendo3Ds(out string hash, RcHashIterator iterator)
    {
        hash = "";
        var md5 = new HashMd5();
        var header = new byte[0x200]; /* NCCH and NCSD headers are both 0x200 bytes */

        var fileHandle = HashEngine.FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open file");

        HashEngine.FileSeek(iterator, fileHandle, 0, 0 /* SEEK_SET */);

        /* If we don't have a full header, this is probably not a 3DS ROM */
        if (HashEngine.FileRead(iterator, fileHandle, header, header.Length) != header.Length)
        {
            HashEngine.FileClose(iterator, fileHandle);
            return HashEngine.IteratorError(iterator, "Could not read 3DS ROM header");
        }

        if (StartsWith(header, "NCSD", 4, 0x100))
        {
            /* A NCSD container contains 1-8 NCCH partitions */
            /* The first partition (index 0) is reserved for executable content */
            long headerOffset = ((uint)header[0x123] << 24) | (uint)(header[0x122] << 16) | (uint)(header[0x121] << 8) | header[0x120];
            /* Offset is in "media units" (1 media unit = 0x200 bytes) */
            headerOffset *= 0x200;

            /* We include the NCSD header in the hash, as that will ensure different versions of a game result in a different hash
             * This is due to some revisions / languages only ever changing other NCCH paritions (e.g. the game manual)
             */
            HashEngine.IteratorVerbose(iterator, "Hashing 512 byte NCSD header");
            md5.Append(header, 0x200);

            HashEngine.IteratorVerboseFormatted(iterator,
                "Detected NCSD header, seeking to NCCH partition at {0:X8}{1:X8}",
                (uint)(headerOffset >> 32), (uint)headerOffset);

            HashEngine.FileSeek(iterator, fileHandle, headerOffset, 0 /* SEEK_SET */);
            if (HashEngine.FileRead(iterator, fileHandle, header, header.Length) != header.Length)
            {
                HashEngine.FileClose(iterator, fileHandle);
                return HashEngine.IteratorError(iterator, "Could not read 3DS NCCH header");
            }

            if (!StartsWith(header, "NCCH", 4, 0x100))
            {
                HashEngine.FileClose(iterator, fileHandle);
                return HashEngine.IteratorErrorFormatted(iterator, "3DS NCCH header was not at {0:X8}{1:X8}", (uint)(headerOffset >> 32), (uint)headerOffset);
            }
        }

        if (StartsWith(header, "NCCH", 4, 0x100))
        {
            if (RcHashNintendo3DsNcch(md5, fileHandle, header, null, iterator) != 0)
            {
                HashEngine.FileClose(iterator, fileHandle);
                return HashEngine.Finalize(iterator, md5, out hash);
            }

            HashEngine.FileClose(iterator, fileHandle);
            return HashEngine.IteratorError(iterator, "Failed to hash 3DS NCCH container");
        }

        /* Couldn't identify either an NCSD or NCCH */

        /* Try to identify this as a CIA */
        if (header[0] == 0x20 && header[1] == 0x20 && header[2] == 0x00 && header[3] == 0x00)
        {
            HashEngine.IteratorVerbose(iterator, "Detected CIA, attempting to find executable NCCH");

            if (RcHashNintendo3DsCia(md5, fileHandle, header, iterator) != 0)
            {
                HashEngine.FileClose(iterator, fileHandle);
                return HashEngine.Finalize(iterator, md5, out hash);
            }

            HashEngine.FileClose(iterator, fileHandle);
            return HashEngine.IteratorError(iterator, "Failed to hash 3DS CIA container");
        }

        /* This might be a homebrew game, try to detect that */
        if (StartsWith(header, "3DSX", 4, 0))
        {
            HashEngine.IteratorVerbose(iterator, "Detected 3DSX");

            if (RcHashNintendo3Ds3Dsx(md5, fileHandle, header, iterator) != 0)
            {
                HashEngine.FileClose(iterator, fileHandle);
                return HashEngine.Finalize(iterator, md5, out hash);
            }

            HashEngine.FileClose(iterator, fileHandle);
            return HashEngine.IteratorError(iterator, "Failed to hash 3DS 3DSX container");
        }

        /* Raw ELF marker (AXF/ELF files) */
        if (StartsWith(header, "\x7fE" + "LF", 4, 0))
        {
            HashEngine.IteratorVerbose(iterator, "Detected AXF/ELF file, hashing entire file");

            /* Don't bother doing anything fancy here, just hash entire file */
            HashEngine.FileClose(iterator, fileHandle);
            return RcHashWholeFile(out hash, iterator);
        }

        HashEngine.FileClose(iterator, fileHandle);
        return HashEngine.IteratorError(iterator, "Not a 3DS ROM");
    }

    private static int RcHashWholeFile(out string hash, RcHashIterator iterator)
    {
        return HashEngine.WholeFile(out hash, iterator);
    }
}
