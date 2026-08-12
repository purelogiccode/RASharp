// New implementation, behavior parity with RALibretro RAHasher (GPL-3.0,
// used as reference only) — the used subset of src/Util.cpp.
// C# provides unicode-safe file access natively, so the wchar dance of the
// C code is unnecessary; semantics (binary read, shared read access, path
// helper behaviors) are preserved.

using System.IO.Compression;

namespace RASharp.Core;

public static class FileUtil
{
    /* util::fullPath — absolute path (_wfullpath semantics) */
    public static string FullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }

    /* util::fileNameWithExtension — text after the last '/' or '\' */
    public static string FileNameWithExtension(string path)
    {
        int name = path.LastIndexOf('/');
        int bs = path.LastIndexOf('\\');
        if (bs > name)
            name = bs;

        return path.Substring(name + 1);
    }

    /* util::fileName — fileNameWithExtension without the last extension */
    public static string FileName(string path)
    {
        string filename = FileNameWithExtension(path);
        int ndx = filename.LastIndexOf('.');
        if (ndx >= 0)
            filename = filename.Substring(0, ndx);

        return filename;
    }

    /* util::extension — text from the last '.' to the end, e.g. ".zip"; "" if none */
    public static string Extension(string path)
    {
        int dot = path.LastIndexOf('.');
        return dot < 0 ? "" : path.Substring(dot);
    }

    /* util::directory — Windows: strip everything from the last '\' (inclusive);
     * if there is no '\', return the input unchanged */
    public static string Directory(string path)
    {
        int ndx = path.LastIndexOf('\\');
        return ndx < 0 ? path : path.Substring(0, ndx);
    }

    /* util::openFile — unicode-safe binary read open; FileStream handles UTF-8
     * paths. SH_DENYNO equivalent: allow shared read/write/delete. */
    public static FileStream? OpenFile(string path)
    {
        try
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
        catch
        {
            return null;
        }
    }

    /* util::loadFile — read the entire file into memory */
    public static byte[]? LoadFile(string path)
    {
        using var file = OpenFile(path);
        if (file == null)
            return null;

        var data = new byte[file.Length];
        long pos = 0;
        while (pos < data.Length)
        {
            int numRead = file.Read(data, (int)pos, (int)(data.Length - pos));
            if (numRead <= 0)
                return null;

            pos += numRead;
        }

        return data;
    }

    /* util::loadZippedFile — miniz semantics on ZipArchive:
     * - empty zip -> error, NULL
     * - more than one entry -> error note, hash the entire zip file
     * - single directory entry -> error, NULL
     * - otherwise extract the first entry */
    public static byte[]? LoadZippedFile(string path, out string unzippedFileName)
    {
        unzippedFileName = "";
        try
        {
            using var archive = ZipFile.OpenRead(path);
            var entries = archive.Entries;
            if (entries.Count == 0)
            {
                Console.Error.WriteLine("Empty zip file \"{0}\"", path);
                return null;
            }

            if (entries.Count > 1)
            {
                Console.Error.WriteLine("Zip file \"{0}\" contains {1} files, determining which to open is not supported - returning entire zip file", path, entries.Count);
                return LoadFile(path);
            }

            var entry = entries[0];
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Zip file \"{0}\" only contains a directory", path);
                return null;
            }

            using var stream = entry.Open();
            var data = new byte[entry.Length];
            long pos = 0;
            while (pos < data.Length)
            {
                int numRead = stream.Read(data, (int)pos, (int)(data.Length - pos));
                if (numRead <= 0)
                    return null;

                pos += numRead;
            }

            unzippedFileName = entry.FullName;
            return data;
        }
        catch
        {
            return null;
        }
    }
}
