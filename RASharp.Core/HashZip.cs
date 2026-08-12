// HashZip — port of rcheevos hash_zip.c (MIT).
// Byte-level ZIP parsing (no System.IO.Compression) to reproduce the exact
// hash semantics: EOCD scan, Zip64 handling, central-directory walk with
// entry filters, name normalization (backslash->slash, upper->lower),
// record hashing in byte-sorted order. Also rc_hash_arduboyfx and
// rc_hash_ms_dos (DOSZ/DOSC/parent).

using System.Text;
using RASharp.Core.Models;

namespace RASharp.Core;

/// <summary>HashZip — port of rcheevos hash_zip.c (MIT). Byte-level ZIP parsing (no System.IO.Compression) to reproduce the exact hash semantics: EOCD scan, Zip64 handling,</summary>
public static class HashZip
{
    /* typedef rc_hash_zip_filter_t */
    private delegate int ZipFilterFunc(byte[] filename, int filenameLen, ulong decompSize, object? userdata);

    /* struct rc_hash_zip_idx — record + length for the sort */
    private sealed class ZipIdx
    {
        public byte[] Data = Array.Empty<byte>();
    }

    private static ushort ReadLe16(byte[] p, int o)
    {
        return (ushort)(p[o] | (p[o + 1] << 8));
    }

    private static uint ReadLe32(byte[] p, int o)
    {
        return (uint)(p[o] | (p[o + 1] << 8) | (p[o + 2] << 16) | (p[o + 3] << 24));
    }

    private static ulong ReadLe64(byte[] p, int o)
    {
        return ReadLe32(p, o) | ((ulong)ReadLe32(p, o + 4) << 32);
    }

    private static void WriteLe32(byte[] p, int o, uint v)
    {
        p[o] = (byte)(v & 0xFF);
        p[o + 1] = (byte)((v >> 8) & 0xFF);
        p[o + 2] = (byte)((v >> 16) & 0xFF);
        p[o + 3] = (byte)((v >> 24) & 0xFF);
    }

    private static void WriteLe64(byte[] p, int o, ulong v)
    {
        WriteLe32(p, o, (uint)(v & 0xFFFFFFFF));
        WriteLe32(p, o + 4, (uint)(v >> 32));
    }

    /* rc_hash_zip_idx_sort — memcmp over the shorter record */
    private static int ZipIdxCompare(ZipIdx a, ZipIdx b)
    {
        var len = Math.Min(a.Data.Length, b.Data.Length);
        for (var i = 0; i < len; i++)
        {
            if (a.Data[i] != b.Data[i])
                return a.Data[i] < b.Data[i] ? -1 : 1;
        }

        return 0;
    }

