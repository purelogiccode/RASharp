// Tests for the `RASharp scan` subcommand (a RASharp extension — not part
// of the RAHasher 1.8.3 parity surface). The scan command enumerates real
// directories and hashes real files, so the engine's default (non-mock)
// filereader must be in effect — a prior test class may have registered the
// in-memory mock filereader.

using System.Security.Cryptography;
using System.Text.Json;
using RASharp.Cli;
using RASharp.Core;
using RASharp.Tests.Parity;

namespace RASharp.Tests;

/// <summary>Tests for the `RASharp scan` subcommand (a RASharp extension — not part of the RAHasher 1.8.3 parity surface). The scan command enumerates real directories and h</summary>
public class TestScan : IDisposable
{
    private readonly string _root;

    public TestScan()
    {
        HashEngine.ResetFilereader();
        _root = Path.Combine(Path.GetTempPath(), "rasharp_scan_test_" + Guid.NewGuid().ToString("N")[..8]);
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

    private static (int ExitCode, string StdOut, string StdErr) RunScan(params string[] args)
    {
        var oldOut = Console.Out;
        var oldErr = Console.Error;
        try
        {
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var exit = ScanCommand.Run(args);
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

    private string WriteText(string relativePath, string content)
    {
        return WriteFile(relativePath, System.Text.Encoding.ASCII.GetBytes(content));
    }

    /* writes a synthetic RetroAchievements.json (the DataFetcher snapshot
     * schema: ID, Title, ConsoleID, ConsoleName, ImageIcon, NumAchievements,
     * Points, DateModified, Hashes[]) at the _root — outside the scanned
     * roms/ subfolder */
    private string WriteDb(string relativePath, params (int Id, string Title, string ConsoleName, string[] Hashes)[] games)
    {
        var entries = games.Select(game =>
            $"{{\"ID\": {game.Id}, \"Title\": \"{game.Title}\", \"ConsoleID\": 1, " +
            $"\"ConsoleName\": \"{game.ConsoleName}\", \"ImageIcon\": \"\", " +
            $"\"NumAchievements\": 5, \"Points\": 50, \"DateModified\": \"2024-01-01\", " +
            $"\"Hashes\": [\"{string.Join("\", \"", game.Hashes)}\"]}}");
        var json = "[\n" + string.Join(",\n", entries) + "\n]";
        return WriteFile(relativePath, System.Text.Encoding.UTF8.GetBytes(json));
    }

    /* ========================================================================= */

    /* a .gb file hashes as whole-file MD5; a .nes as the NES cartridge hash;
     * an unmapped extension falls back to a Game Boy whole-file hash */
    private const string Nes32KHash = "6a2305a2b6675a97ff792709be1ca857"; /* pinned vector, TestHashNes32KWithHeader */

    /// <summary>Tests scanning a directory tree recursively, text format.</summary>
    [Fact]
    public void ScanRecursiveText()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var nes = TestDataGen.GenerateNesFile(32, true, out _);
        var txt = "hello"u8.ToArray();
        WriteFile("game.gb", gb);
        WriteFile(Path.Combine("sub", "folder", "game.nes"), nes);
        WriteFile("notes.txt", txt);

        var (exit, stdout, stderr) = RunScan(_root);

        Assert.Equal(0, exit);
        var gbHash = Md5Hex(gb);
        var txtHash = Md5Hex(txt);
        var expected = new[]
        {
            $"{gbHash} GB game.gb",
            $"{txtHash} GB notes.txt",
            $"{Nes32KHash} NES {Path.Combine("sub", "folder", "game.nes")}"
        };
        Assert.Equal(expected, stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
        Assert.Contains("Scanned 3 file(s): 3 hashed, 0 failed", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests scanning with --no-recursive.</summary>
    [Fact]
    public void ScanNoRecursive()
    {
        WriteFile("game.gb", TestDataGen.GenerateGenericFile(131072));
        WriteFile(Path.Combine("sub", "game.nes"), TestDataGen.GenerateNesFile(32, true, out _));

        var (exit, stdout, _) = RunScan("--no-recursive", _root);

        Assert.Equal(0, exit);
        Assert.Single(stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        Assert.Contains("game.gb", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("game.nes", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests scanning a single file argument.</summary>
    [Fact]
    public void ScanSingleFile()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var path = WriteFile("game.gb", gb);

        var (exit, stdout, _) = RunScan(path);

        Assert.Equal(0, exit);
        Assert.Contains($"{Md5Hex(gb)} GB {path}", stdout, StringComparison.Ordinal);
    }

    /* two same-named files passed as single-file arguments must keep distinct
     * row paths (each shown relative to its own parent directory) */
    /// <summary>Tests that single-file arguments with identical names stay distinct.</summary>
    [Fact]
    public void ScanSingleFilesKeepDistinctPaths()
    {
        var gbA = TestDataGen.GenerateGenericFile(131072);
        var gbB = TestDataGen.GenerateGenericFile(65536);
        var pathA = WriteFile(Path.Combine("a", "x.gb"), gbA);
        var pathB = WriteFile(Path.Combine("b", "x.gb"), gbB);

        var (exit, stdout, _) = RunScan(pathA, pathB);

        Assert.Equal(0, exit);
        var lines = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(
            [$"{Md5Hex(gbA)} GB {pathA}", $"{Md5Hex(gbB)} GB {pathB}"],
            lines, StringComparer.Ordinal);
    }

    /// <summary>Tests the csv output format.</summary>
    [Fact]
    public void ScanCsvFormat()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        WriteFile("game.gb", gb);

        var (exit, stdout, _) = RunScan("--format", "csv", _root);

        Assert.Equal(0, exit);
        var lines = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("file,console,hash", lines[0]);
        Assert.Equal($"game.gb,GB,{Md5Hex(gb)}", lines[1]);
    }

    /* RFC 4180 quoting: a comma in the file name must not break the csv */
    /// <summary>Tests csv quoting of file names containing commas.</summary>
    [Fact]
    public void ScanCsvQuotesCommaInFilename()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        WriteFile("game,1.gb", gb);

        var (exit, stdout, _) = RunScan("--format", "csv", _root);

        Assert.Equal(0, exit);
        Assert.Contains($"\"game,1.gb\",GB,{Md5Hex(gb)}", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests the json output format.</summary>
    [Fact]
    public void ScanJsonFormat()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var nes = TestDataGen.GenerateNesFile(32, true, out _);
        WriteFile("game.gb", gb);
        WriteFile(Path.Combine("sub", "game.nes"), nes);

        var (exit, stdout, _) = RunScan("-f", "json", _root);

        Assert.Equal(0, exit);
        using var doc = JsonDocument.Parse(stdout);
        var rows = doc.RootElement.EnumerateArray().ToArray();
        Assert.Equal(2, rows.Length);

        var gbRow = rows.Single(row => string.Equals(row.GetProperty("file").GetString(), "game.gb", StringComparison.Ordinal));
        Assert.Equal("GB", gbRow.GetProperty("console").GetString());
        Assert.Equal(4, gbRow.GetProperty("consoleId").GetInt32());
        Assert.Equal(Md5Hex(gb), gbRow.GetProperty("hash").GetString());

        var nesRow = rows.Single(row => string.Equals(row.GetProperty("file").GetString(), Path.Combine("sub", "game.nes"), StringComparison.Ordinal));
        Assert.Equal("NES", nesRow.GetProperty("console").GetString());
        Assert.Equal(7, nesRow.GetProperty("consoleId").GetInt32());
        Assert.Equal(Nes32KHash, nesRow.GetProperty("hash").GetString());
    }

    /* a well-formed .cue whose bin file is missing fails every candidate
     * disc console, so the row carries the failure marker and the exit
     * code is 1 — while the other rows are still emitted */
    /// <summary>Tests that files failing every console are marked and fail the run.</summary>
    [Fact]
    public void ScanFailureRow()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        WriteFile("game.gb", gb);
        WriteText("broken.cue", "FILE \"nope.bin\" BINARY\r\n  TRACK 01 MODE2/2352\r\n    INDEX 01 00:00:00\r\n");

        var (exit, stdout, stderr) = RunScan(_root);

        Assert.Equal(1, exit);
        var lines = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Contains($"{Md5Hex(gb)} GB game.gb", lines, StringComparer.Ordinal);
        Assert.Contains("???????????????????????????????? ? broken.cue", lines, StringComparer.Ordinal);
        Assert.Contains("Scanned 2 file(s): 1 hashed, 1 failed", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests scan --help.</summary>
    [Fact]
    public void ScanHelp()
    {
        var (exit, stdout, _) = RunScan("--help");

        Assert.Equal(0, exit);
        Assert.Contains("Usage: RASharp scan", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests an unknown output format.</summary>
    [Fact]
    public void ScanUnknownFormat()
    {
        var (exit, _, stderr) = RunScan("--format", "xml", _root);

        Assert.Equal(1, exit);
        Assert.Contains("Unknown scan format", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests a missing path argument.</summary>
    [Fact]
    public void ScanMissingPath()
    {
        var (exit, _, stderr) = RunScan(Path.Combine(_root, "does-not-exist"));

        Assert.Equal(1, exit);
        Assert.Contains("No such file or directory", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests scan with no paths prints usage and fails.</summary>
    [Fact]
    public void ScanNoPaths()
    {
        var (exit, stdout, _) = RunScan();

        Assert.Equal(1, exit);
        Assert.Contains("Usage: RASharp scan", stdout, StringComparison.Ordinal);
    }

    /* end-to-end: the Program.Run dispatch must route "scan" to the
     * subcommand when the real CLI binary is invoked (the parity harness
     * runs the freshly built RASharp.exe) */
    /// <summary>Tests that the real CLI binary dispatches the scan subcommand.</summary>
    [Fact]
    public void ScanDispatchThroughCliExe()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; /* the parity harness locates RASharp.exe (Windows apphost) */
        }

        var gb = TestDataGen.GenerateGenericFile(131072);
        WriteFile("game.gb", gb);

        var result = ParityHarness.Run(ParityHarness.CliPath, ["scan", _root], _root);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains($"{Md5Hex(gb)} GB game.gb", ParityHarness.ToText(result.StdOut), StringComparison.Ordinal);
    }

    /* ========================================================================= */
    /* --match (RetroAchievements database lookup)                               */

    private const string RomsDir = "roms";

    private string RomsPath => Path.Combine(_root, RomsDir);

    private string DbPath => Path.Combine(_root, "ra.json");

    /// <summary>Tests that --match annotates text rows whose hash belongs to a game.</summary>
    [Fact]
    public void ScanMatchAnnotatesText()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteDb("ra.json", (1, "Test Game One", "Game Boy", [Md5Hex(gb)]));

        var (exit, stdout, _) = RunScan("--match", DbPath, RomsPath);

        Assert.Equal(0, exit);
        Assert.Contains($"{Md5Hex(gb)} GB game.gb => Test Game One (ID 1)", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests that unmatched rows stay unannotated.</summary>
    [Fact]
    public void ScanMatchNoMatchLeavesRow()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteDb("ra.json", (1, "Other Game", "Game Boy", ["00000000000000000000000000000000"]));

        var (exit, stdout, _) = RunScan("--match", DbPath, RomsPath);

        Assert.Equal(0, exit);
        Assert.Contains($"{Md5Hex(gb)} GB game.gb", stdout, StringComparison.Ordinal);
        Assert.DoesNotContain("=>", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests that a hash shared by several games lists them all.</summary>
    [Fact]
    public void ScanMatchMultipleGamesText()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var hash = Md5Hex(gb);
        WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteDb("ra.json",
            (3, "Dup A", "Game Boy", [hash]),
            (4, "Dup B", "Game Boy", [hash]));

        var (exit, stdout, _) = RunScan("--match", DbPath, RomsPath);

        Assert.Equal(0, exit);
        Assert.Contains($"{hash} GB game.gb => Dup A (ID 3) (+1 more)", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests the csv columns added by --match.</summary>
    [Fact]
    public void ScanMatchCsv()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var hash = Md5Hex(gb);
        WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteDb("ra.json", (1, "Test Game One", "Game Boy", [hash]));

        var (exit, stdout, _) = RunScan("--format", "csv", "--match", DbPath, RomsPath);

        Assert.Equal(0, exit);
        var lines = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("file,console,hash,game_id,game_title,game_matches", lines[0]);
        Assert.Contains($"game.gb,GB,{hash},1,Test Game One,1", lines, StringComparer.Ordinal);
    }

    /* a hash shared by two games reports the first game plus the total count */
    /// <summary>Tests the csv multi-match count column.</summary>
    [Fact]
    public void ScanMatchCsvMultipleGames()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var hash = Md5Hex(gb);
        WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteDb("ra.json",
            (3, "Dup A", "Game Boy", [hash]),
            (4, "Dup B", "Game Boy", [hash]));

        var (exit, stdout, _) = RunScan("--format", "csv", "--match", DbPath, RomsPath);

        Assert.Equal(0, exit);
        Assert.Contains($"game.gb,GB,{hash},3,Dup A,2", stdout, StringComparison.Ordinal);
    }

    /* a malformed entry (numbers as strings, missing fields) must not abort
     * loading — the entry is skipped for lookups, the rest still match */
    /// <summary>Tests that malformed database entries do not abort loading.</summary>
    [Fact]
    public void ScanMatchToleratesMalformedEntries()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var hash = Md5Hex(gb);
        WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteFile("ra.json", System.Text.Encoding.UTF8.GetBytes(
            "[\n" +
            "  {\"ID\": 1, \"Title\": \"Good Game\", \"ConsoleName\": \"Game Boy\", " +
            "\"NumAchievements\": \"28\", \"Points\": 3.5, \"DateModified\": \"x\", " +
            "\"Hashes\": [\"" + hash + "\"]},\n" +
            "  {\"ID\": \"not-a-number\", \"Title\": \"Broken\", \"Hashes\": [\"" + hash + "\"]}\n" +
            "]\n"));

        var (exit, stdout, _) = RunScan("--match", DbPath, RomsPath);

        Assert.Equal(0, exit);
        Assert.Contains($"{hash} GB game.gb => Good Game (ID 1)", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests the games array added to json rows by --match.</summary>
    [Fact]
    public void ScanMatchJson()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var nes = TestDataGen.GenerateNesFile(32, true, out _);
        var hash = Md5Hex(gb);
        WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteFile(Path.Combine(RomsDir, "other.nes"), nes);
        WriteDb("ra.json", (1, "Test Game One", "Game Boy", [hash]));

        var (exit, stdout, _) = RunScan("-f", "json", "--match", DbPath, RomsPath);

        Assert.Equal(0, exit);
        using var doc = JsonDocument.Parse(stdout);
        var rows = doc.RootElement.EnumerateArray().ToArray();

        var matched = rows.Single(row => string.Equals(row.GetProperty("file").GetString(), "game.gb", StringComparison.Ordinal));
        var games = matched.GetProperty("games");
        Assert.Equal(1, games.GetArrayLength());
        Assert.Equal(1, games[0].GetProperty("id").GetInt32());
        Assert.Equal("Test Game One", games[0].GetProperty("title").GetString());
        Assert.Equal("Game Boy", games[0].GetProperty("consoleName").GetString());

        var unmatched = rows.Single(row => string.Equals(row.GetProperty("file").GetString(), "other.nes", StringComparison.Ordinal));
        Assert.Equal(0, unmatched.GetProperty("games").GetArrayLength());
    }

    /// <summary>Tests that a missing or malformed database fails the run.</summary>
    [Fact]
    public void ScanMatchMissingDatabase()
    {
        var (exit, _, stderr) = RunScan("--match", Path.Combine(_root, "nope.json"), RomsPath);

        Assert.Equal(1, exit);
        Assert.Contains("Cannot load RetroAchievements database", stderr, StringComparison.Ordinal);
    }

    /* ========================================================================= */
    /* --move                                                                   */

    /// <summary>Tests that matched files move into the console-key subfolder of the destination and unmatched stay.</summary>
    [Fact]
    public void ScanMoveMatchedFiles()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var nes = TestDataGen.GenerateNesFile(32, true, out _);
        var txt = "notes"u8.ToArray();
        var gbPath = WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteFile(Path.Combine(RomsDir, "sub", "folder", "game.nes"), nes);
        WriteFile(Path.Combine(RomsDir, "notes.txt"), txt);
        WriteDb("ra.json",
            (1, "Test Game One", "Game Boy", [Md5Hex(gb)]),
            (2, "Test Game Two", "NES/Famicom", [Nes32KHash]));
        var dest = Path.Combine(_root, "Compatible Games");

        var (exit, stdout, stderr) = RunScan("--match", DbPath, "--move", dest, RomsPath);

        Assert.Equal(0, exit);
        Assert.False(File.Exists(gbPath));
        Assert.True(File.Exists(Path.Combine(dest, "GB", "game.gb")));
        Assert.True(File.Exists(Path.Combine(dest, "NES", "game.nes")));
        /* the unmatched .txt stays in place */
        Assert.True(File.Exists(Path.Combine(RomsPath, "notes.txt")));
        Assert.Contains("Moved 2 file(s) to", stderr, StringComparison.Ordinal);
        Assert.Contains("Scanned 3 file(s): 3 hashed, 0 failed", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests that an existing destination name gets a numeric suffix.</summary>
    [Fact]
    public void ScanMoveNameCollision()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var hash = Md5Hex(gb);
        WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteDb("ra.json", (1, "Test Game One", "Game Boy", [hash]));
        var dest = Path.Combine(_root, "Compatible Games");
        WriteFile(Path.Combine("Compatible Games", "GB", "game.gb"), "taken"u8.ToArray());

        var (exit, _, stderr) = RunScan("--match", DbPath, "--move", dest, RomsPath);

        Assert.Equal(0, exit);
        Assert.True(File.Exists(Path.Combine(dest, "GB", "game.gb")));
        Assert.True(File.Exists(Path.Combine(dest, "GB", "game (1).gb")));
        Assert.Contains("Moved 1 file(s) to", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests that --move without --match is rejected.</summary>
    [Fact]
    public void ScanMoveRequiresMatch()
    {
        var (exit, _, stderr) = RunScan("--move", Path.Combine(_root, "Compatible Games"), RomsPath);

        Assert.Equal(1, exit);
        Assert.Contains("--move requires --match", stderr, StringComparison.Ordinal);
    }

    /* ========================================================================= */
    /* --dry-run                                                                */

    /// <summary>Tests that --dry-run previews the move plan without moving files.</summary>
    [Fact]
    public void ScanMoveDryRun()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var gbPath = WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteDb("ra.json", (1, "Test Game One", "Game Boy", [Md5Hex(gb)]));
        var dest = Path.Combine(_root, "Compatible Games");

        var (exit, stdout, stderr) = RunScan("--match", DbPath, "--move", dest, "--dry-run", RomsPath);

        Assert.Equal(0, exit);
        Assert.True(File.Exists(gbPath), "source file must stay in place during a dry run");
        Assert.False(File.Exists(Path.Combine(dest, "GB", "game.gb")), "nothing may be created by a dry run");
        Assert.Contains($"Would move \"{gbPath}\" to \"{Path.Combine(dest, "GB", "game.gb")}\"", stderr, StringComparison.Ordinal);
        Assert.Contains($"Would move 1 file(s) to {dest} (dry run)", stderr, StringComparison.Ordinal);
    }

    /* the plan must include the collision suffix a real move would apply */
    /// <summary>Tests that --dry-run shows the collision-renamed destination.</summary>
    [Fact]
    public void ScanMoveDryRunShowsCollisionName()
    {
        var gb = TestDataGen.GenerateGenericFile(131072);
        var gbPath = WriteFile(Path.Combine(RomsDir, "game.gb"), gb);
        WriteDb("ra.json", (1, "Test Game One", "Game Boy", [Md5Hex(gb)]));
        var dest = Path.Combine(_root, "Compatible Games");
        WriteFile(Path.Combine("Compatible Games", "GB", "game.gb"), "taken"u8.ToArray());

        var (exit, _, stderr) = RunScan("--match", DbPath, "--move", dest, "--dry-run", RomsPath);

        Assert.Equal(0, exit);
        Assert.True(File.Exists(gbPath));
        Assert.Contains($"\"{Path.Combine(dest, "GB", "game (1).gb")}\"", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests that --dry-run without --move is rejected.</summary>
    [Fact]
    public void ScanDryRunRequiresMove()
    {
        var (exit, _, stderr) = RunScan("--match", DbPath, "--dry-run", RomsPath);

        Assert.Equal(1, exit);
        Assert.Contains("--dry-run requires --move", stderr, StringComparison.Ordinal);
    }
}
