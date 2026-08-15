// Tests for the `RetroAchievementsSharp identify` subcommand (a RetroAchievementsSharp extension — not
// part of the RAHasher 1.8.3 parity surface). It hashes a single file with
// an explicit console and resolves the hash against a local
// RetroAchievements.json snapshot (--db) or the live RA API (--user/
// --api-key, HTTP injected via RaApi.SendGetOverride).

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using RetroAchievementsSharp.Cli;

namespace RetroAchievementsSharp.Tests;

/// <summary>Tests for the `RetroAchievementsSharp identify` subcommand (a RetroAchievementsSharp extension — not part of the RAHasher 1.8.3 parity surface). It hashes a single file with an explicit console and r</summary>
public class TestIdentify : IDisposable
{
    private readonly string _root;

    public TestIdentify()
    {
        /* identify hashes real files; make sure a mock filereader registered
         * by another test class is not in effect */
        HashEngine.ResetFilereader();
        _root = Path.Combine(Path.GetTempPath(), "rasharp_identify_test_" + Guid.NewGuid().ToString("N")[..8]);
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

    private static string Md5Hex(byte[] data)
    {
        return Convert.ToHexString(MD5.HashData(data)).ToLowerInvariant();
    }

    private static (int ExitCode, string StdOut, string StdErr) RunIdentify(params string[] args)
    {
        var oldOut = Console.Out;
        var oldErr = Console.Error;
        try
        {
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var exit = IdentifyCommand.Run(args);
            return (exit, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetError(oldErr);
        }
    }

    private string WriteFile(string relativePath, byte[] content)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
        return path;
    }

    private string WriteDb(params (int Id, string Title, string ConsoleName, string[] Hashes)[] games)
    {
        var entries = games.Select(game =>
            $"{{\"ID\": {game.Id}, \"Title\": \"{game.Title}\", \"ConsoleID\": 1, " +
            $"\"ConsoleName\": \"{game.ConsoleName}\", \"ImageIcon\": \"\", " +
            $"\"NumAchievements\": 5, \"Points\": 50, \"DateModified\": \"2024-01-01\", " +
            $"\"Hashes\": [\"{string.Join("\", \"", game.Hashes)}\"]}}");
        var path = Path.Combine(_root, "ra.json");
        File.WriteAllText(path, "[\n" + string.Join(",\n", entries) + "\n]");
        return path;
    }

    /* ========================================================================= */

    /// <summary>Tests a local lookup that finds the game.</summary>
    [Fact]
    public void IdentifyLocalMatch()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var path = WriteFile("game.gb", gb);
        var dbPath = WriteDb((1, "Test Game One", "Game Boy", [Md5Hex(gb)]));

        var (exit, stdout, stderr) = RunIdentify("GB", path, "--db", dbPath);

        Assert.Equal(0, exit);
        Assert.Contains($"{Md5Hex(gb)} GB {path} => Test Game One (ID 1)", stdout, StringComparison.Ordinal);
        Assert.Contains("identify: local lookup in", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests a local lookup with no match.</summary>
    [Fact]
    public void IdentifyLocalNoMatch()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var path = WriteFile("game.gb", gb);
        var dbPath = WriteDb((1, "Other Game", "Game Boy", ["00000000000000000000000000000000"]));

        var (exit, stdout, _) = RunIdentify("GB", path, "--db", dbPath);

        Assert.Equal(1, exit);
        Assert.Contains("=> not found", stdout, StringComparison.Ordinal);
    }

    /* identify hashes the zip CONTENT (explicit console), unlike the scan
     * auto-detect path which hashes the zip filename — so zips can match */
    /// <summary>Tests that a zip is identified by its content hash.</summary>
    [Fact]
    public void IdentifyZipContentMatch()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var zipPath = Path.Combine(_root, "game.zip");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("game.gb");
            using var stream = entry.Open();
            stream.Write(gb);
        }

        var dbPath = WriteDb((7, "Zipped Game", "Game Boy", [Md5Hex(gb)]));

        var (exit, stdout, _) = RunIdentify("GB", zipPath, "--db", dbPath);

        Assert.Equal(0, exit);
        Assert.Contains($"{Md5Hex(gb)} GB {zipPath} => Zipped Game (ID 7)", stdout, StringComparison.Ordinal);
    }

    /* "?" auto-detection must resolve the console per file and still match */
    /// <summary>Tests auto-detection with the '?' system argument.</summary>
    [Fact]
    public void IdentifyAutoDetect()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var path = WriteFile("game.gb", gb);
        var dbPath = WriteDb((1, "Test Game One", "Game Boy", [Md5Hex(gb)]));

        var (exit, stdout, _) = RunIdentify("?", path, "--db", dbPath);

        Assert.Equal(0, exit);
        Assert.Contains($"{Md5Hex(gb)} GB {path} => Test Game One (ID 1)", stdout, StringComparison.Ordinal);
    }

    /* the live lookup fetches API_GetGameList for the file's console and
     * matches the hash in the response — the HTTP call is injected */
    /// <summary>Tests the live API lookup path.</summary>
    [Fact]
    public void IdentifyLiveLookup()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var hash = Md5Hex(gb);
        var path = WriteFile("game.gb", gb);
        var gameListJson =
            "[{\"ID\": 42, \"Title\": \"Live Game\", \"ConsoleID\": 4, \"ConsoleName\": \"Game Boy\", " +
            "\"ImageIcon\": \"\", \"NumAchievements\": 9, \"Points\": 40, \"DateModified\": \"2024-01-01\", " +
            $"\"Hashes\": [\"{hash}\"]}}]";

        string? requestedUrl = null;
        RaApi.SendGetOverride = url =>
        {
            requestedUrl = url;
            return gameListJson;
        };

        try
        {
            var (exit, stdout, stderr) = RunIdentify("GB", path, "--user", "testuser", "--api-key", "testkey");

            Assert.Equal(0, exit);
            Assert.Contains($"{hash} GB {path} => Live Game (ID 42)", stdout, StringComparison.Ordinal);
            Assert.Contains("live lookup", stderr, StringComparison.Ordinal);
            Assert.NotNull(requestedUrl);
            Assert.Contains("API_GetGameList.php", requestedUrl, StringComparison.Ordinal);
            Assert.Contains("u=testuser", requestedUrl, StringComparison.Ordinal);
            Assert.Contains("i=4", requestedUrl, StringComparison.Ordinal);
        }
        finally
        {
            RaApi.SendGetOverride = null;
        }
    }

    /// <summary>Tests the live lookup when the API call fails.</summary>
    [Fact]
    public void IdentifyLiveApiFailure()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var path = WriteFile("game.gb", gb);

        RaApi.SendGetOverride = _ => null;
        try
        {
            var (exit, _, stderr) = RunIdentify("GB", path, "--user", "u", "--api-key", "k");

            Assert.Equal(1, exit);
            Assert.Contains("Failed to fetch the game list", stderr, StringComparison.Ordinal);
        }
        finally
        {
            RaApi.SendGetOverride = null;
        }
    }

    /* without credentials or a database file there is nothing to look up */
    /// <summary>Tests that identify explains the missing lookup source.</summary>
    [Fact]
    public void IdentifyNoLookupSource()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var path = WriteFile("game.gb", gb);

        var (exit, _, stderr) = RunIdentify("GB", path);

        Assert.Equal(1, exit);
        Assert.Contains("Cannot find", stderr, StringComparison.Ordinal);
        Assert.Contains("--db", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests an unknown system argument.</summary>
    [Fact]
    public void IdentifyUnknownSystem()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var path = WriteFile("game.gb", gb);

        var (exit, _, stderr) = RunIdentify("NOPE", path, "--db", WriteDb());

        Assert.Equal(1, exit);
        Assert.Contains("Unknown system", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests a missing file argument.</summary>
    [Fact]
    public void IdentifyMissingFile()
    {
        var (exit, _, stderr) = RunIdentify("GB", Path.Combine(_root, "nope.gb"), "--db", WriteDb());

        Assert.Equal(1, exit);
        Assert.Contains("No such file", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests the json output format.</summary>
    [Fact]
    public void IdentifyJsonFormat()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var path = WriteFile("game.gb", gb);
        var dbPath = WriteDb((1, "Test Game One", "Game Boy", [Md5Hex(gb)]));

        var (exit, stdout, _) = RunIdentify("GB", path, "--db", dbPath, "-f", "json");

        Assert.Equal(0, exit);
        using var doc = JsonDocument.Parse(stdout);
        var rows = doc.RootElement.EnumerateArray().ToArray();
        Assert.Single(rows);
        Assert.Equal("GB", rows[0].GetProperty("console").GetString());
        Assert.Equal(4, rows[0].GetProperty("consoleId").GetInt32());
        var games = rows[0].GetProperty("games");
        Assert.Equal(1, games.GetArrayLength());
        Assert.Equal(1, games[0].GetProperty("id").GetInt32());
        Assert.Equal("Test Game One", games[0].GetProperty("title").GetString());
    }
}