    /* rc_hash_zip_file */
    private static int ZipFileHash(HashMd5 md5, object fileHandle, RcHashIterator iterator, ZipFilterFunc? filterFunc, object? filterUserdata)
    {
        var buf = new byte[2048];
        long ecdhOfs;
        const int eocdirhdrSize = 22; /* the 'end of central directory header' is 22 bytes */
        const int cdirhdrSize = 46; /* the 'central directory header' is 46 bytes */
        var hashindices = new List<ZipIdx>();

        HashEngine.FileSeek(iterator, fileHandle, 0, HashEngine.SeekEnd);
        var archiveSize = HashEngine.FileTell(iterator, fileHandle);

        /* Basic sanity checks - reject files which are too small */
        if (archiveSize < eocdirhdrSize)
            return HashEngine.IteratorError(iterator, "ZIP is too small");

        /* Find the end of central directory record by scanning the file from the end towards the beginning */
        for (ecdhOfs = archiveSize - buf.Length;; ecdhOfs -= (buf.Length - 3))
        {
            int i, n = buf.Length;
            if (ecdhOfs < 0)
            {
                ecdhOfs = 0;
            }

            if (n > archiveSize)
            {
                n = (int)archiveSize;
            }

            HashEngine.FileSeek(iterator, fileHandle, ecdhOfs, HashEngine.SeekSet);
            if (HashEngine.FileRead(iterator, fileHandle, buf, n) != n)
                return HashEngine.IteratorError(iterator, "ZIP read error");

            for (i = n - 4; i >= 0; --i)
            {
                if (buf[i] == (byte)'P' && ReadLe32(buf, i) == 0x06054b50) /* end of central directory header signature */
                    break;
            }

            if (i >= 0)
            {
                ecdhOfs += i;
                break;
            }

            if (ecdhOfs == 0 || (archiveSize - ecdhOfs) >= (0xFFFF + eocdirhdrSize))
                return HashEngine.IteratorError(iterator, "Failed to find ZIP central directory");
        }

        /* Read and verify the end of central directory record. */
        HashEngine.FileSeek(iterator, fileHandle, ecdhOfs, HashEngine.SeekSet);
        if (HashEngine.FileRead(iterator, fileHandle, buf, eocdirhdrSize) != eocdirhdrSize)
            return HashEngine.IteratorError(iterator, "Failed to read ZIP central directory");

        /* Read central dir information from end of central directory header */
        long totalFiles = ReadLe16(buf, 0x0A);
        long cdirSize = ReadLe32(buf, 0x0C);
        long cdirOfs = ReadLe32(buf, 0x10);

        /* Check if this is a Zip64 file. In the block of code below:
         * - 20 is the size of the ZIP64 end of central directory locator
         * - 56 is the size of the ZIP64 end of central directory header
         */
        if ((cdirOfs == 0xFFFFFFFF || cdirSize == 0xFFFFFFFF || totalFiles == 0xFFFF) && ecdhOfs >= (20 + 56))
        {
            /* Read the ZIP64 end of central directory locator if it actually exists */
            HashEngine.FileSeek(iterator, fileHandle, ecdhOfs - 20, HashEngine.SeekSet);
            if (HashEngine.FileRead(iterator, fileHandle, buf, 20) == 20 && ReadLe32(buf, 0) == 0x07064b50) /* locator signature */
            {
                /* Found the locator, now read the actual ZIP64 end of central directory header */
                var ecdh64Ofs = (long)ReadLe64(buf, 0x08);
                if (ecdh64Ofs <= (archiveSize - 56))
                {
                    HashEngine.FileSeek(iterator, fileHandle, ecdh64Ofs, HashEngine.SeekSet);
                    if (HashEngine.FileRead(iterator, fileHandle, buf, 56) == 56 && ReadLe32(buf, 0) == 0x06064b50) /* header signature */
                    {
                        totalFiles = (long)ReadLe64(buf, 0x20);
                        cdirSize = (long)ReadLe64(buf, 0x28);
                        cdirOfs = (long)ReadLe64(buf, 0x30);
                    }
                }
            }
        }

        /* Basic verificaton of central directory (limit to a 256MB content directory) */
        if ((cdirSize >= 0x10000000) || (cdirSize < totalFiles * cdirhdrSize) || ((cdirOfs + cdirSize) > archiveSize))
            return HashEngine.IteratorError(iterator, "Central directory of ZIP file is invalid");

        /* Read entire central directory to a buffer */
        var allocBuf = new byte[(int)cdirSize];

        HashEngine.FileSeek(iterator, fileHandle, cdirOfs, HashEngine.SeekSet);
        if (HashEngine.FileRead(iterator, fileHandle, allocBuf, (int)cdirSize) != cdirSize)
            return HashEngine.IteratorError(iterator, "Failed to read central directory of ZIP file");

        var cdirMax = (int)(cdirSize - cdirhdrSize);
        var cdir = 0;
        int cdirEntryLen;

        /* Now process the central directory file records */
        for (long iFile = 0; cdir >= 0 && cdir <= cdirMax && iFile < totalFiles; iFile++, cdir += cdirEntryLen)
        {
            var signature = ReadLe32(allocBuf, cdir + 0x00);
            uint method = ReadLe16(allocBuf, cdir + 0x0A);
            var crc32 = ReadLe32(allocBuf, cdir + 0x10);
            ulong compSize = ReadLe32(allocBuf, cdir + 0x14);
            ulong decompSize = ReadLe32(allocBuf, cdir + 0x18);
            uint filenameLen = ReadLe16(allocBuf, cdir + 0x1C);
            int extraLen = ReadLe16(allocBuf, cdir + 0x1E);
            int commentLen = ReadLe16(allocBuf, cdir + 0x20);
            int externalAttr = ReadLe16(allocBuf, cdir + 0x26);
            ulong localHdrOfs = ReadLe32(allocBuf, cdir + 0x2A);
            cdirEntryLen = cdirhdrSize + (int)filenameLen + extraLen + commentLen;

            if (signature != 0x02014b50) /* expected central directory entry signature */
                break;

            /* Ignore records describing a directory (we only hash file records) */
            var name = new byte[filenameLen];
            Array.Copy(allocBuf, cdir + cdirhdrSize, name, 0, filenameLen);
            if (filenameLen == 0 || name[filenameLen - 1] == (byte)'/' || name[filenameLen - 1] == (byte)'\\' || (externalAttr & 0x10) != 0)
                continue;

            /* Handle Zip64 fields */
            if (decompSize == 0xFFFFFFFF || compSize == 0xFFFFFFFF || localHdrOfs == 0xFFFFFFFF)
            {
                var invalid = false;
                var x = cdir + cdirhdrSize + (int)filenameLen;
                var xEnd = x + extraLen;
                while ((x + 4) < xEnd)
                {
                    var field = x + 4;
                    var fieldEnd = field + ReadLe16(allocBuf, x + 2);
                    if (ReadLe16(allocBuf, x) != 0x0001 || fieldEnd > xEnd)
                    {
                        x = fieldEnd;
                        continue; /* Not the Zip64 extended information extra field */
                    }

                    if (decompSize == 0xFFFFFFFF)
                    {
                        if ((fieldEnd - field) < 8)
                        {
                            invalid = true;
                            break;
                        }

                        decompSize = ReadLe64(allocBuf, field);
                        field += 8;
                    }

                    if (compSize == 0xFFFFFFFF)
                    {
                        if ((fieldEnd - field) < 8)
                        {
                            invalid = true;
                            break;
                        }

                        compSize = ReadLe64(allocBuf, field);
                        field += 8;
                    }

                    if (localHdrOfs == 0xFFFFFFFF)
                    {
                        if ((fieldEnd - field) < 8)
                        {
                            invalid = true;
                            break;
                        }

                        localHdrOfs = ReadLe64(allocBuf, field);
                    }

                    break;
                }

                if (invalid)
                    return HashEngine.IteratorError(iterator, "Encountered invalid Zip64 file");
            }

            /* Basic sanity check on file record */
            /* 30 is the length of the local directory header preceeding the compressed data */
            if ((method == 0 && decompSize != compSize) || (decompSize != 0 && compSize == 0) ||
                ((localHdrOfs + 30 + compSize) > (ulong)archiveSize))
                return HashEngine.IteratorError(iterator, "Encountered invalid entry in ZIP central directory");

            if (filterFunc != null)
            {
                var filtered = filterFunc(name, (int)filenameLen, decompSize, filterUserdata);
                if (filtered < 0)
                    return 0;

                if (filtered != 0) /* this file shouldn't be hashed */
                    continue;
            }

            /* Write the pointer and length of the data we record about this file */
            var hashindex = new ZipIdx { Data = new byte[filenameLen + 1 + 4 + 8] };
            hashindices.Add(hashindex);

            HashEngine.IteratorVerboseFormatted(iterator, "File in ZIP: {0} ({1} bytes, CRC32 = {2:X8})",
                Encoding.ASCII.GetString(name, 0, (int)filenameLen), decompSize, crc32);

            /* Convert and store the file name in the hash data buffer */
            var hashdata = 0;
            for (var i = 0; i < filenameLen; i++)
            {
                var ch = name[i];
                hashindex.Data[hashdata++] =
                    (ch switch
                    {
                        (byte)'\\' => (byte)'/',
                        >= (byte)'A' and <= (byte)'Z' => (byte)(ch | 0x20),
                        _ => ch
                    }); /* else use the byte as-is */
            }

            /* Add zero terminator, CRC32 and decompressed size to the hash data buffer */
            hashindex.Data[hashdata++] = 0;
            WriteLe32(hashindex.Data, hashdata, crc32);
            hashdata += 4;
            WriteLe64(hashindex.Data, hashdata, decompSize);
        }

        HashEngine.IteratorVerboseFormatted(iterator, "Hashing {0} files in ZIP archive", hashindices.Count);

        /* Sort the file list indices */
        hashindices.Sort(ZipIdxCompare);

        /* Hash the data in the order of the now sorted indices */
        foreach (ZipIdx hashindex in hashindices)
            md5.Append(hashindex.Data, 0, hashindex.Data.Length);

        return 1;
    }

