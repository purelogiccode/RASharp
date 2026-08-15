// Tests for the FileUtil port (RALibretro RAHasher src/Util.cpp subset):
// pure path-string helpers plus the file/zip loaders. The loaders use the
// real file system (temp directory) — no mock filereader involved.

using System.IO.Compression;
using RASharp;

namespace RASharp.Tests;

/// <summary>Tests for the FileUtil port (RALibretro RAHasher src/Util.cpp subset): pure path-string helpers plus the file/zip loaders. The loaders use the real file syst</summary>
public class TestFileUtil : IDisposable
{
    private readonly string _root;

    public TestFileUtil()
    {
        _root = Path.Combine(Path.GetTempPath(), "rasharp_fileutil_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            /* best effort cleanup */
        }
    }

    private string WriteFile(string relativePath, byte[] content)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
        return path;
    }

    private static string CaptureStderr(Action action)
    {
        var oldErr = Console.Error;
        try
        {
            using var stderr = new StringWriter();
            Console.SetError(stderr);
            action();
            return stderr.ToString();
        }
        finally
        {
            Console.SetError(oldErr);
        }
    }

    /* ========================================================================= */
    /* fullPath                                                                  */

    /// <summary>Tests that a relative path is resolved to an absolute path.</summary>
    [Fact]
    public void FullPathResolvesRelative()
    {
        Assert.Equal(Path.GetFullPath("game.bin"), FileUtil.FullPath("game.bin"));
    }

    /// <summary>Tests that an absolute path is returned unchanged.</summary>
    [Fact]
    public void FullPathKeepsAbsolute()
    {
        var path = Path.Combine(Path.GetTempPath(), "absolute.bin");
        Assert.Equal(path, FileUtil.FullPath(path));
    }

    /// <summary>Tests that an invalid path is returned unchanged instead of throwing.</summary>
    [Fact]
    public void FullPathReturnsInputOnError()
    {
        /* embedded NUL makes Path.GetFullPath throw; the port logs and falls back */
        const string bad = "game\0.bin";
        Assert.Equal(bad, FileUtil.FullPath(bad));
    }

    /* ========================================================================= */
    /* fileNameWithExtension / fileName / extension / directory                  */

    /// <summary>Tests that the text after the last separator is returned.</summary>
    [Theory]
    [InlineData("folder/game.bin", "game.bin")]
    [InlineData(@"folder\game.bin", "game.bin")]
    [InlineData("game.bin", "game.bin")]
    [InlineData("a/b\\c.txt", "c.txt")] /* the last separator wins, '\' > '/' */
    [InlineData("", "")]
    public void FileNameWithExtensionTakesLastSegment(string path, string expected)
    {
        Assert.Equal(expected, FileUtil.FileNameWithExtension(path));
    }

    /// <summary>Tests that the extension is stripped from the file name.</summary>
    [Theory]
    [InlineData("folder/game.bin", "game")]
    [InlineData("game", "game")]
    [InlineData("a.b.c", "a.b")]
    [InlineData(".hidden", "")] /* the dot is at position 0, so nothing remains */
    public void FileNameStripsExtension(string path, string expected)
    {
        Assert.Equal(expected, FileUtil.FileName(path));
    }

    /// <summary>Tests the extension extraction (last dot anywhere in the path).</summary>
    [Theory]
    [InlineData("game.zip", ".zip")]
    [InlineData("game", "")]
    [InlineData("a.tar.gz", ".gz")]
    [InlineData("dir.with.dot/file", ".dot/file")] /* last dot wins, separators not special */
    public void ExtensionTakesLastDot(string path, string expected)
    {
        Assert.Equal(expected, FileUtil.Extension(path));
    }

    /// <summary>Tests the Windows-style directory extraction.</summary>
    [Theory]
    [InlineData(@"a\b\c", @"a\b")]
    [InlineData("noSep", "noSep")] /* no backslash: input unchanged */
    [InlineData(@"a\", "a")]
    [InlineData("", "")]
    public void DirectoryStripsAfterLastBackslash(string path, string expected)
    {
        Assert.Equal(expected, FileUtil.Directory(path));
    }

    /* ========================================================================= */
    /* openFile / loadFile                                                       */

    /// <summary>Tests that an existing file opens and a missing one returns null.</summary>
    [Fact]
    public void OpenFileHandlesExistingAndMissing()
    {
        var path = WriteFile("game.bin", [1, 2, 3]);

        using (var stream = FileUtil.OpenFile(path))
        {
            Assert.NotNull(stream);
            Assert.Equal(3, stream.Length);
        }

        Assert.Null(FileUtil.OpenFile(Path.Combine(_root, "missing.bin")));
    }

    /// <summary>Tests that LoadFile round-trips file contents.</summary>
    [Fact]
    public void LoadFileRoundTripsContents()
    {
        var bytes = "hello world"u8.ToArray();
        var path = WriteFile("game.bin", bytes);

        Assert.Equal(bytes, FileUtil.LoadFile(path));
        Assert.Null(FileUtil.LoadFile(Path.Combine(_root, "missing.bin")));
    }

    /* ========================================================================= */
    /* loadZippedFile                                                            */

    private static byte[] MakeZip(Action<ZipArchive> fill)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            fill(archive);
        }

        return ms.ToArray();
    }

    /// <summary>Tests that a single-entry zip extracts its first (only) entry.</summary>
    [Fact]
    public void LoadZippedFileExtractsSingleEntry()
    {
        var content = "rom contents"u8.ToArray();
        var path = WriteFile("rom.zip", MakeZip(archive =>
        {
            var entry = archive.CreateEntry("rom.bin");
            using var stream = entry.Open();
            stream.Write(content);
        }));

        var data = FileUtil.LoadZippedFile(path, out var name);

        Assert.Equal(content, data);
        Assert.Equal("rom.bin", name);
    }

    /// <summary>Tests that an empty zip is rejected with a stderr note.</summary>
    [Fact]
    public void LoadZippedFileRejectsEmptyZip()
    {
        var path = WriteFile("empty.zip", MakeZip(_ => { }));

        var stderr = CaptureStderr(() =>
        {
            var data = FileUtil.LoadZippedFile(path, out var name);
            Assert.Null(data);
            Assert.Equal("", name);
        });

        Assert.Contains("Empty zip file", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests that a multi-entry zip falls back to the raw zip bytes.</summary>
    [Fact]
    public void LoadZippedFileReturnsWholeZipForMultipleEntries()
    {
        var zipBytes = MakeZip(archive =>
        {
            archive.CreateEntry("a.txt");
            archive.CreateEntry("b.txt");
        });
        var path = WriteFile("multi.zip", zipBytes);

        var stderr = CaptureStderr(() =>
        {
            var data = FileUtil.LoadZippedFile(path, out var name);
            Assert.Equal(zipBytes, data); /* the entire zip file, not an entry */
            Assert.Equal("", name);
        });

        Assert.Contains("returning entire zip file", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests that a zip containing only a directory is rejected.</summary>
    [Fact]
    public void LoadZippedFileRejectsDirectoryOnly()
    {
        var path = WriteFile("dir.zip", MakeZip(archive =>
        {
            archive.CreateEntry("sub/");
        }));

        var stderr = CaptureStderr(() =>
        {
            var data = FileUtil.LoadZippedFile(path, out _);
            Assert.Null(data);
        });

        Assert.Contains("only contains a directory", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests that a missing zip file returns null.</summary>
    [Fact]
    public void LoadZippedFileMissingReturnsNull()
    {
        Assert.Null(FileUtil.LoadZippedFile(Path.Combine(_root, "missing.zip"), out var name));
        Assert.Equal("", name);
    }

    /* ========================================================================= */
    /* loadZippedFileToTemp — the disk-backed variant for >= 2 GiB entries       */

    /// <summary>Tests that the temp variant extracts a single entry to disk.</summary>
    [Fact]
    public void LoadZippedFileToTempExtractsSingleEntry()
    {
        var content = "rom contents"u8.ToArray();
        var path = WriteFile("rom.zip", MakeZip(archive =>
        {
            var entry = archive.CreateEntry("rom.bin");
            using var stream = entry.Open();
            stream.Write(content);
        }));

        var tempPath = FileUtil.LoadZippedFileToTemp(path, out var name);
        try
        {
            Assert.NotNull(tempPath);
            Assert.Equal("rom.bin", name);
            Assert.Equal(content, File.ReadAllBytes(tempPath));
        }
        finally
        {
            if (tempPath != null)
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>Tests that the temp variant rejects an empty zip.</summary>
    [Fact]
    public void LoadZippedFileToTempRejectsEmptyZip()
    {
        var path = WriteFile("empty.zip", MakeZip(_ => { }));

        var stderr = CaptureStderr(() =>
        {
            var tempPath = FileUtil.LoadZippedFileToTemp(path, out var name);
            Assert.Null(tempPath);
            Assert.Equal("", name);
        });

        Assert.Contains("Empty zip file", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests that the temp variant falls back to the whole zip file for multiple entries.</summary>
    [Fact]
    public void LoadZippedFileToTempReturnsWholeZipForMultipleEntries()
    {
        var zipBytes = MakeZip(archive =>
        {
            archive.CreateEntry("a.txt");
            archive.CreateEntry("b.txt");
        });
        var path = WriteFile("multi.zip", zipBytes);

        var stderr = CaptureStderr(() =>
        {
            var tempPath = FileUtil.LoadZippedFileToTemp(path, out var name);
            try
            {
                Assert.NotNull(tempPath);
                Assert.Equal("", name);
                Assert.Equal(zipBytes, File.ReadAllBytes(tempPath));
            }
            finally
            {
                if (tempPath != null)
                {
                    File.Delete(tempPath);
                }
            }
        });

        Assert.Contains("returning entire zip file", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests that the temp variant rejects a directory-only zip.</summary>
    [Fact]
    public void LoadZippedFileToTempRejectsDirectoryOnly()
    {
        var path = WriteFile("dir.zip", MakeZip(archive =>
        {
            archive.CreateEntry("sub/");
        }));

        var stderr = CaptureStderr(() =>
        {
            var tempPath = FileUtil.LoadZippedFileToTemp(path, out _);
            Assert.Null(tempPath);
        });

        Assert.Contains("only contains a directory", stderr, StringComparison.Ordinal);
    }
}
