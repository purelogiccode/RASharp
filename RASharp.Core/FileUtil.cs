// New implementation, behavior parity with RALibretro RAHasher (GPL-3.0,
// used as reference only) — the used subset of src/Util.cpp.
// C# provides unicode-safe file access natively, so the wchar dance of the
// C code is unnecessary; semantics (binary read, shared read access, path
// helper behaviors) are preserved.

using System.IO.Compression;
using Serilog;

namespace RASharp.Core;

/// <summary>New implementation, behavior parity with RALibretro RAHasher (GPL-3.0, used as reference only) — the used subset of src/Util.cpp. C# provides unicode-safe file </summary>
public static class FileUtil
{
    /* util::fullPath — absolute path (_wfullpath semantics) */
    /// <summary>util::fullPath — absolute path (_wfullpath semantics)</summary>
    /// <param name="path">the file path</param>
    /// <returns>the generated value</returns>
    public static string FullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "FullPath failed for {Path}", path);
            return path;
        }
    }

    /* util::fileNameWithExtension — text after the last '/' or '\' */
    /// <summary>util::fileNameWithExtension — text after the last '/' or '\'</summary>
    /// <param name="path">the file path</param>
    /// <returns>the generated value</returns>
    public static string FileNameWithExtension(string path)
    {
        var name = path.LastIndexOf('/');
        var bs = path.LastIndexOf('\\');
        if (bs > name)
        {
            name = bs;
        }

        return path.Substring(name + 1);
    }

    /* util::fileName — fileNameWithExtension without the last extension */
    /// <summary>util::fileName — fileNameWithExtension without the last extension</summary>
    /// <param name="path">the file path</param>
    /// <returns>the generated value</returns>
    public static string FileName(string path)
    {
        var filename = FileNameWithExtension(path);
        var ndx = filename.LastIndexOf('.');
        if (ndx >= 0)
        {
            filename = filename.Substring(0, ndx);
        }

        return filename;
    }

    /* util::extension — text from the last '.' to the end, e.g. ".zip"; "" if none */
    /// <summary>util::extension — text from the last '.' to the end, e.g. ".zip"; "" if none</summary>
    /// <param name="path">the file path</param>
    /// <returns>the generated value</returns>
    public static string Extension(string path)
    {
        var dot = path.LastIndexOf('.');
        return dot < 0 ? "" : path.Substring(dot);
    }

    /* util::directory — Windows: strip everything from the last '\' (inclusive);
     * if there is no '\', return the input unchanged */
    /// <summary>util::directory — Windows: strip everything from the last '\' (inclusive); if there is no '\', return the input unchanged</summary>
    /// <param name="path">the file path</param>
    /// <returns>the generated value</returns>
    public static string Directory(string path)
    {
        var ndx = path.LastIndexOf('\\');
        return ndx < 0 ? path : path.Substring(0, ndx);
    }

    /* util::openFile — unicode-safe binary read open; FileStream handles UTF-8
     * paths. SH_DENYNO equivalent: allow shared read/write/delete. */
    /// <summary>util::openFile — unicode-safe binary read open; FileStream handles UTF-8 paths. SH_DENYNO equivalent: allow shared read/write/delete.</summary>
    /// <param name="path">the file path</param>
    /// <returns>the handle, or null on failure</returns>
    public static FileStream? OpenFile(string path)
    {
        try
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "OpenFile failed for {Path}", path);
            return null;
        }
    }

    /* util::loadFile — read the entire file into memory */
    /// <summary>util::loadFile — read the entire file into memory</summary>
    /// <param name="path">the file path</param>
    /// <returns>the result</returns>
    public static byte[]? LoadFile(string path)
    {
        using var file = OpenFile(path);
        if (file == null)
            return null;

        var data = new byte[file.Length];
        long pos = 0;
        while (pos < data.Length)
        {
            var numRead = file.Read(data, (int)pos, (int)(data.Length - pos));
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
    /// <summary>util::loadZippedFile — miniz semantics on ZipArchive: - empty zip -&gt; error, NULL - more than one entry -&gt; error note, hash the entire zip file - single director</summary>
    /// <param name="path">the file path</param>
    /// <param name="unzippedFileName">the unzipped file name parameter</param>
    /// <returns>the result</returns>
    public static byte[]? LoadZippedFile(string path, out string unzippedFileName)
    {
        unzippedFileName = "";
        try
        {
            using var archive = ZipFile.OpenRead(path);
            var entries = archive.Entries;
            switch (entries.Count)
            {
                case 0:
                    Console.Error.WriteLine("Empty zip file \"{0}\"", path);
                    return null;
                case > 1:
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
                var numRead = stream.Read(data, (int)pos, (int)(data.Length - pos));
                if (numRead <= 0)
                    return null;

                pos += numRead;
            }

            unzippedFileName = entry.FullName;
            return data;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "LoadZippedFile failed for {Path}", path);
            return null;
        }
    }

    /* LoadZippedFile variant for entries too large for a byte[] (the CLR caps
     * arrays below 2 GiB; the C mallocs the full entry). Same decision tree:
     * empty zip -> null, multi-entry -> the whole zip file, dir-only -> null,
     * otherwise the first entry — but materialized on disk instead of memory. */
    /// <summary>LoadZippedFile variant for entries too large for a byte[] (the CLR caps arrays below 2 GiB; the C mallocs the full entry). Same decision tree: empty zip -&gt; null, mu</summary>
    /// <param name="path">the file path</param>
    /// <param name="unzippedFileName">the unzipped file name parameter</param>
    /// <returns>a temp file with the content to hash, or null on failure (caller deletes it)</returns>
    public static string? LoadZippedFileToTemp(string path, out string unzippedFileName)
    {
        unzippedFileName = "";
        try
        {
            using var archive = ZipFile.OpenRead(path);
            var entries = archive.Entries;
            switch (entries.Count)
            {
                case 0:
                    Console.Error.WriteLine("Empty zip file \"{0}\"", path);
                    return null;
                case > 1:
                    Console.Error.WriteLine("Zip file \"{0}\" contains {1} files, determining which to open is not supported - returning entire zip file", path, entries.Count);
                    var zipCopy = Path.GetTempFileName();
                    File.Copy(path, zipCopy, overwrite: true);
                    return zipCopy;
            }

            var entry = entries[0];
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Zip file \"{0}\" only contains a directory", path);
                return null;
            }

            var temp = Path.GetTempFileName();
            using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write))
            using (var stream = entry.Open())
            {
                stream.CopyTo(output);
            }

            unzippedFileName = entry.FullName;
            return temp;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "LoadZippedFileToTemp failed for {Path}", path);
            return null;
        }
    }
}