    /* rc_hash_arduboyfx_filter */
    private static int ArduboyFxFilter(byte[] filename, int filenameLen, ulong decompSize, object? userdata)
    {
        /* An .arduboy file is a zip file containing an info.json pointing at one or more bin
         * and hex files. It can also contain a bunch of screenshots, but we don't care about
         * those. As they're also referenced in the info.json, we have to ignore that too.
         * Instead of ignoring the info.json and all image files, only process any bin/hex files */
        if (filenameLen > 4)
        {
            if ((filename[filenameLen - 4] == '.' && (filename[filenameLen - 3] | 0x20) == 'b' && (filename[filenameLen - 2] | 0x20) == 'i' && (filename[filenameLen - 1] | 0x20) == 'n') ||
                (filename[filenameLen - 4] == '.' && (filename[filenameLen - 3] | 0x20) == 'h' && (filename[filenameLen - 2] | 0x20) == 'e' && (filename[filenameLen - 1] | 0x20) == 'x'))
                return 0; /* keep hex and bin */
        }

        return 1; /* filter everything else */
    }

    /* rc_hash_arduboyfx */
    /// <summary>rc_hash_arduboyfx</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashArduboyFx(out string hash, RcHashIterator iterator)
    {
        hash = "";
        var md5 = new HashMd5();

        var fileHandle = HashEngine.FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open file");

        var res = ZipFileHash(md5, fileHandle, iterator, ArduboyFxFilter, null);
        HashEngine.FileClose(iterator, fileHandle);

        if (res == 0)
            return 0;

        return HashEngine.Finalize(iterator, md5, out hash);
    }

