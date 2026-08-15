// Tests for Program.GenerateHashes — the hashing core shared by the
// legacy single-file flow and the identify subcommand. It uses the default
// (real-file) filereader, so these tests operate on a temp directory and
// reset the global custom filereader that other test classes install.

using System.IO.Compression;
using System.Security.Cryptography;
using RASharp.Cli;

namespace RASharp.Tests;

/// <summary>Tests for Program.GenerateHashes — the hashing core shared by the legacy single-file flow and the identify subcommand. It uses the default (real-file) filereader</summary>
public class TestProgramHashes : IDisposable
{
    private readonly string _root;

    public TestProgramHashes()
    {
        _root = Path.Combine(Path.GetTempPath(), "rasharp_program_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
        /* these tests use the default (real-file) filereader, not the mock */
        RcHash.InitCustomFilereader(null);
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

    private static string Md5Hex(byte[] data)
    {
        return Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant();
    }

    /// <summary>Tests the whole-file hash path with a real file.</summary>
    [Fact]
    public void GenerateHashesHashesFullFile()
    {
        var content = "some game data"u8.ToArray();
        var path = WriteFile("game.bin", content);

        var hashes = Program.GenerateHashes((int)ConsoleIds.RcConsolePc8800, path);

        Assert.Single(hashes);
        Assert.Equal(ConsoleIds.RcConsolePc8800, hashes[0].ConsoleId);
        Assert.Equal(Md5Hex(content), hashes[0].Hash);
    }

    /// <summary>Tests the zip special case — a single-entry zip hashes its extracted content.</summary>
    [Fact]
    public void GenerateHashesExtractsZipContent()
    {
        var content = "zipped rom"u8.ToArray();
        var zipPath = WriteFile("game.zip", MakeZip(archive =>
        {
            var entry = archive.CreateEntry("rom.bin");
            using var stream = entry.Open();
            stream.Write(content);
        }));

        var hashes = Program.GenerateHashes((int)ConsoleIds.RcConsolePc8800, zipPath);

        Assert.Single(hashes);
        Assert.Equal(ConsoleIds.RcConsolePc8800, hashes[0].ConsoleId);
        Assert.Equal(Md5Hex(content), hashes[0].Hash); /* the entry, not the zip bytes */
    }

    /// <summary>Tests the '?' auto-detect path (consoleId above the table maximum).</summary>
    [Fact]
    public void GenerateHashesAutoDetectsByExtension()
    {
        var content = TestDataGen.GenerateGenericFile(131072);
        var path = WriteFile("game.d88", content);

        var hashes = Program.GenerateHashes(ConsoleIds.RcConsoleMax + 1, path);

        Assert.Single(hashes);
        Assert.Equal(ConsoleIds.RcConsolePc8800, hashes[0].ConsoleId);
        Assert.Equal(Md5Hex(content), hashes[0].Hash);
    }

    /// <summary>Tests that a missing file yields no hashes.</summary>
    [Fact]
    public void GenerateHashesMissingFileReturnsEmpty()
    {
        var hashes = Program.GenerateHashes((int)ConsoleIds.RcConsolePc8800, Path.Combine(_root, "missing.bin"));

        Assert.Empty(hashes);
    }

    private static byte[] MakeZip(Action<ZipArchive> fill)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            fill(archive);
        }

        return ms.ToArray();
    }
}
