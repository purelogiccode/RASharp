// Tests for the `RASharp fetch-db` subcommand (a RASharp extension — not
// part of the RAHasher 1.8.3 parity surface). It downloads (or copies) a
// RetroAchievements database snapshot, validates it, and saves it
// atomically. HTTP is injected via RaApi.SendGetOverride.

using RASharp.Cli;

namespace RASharp.Tests;

/// <summary>Tests for the `RASharp fetch-db` subcommand (a RASharp extension — not part of the RAHasher 1.8.3 parity surface). It downloads (or copies) a RetroAchievements datab</summary>
public class TestFetchDb : IDisposable
{
    private readonly string _root;

    public TestFetchDb()
    {
        _root = Path.Combine(Path.GetTempPath(), "rasharp_fetchdb_test_" + Guid.NewGuid().ToString("N")[..8]);
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

    private static (int ExitCode, string StdOut, string StdErr) RunFetchDb(params string[] args)
    {
        var oldOut = Console.Out;
        var oldErr = Console.Error;
        try
        {
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var exit = FetchDbCommand.Run(args);
            return (exit, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetError(oldErr);
        }
    }

    private const string ValidDbJson =
        "[\n" +
        "  {\"ID\": 1, \"Title\": \"Game One\", \"ConsoleID\": 4, \"ConsoleName\": \"Game Boy\", " +
        "\"ImageIcon\": \"\", \"NumAchievements\": 5, \"Points\": 50, \"DateModified\": \"2024-01-01\", " +
        "\"Hashes\": [\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"]},\n" +
        "  {\"ID\": 2, \"Title\": \"Game Two\", \"ConsoleID\": 7, \"ConsoleName\": \"NES/Famicom\", " +
        "\"ImageIcon\": \"\", \"NumAchievements\": 3, \"Points\": 30, \"DateModified\": \"2024-01-01\", " +
        "\"Hashes\": [\"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\", \"cccccccccccccccccccccccccccccccc\"]}\n" +
        "]\n";

    /* ========================================================================= */

/// <summary>Tests copying a local database file.</summary>
    [Fact]
    public void FetchDbLocalCopy()
    {
        var source = Path.Combine(_root, "src.json");
        var dest = Path.Combine(_root, "out.json");
        File.WriteAllText(source, ValidDbJson);

        var (exit, stdout, _) = RunFetchDb(source, "--out", dest);

        Assert.Equal(0, exit);
        Assert.True(File.Exists(dest));
        Assert.Equal(ValidDbJson, File.ReadAllText(dest));
        Assert.Contains("Saved", stdout, StringComparison.Ordinal);
        Assert.Contains("2 game(s), 3 hash(es)", stdout, StringComparison.Ordinal);
    }

    /* a malformed download must never clobber an existing snapshot */
/// <summary>Tests that an invalid database is refused and nothing is written.</summary>
    [Fact]
    public void FetchDbRefusesInvalidJson()
    {
        var source = Path.Combine(_root, "src.json");
        var dest = Path.Combine(_root, "out.json");
        File.WriteAllText(source, "{\"not\": \"an array\"}");

        var (exit, _, stderr) = RunFetchDb(source, "--out", dest);

        Assert.Equal(1, exit);
        Assert.Contains("Refusing to save", stderr, StringComparison.Ordinal);
        Assert.False(File.Exists(dest));
    }

/// <summary>Tests that an empty game list is refused.</summary>
    [Fact]
    public void FetchDbRefusesEmptyList()
    {
        var source = Path.Combine(_root, "src.json");
        var dest = Path.Combine(_root, "out.json");
        File.WriteAllText(source, "[]");

        var (exit, _, stderr) = RunFetchDb(source, "--out", dest);

        Assert.Equal(1, exit);
        Assert.Contains("no games with hashes", stderr, StringComparison.Ordinal);
        Assert.False(File.Exists(dest));
    }

/// <summary>Tests an HTTP download via the injected client.</summary>
    [Fact]
    public void FetchDbHttpDownload()
    {
        var dest = Path.Combine(_root, "out.json");
        string? requestedUrl = null;
        RaApi.SendGetOverride = url =>
        {
            requestedUrl = url;
            return ValidDbJson;
        };

        try
        {
            var (exit, stdout, _) = RunFetchDb("https://example.com/db/RetroAchievements.json", "--out", dest);

            Assert.Equal(0, exit);
            Assert.Equal("https://example.com/db/RetroAchievements.json", requestedUrl);
            Assert.True(File.Exists(dest));
            Assert.Contains("from https://example.com/db/RetroAchievements.json", stdout, StringComparison.Ordinal);
        }
        finally
        {
            RaApi.SendGetOverride = null;
        }
    }

/// <summary>Tests a failed download.</summary>
    [Fact]
    public void FetchDbDownloadFailure()
    {
        var dest = Path.Combine(_root, "out.json");
        RaApi.SendGetOverride = _ => null;

        try
        {
            var (exit, _, stderr) = RunFetchDb("https://example.com/nope.json", "--out", dest);

            Assert.Equal(1, exit);
            Assert.Contains("Cannot download", stderr, StringComparison.Ordinal);
            Assert.False(File.Exists(dest));
        }
        finally
        {
            RaApi.SendGetOverride = null;
        }
    }

    /* the output is written atomically: a temp file must not linger */
/// <summary>Tests that no temp file is left behind.</summary>
    [Fact]
    public void FetchDbNoTempLeftBehind()
    {
        var source = Path.Combine(_root, "src.json");
        var dest = Path.Combine(_root, "out.json");
        File.WriteAllText(source, ValidDbJson);

        var (exit, _, _) = RunFetchDb(source, "--out", dest);

        Assert.Equal(0, exit);
        Assert.False(File.Exists(dest + ".tmp"));
    }

/// <summary>Tests fetch-db --help and missing source argument.</summary>
    [Fact]
    public void FetchDbHelpAndMissingSource()
    {
        var (exit, stdout, _) = RunFetchDb("--help");
        Assert.Equal(0, exit);
        Assert.Contains("Usage: RASharp fetch-db", stdout, StringComparison.Ordinal);

        var (exit2, stdout2, _) = RunFetchDb();
        Assert.Equal(1, exit2);
        Assert.Contains("Usage: RASharp fetch-db", stdout2, StringComparison.Ordinal);
    }
}