    /* struct rc_hash_ms_dos_dosz_state */
    private sealed class MsDosDoszState
    {
        public string Path = "";
        public MsDosDoszState? Child;
        public HashMd5? Md5;
        public RcHashIterator? Iterator;
        public object? FileHandle;
        public uint NParents;
    }

    private static int MsDosParent(MsDosDoszState child, byte[] parentname, int parentnameLen)
    {
        var lastfslash = child.Path.LastIndexOf('/');
        var lastbslash = child.Path.LastIndexOf('\\');
        var lastslash = Math.Max(lastfslash, lastbslash);
        var dirLen = (lastslash >= 0 ? lastslash + 1 : 0);
        var parentPath = child.Path.Substring(0, dirLen) + Encoding.ASCII.GetString(parentname, 0, parentnameLen);

        /* Make sure there is no recursion where a parent DOSZ is an already seen child DOSZ */
        for (MsDosDoszState? check = child.Child; check != null; check = check.Child)
        {
            if (string.Equals(check.Path, parentPath, StringComparison.Ordinal))
                return HashEngine.IteratorError(child.Iterator!, "Invalid DOSZ file with recursive parents");
        }

        /* Try to open the parent DOSZ file */
        var parentHandle = HashEngine.FileOpen(child.Iterator!, parentPath);
        if (parentHandle == null)
        {
            HashEngine.IteratorErrorFormatted(child.Iterator!, "DOSZ parent file '{0}' does not exist", parentPath);
            return 0;
        }

        /* Fully hash the parent DOSZ ahead of the child */
        var parent = new MsDosDoszState
        {
            Path = parentPath,
            Child = child,
            Md5 = child.Md5,
            Iterator = child.Iterator,
            FileHandle = parentHandle,
            NParents = child.NParents
        };
        var parentRes = RcHashDosz(parent);
        HashEngine.FileClose(child.Iterator!, parentHandle);
        return parentRes;
    }

