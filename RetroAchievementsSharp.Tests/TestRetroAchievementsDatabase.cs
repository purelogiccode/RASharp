// Direct tests for the RetroAchievements database snapshot parser used by
// `scan --match`. Parse is pure JSON handling; TryLoad adds the file layer.

using RetroAchievementsSharp.Cli;

namespace RetroAchievementsSharp.Tests;

/// <summary>Direct tests for the RetroAchievements database snapshot parser used by `scan --match`. Parse is pure JSON handling; TryLoad adds the file layer.</summary>
public class TestRetroAchievementsDatabase : IDisposable
{
    private readonly string _root;

    public TestRetroAchievementsDatabase()
    {
        _root = Path.Combine(Path.GetTempPath(), "rasharp_db_test_" + Guid.NewGuid().ToString("N")[..8]);
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

    private static string GameJson(int id, string title, string consoleName, params string[] hashes)
    {
        var hashesJson = string.Join(", ", hashes.Select(h => $"\"{h}\""));
        return "{\"ID\": " + id + ", \"Title\": \"" + title + "\", \"ConsoleID\": 1, \"ConsoleName\": \"" + consoleName +
               "\", \"ImageIcon\": \"\", \"NumAchievements\": 5, \"Points\": 50, " +
               "\"Hashes\": [" + hashesJson + "]}";
    }

    private const string HashA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string HashB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    /* ========================================================================= */

    /// <summary>Tests parsing a valid two-game database.</summary>
    [Fact]
    public void ParseValidDatabase()
    {
        var json = "[" + GameJson(1, "Game One", "Game Boy", HashA) + ", " +
                   GameJson(2, "Game Two", "NES/Famicom", HashB, "cccccccccccccccccccccccccccccccc") + "]";

        var db = RetroAchievementsDatabase.Parse(json, out var error);

        Assert.NotNull(db);
        Assert.Null(error);
        Assert.Equal(2, db.GameCount);
        Assert.Equal(3, db.HashCount);

        var games = db.Lookup(HashA);
        Assert.Single(games);
        Assert.Equal(1, games[0].Id);
        Assert.Equal("Game One", games[0].Title);
        Assert.Equal("Game Boy", games[0].ConsoleName);
        Assert.Equal(5, games[0].NumAchievements);
        Assert.Equal(50, games[0].Points);

        Assert.Single(db.Lookup(HashB)); /* game 2 only; its second hash is different */
        Assert.Empty(db.Lookup("dddddddddddddddddddddddddddddddd"));
    }

    /// <summary>Tests that hash lookup is case-insensitive.</summary>
    [Fact]
    public void LookupIsCaseInsensitive()
    {
        var json = "[" + GameJson(1, "Game One", "Game Boy", HashA) + "]";

        var db = RetroAchievementsDatabase.Parse(json, out _);

        Assert.NotNull(db);
        Assert.Single(db.Lookup(HashA.ToUpperInvariant()));
    }

    /// <summary>Tests that a hash shared by several games maps to all of them.</summary>
    [Fact]
    public void DuplicateHashMapsToAllGames()
    {
        var json = "[" + GameJson(1, "Game One", "Game Boy", HashA) + ", " +
                   GameJson(2, "Game Two", "Game Boy", HashA) + "]";

        var db = RetroAchievementsDatabase.Parse(json, out _);

        Assert.NotNull(db);
        Assert.Equal(2, db.GameCount);
        Assert.Equal(1, db.HashCount); /* the shared hash is indexed once */
        Assert.Equal(2, db.Lookup(HashA).Count);
    }

    /// <summary>Tests that entries without a usable Hashes array are skipped.</summary>
    [Fact]
    public void EntriesWithoutHashesAreSkipped()
    {
        var json = "[" + GameJson(1, "Game One", "Game Boy", HashA) + ", " +
                   "{\"ID\": 2, \"Title\": \"No Hashes\", \"ConsoleName\": \"GB\", \"NumAchievements\": 1, \"Points\": 1}, " +
                   "{\"ID\": 3, \"Title\": \"Bad Hashes\", \"ConsoleName\": \"GB\", \"NumAchievements\": 1, \"Points\": 1, \"Hashes\": \"not-an-array\"}, " +
                   "42]"; /* non-object entries are skipped too */

        var db = RetroAchievementsDatabase.Parse(json, out _);

        Assert.NotNull(db);
        Assert.Equal(1, db.GameCount);
        Assert.Equal(1, db.HashCount);
    }

    /// <summary>Tests that missing fields default to zero/empty.</summary>
    [Fact]
    public void MissingFieldsDefaultToZero()
    {
        const string json = "[{\"Hashes\": [\"" + HashA + "\"]}]";

        var db = RetroAchievementsDatabase.Parse(json, out _);

        Assert.NotNull(db);
        var games = db.Lookup(HashA);
        Assert.Single(games);
        Assert.Equal(0, games[0].Id);
        Assert.Equal("", games[0].Title);
        Assert.Equal("", games[0].ConsoleName);
    }

    /// <summary>Tests that empty hash strings are not indexed.</summary>
    [Fact]
    public void EmptyHashesAreNotIndexed()
    {
        var json = "[" + GameJson(1, "Game One", "Game Boy", "") + "]";

        var db = RetroAchievementsDatabase.Parse(json, out _);

        Assert.NotNull(db);
        Assert.Equal(0, db.GameCount);
        Assert.Equal(0, db.HashCount);
    }

    /// <summary>Tests that a non-array root is rejected with an error.</summary>
    [Fact]
    public void NonArrayRootIsRejected()
    {
        var db = RetroAchievementsDatabase.Parse("{\"ID\": 1}", out var error);

        Assert.Null(db);
        Assert.Contains("array", error, StringComparison.Ordinal);
    }

    /// <summary>Tests that malformed JSON is rejected with an error.</summary>
    [Fact]
    public void MalformedJsonIsRejected()
    {
        var db = RetroAchievementsDatabase.Parse("this is not json", out var error);

        Assert.Null(db);
        Assert.Contains("Cannot parse", error, StringComparison.Ordinal);
    }

    /// <summary>Tests TryLoad on a missing file.</summary>
    [Fact]
    public void TryLoadMissingFileFails()
    {
        var db = RetroAchievementsDatabase.TryLoad(Path.Combine(_root, "ra.json"), out var error);

        Assert.Null(db);
        Assert.Contains("Cannot load", error, StringComparison.Ordinal);
    }

    /// <summary>Tests TryLoad on a valid file.</summary>
    [Fact]
    public void TryLoadValidFile()
    {
        var path = Path.Combine(_root, "ra.json");
        File.WriteAllText(path, "[" + GameJson(1, "Game One", "Game Boy", HashA) + "]");

        var db = RetroAchievementsDatabase.TryLoad(path, out var error);

        Assert.NotNull(db);
        Assert.Null(error);
        Assert.Equal(1, db.GameCount);
        Assert.Single(db.Lookup(HashA));
    }
}