    private static int MsDosDosc(MsDosDoszState dosz)
    {
        var pathLen = dosz.Path.Length;
        if (dosz.Path[pathLen - 1] == 'z' || dosz.Path[pathLen - 1] == 'Z')
        {
            /* Swap the z to c and use the same capitalization, hash the file if it exists */
            var doscPathChars = dosz.Path.ToCharArray();
            doscPathChars[pathLen - 1] = (dosz.Path[pathLen - 1] == 'z' ? 'c' : 'C');
            var doscPath = new string(doscPathChars);

            var fileHandle = HashEngine.FileOpen(dosz.Iterator!, doscPath);
            if (fileHandle != null)
            {
                /* Hash the entire contents of the DOSC file */
                var res = ZipFileHash(dosz.Md5!, fileHandle, dosz.Iterator!, null, null);
                HashEngine.FileClose(dosz.Iterator!, fileHandle);
                if (res == 0)
                    return 0;
            }
        }

        return 1;
    }

    /* rc_hash_dosz_filter */
    private static int DoszFilter(byte[] filename, int filenameLen, ulong decompSize, object? userdata)
    {
        var dosz = (MsDosDoszState)userdata!;

        /* A DOSZ file can contain a special empty <base>.dosz.parent file in its root which means a parent dosz file is used */
        if (decompSize == 0 && filenameLen > 7 &&
            (filename[filenameLen - 7] | 0x20) == '.' && (filename[filenameLen - 6] | 0x20) == 'p' &&
            (filename[filenameLen - 5] | 0x20) == 'a' && (filename[filenameLen - 4] | 0x20) == 'r' &&
            (filename[filenameLen - 3] | 0x20) == 'e' && (filename[filenameLen - 2] | 0x20) == 'n' &&
            (filename[filenameLen - 1] | 0x20) == 't' &&
            !ContainsChar(filename, filenameLen, (byte)'/') &&
            !ContainsChar(filename, filenameLen, (byte)'\\'))
        {
            /* A DOSZ file can only have one parent file */
            if (dosz.NParents++ != 0)
                return -1;

            /* process the parent. if it fails, stop */
            var parentname = new byte[filenameLen - 7];
            Array.Copy(filename, 0, parentname, 0, filenameLen - 7);
            if (MsDosParent(dosz, parentname, filenameLen - 7) == 0)
                return -1;

            /* We don't hash this meta file so a user is free to rename it and the parent file */
            return 1;
        }

        return 0;
    }

    private static bool ContainsChar(byte[] buffer, int length, byte ch)
    {
        for (var i = 0; i < length; i++)
        {
            if (buffer[i] == ch)
                return true;
        }

        return false;
    }

    /* rc_hash_dosz */
    private static int RcHashDosz(MsDosDoszState dosz)
    {
        if (ZipFileHash(dosz.Md5!, dosz.FileHandle!, dosz.Iterator!, DoszFilter, dosz) == 0)
            return 0;

        /* A DOSZ file can only have one parent file */
        if (dosz.NParents > 1)
            return HashEngine.IteratorError(dosz.Iterator!, "Invalid DOSZ file with multiple parents");

        /* Check if an associated .dosc file exists */
        if (MsDosDosc(dosz) == 0)
            return 0;

        return 1;
    }

    /* rc_hash_ms_dos */
    /// <summary>rc_hash_ms_dos</summary>
    /// <param name="hash">the generated 32-char hash</param>
    /// <param name="iterator">the hash iterator</param>
    /// <returns>the result</returns>
    public static int RcHashMsDos(out string hash, RcHashIterator iterator)
    {
        hash = "";
        var md5 = new HashMd5();

        var fileHandle = HashEngine.FileOpen(iterator, iterator.Path!);
        if (fileHandle == null)
            return HashEngine.IteratorError(iterator, "Could not open file");

        var dosz = new MsDosDoszState
        {
            Path = iterator.Path!,
            FileHandle = fileHandle,
            Iterator = iterator,
            Md5 = md5
        };

        var res = RcHashDosz(dosz);
        HashEngine.FileClose(iterator, fileHandle);

        if (res == 0)
            return 0;

        return HashEngine.Finalize(iterator, md5, out hash);
    }
}
