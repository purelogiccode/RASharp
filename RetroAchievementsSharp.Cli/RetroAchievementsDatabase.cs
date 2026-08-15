// Loader for the RetroAchievements game database snapshot used by
// `RetroAchievementsSharp scan --match` — the JSON file produced by the
// RetroAchievements.DataFetcher tool (see CSharp_SimpleLauncher):
// an array of game entries with ID, Title, ConsoleID, ConsoleName,
// ImageIcon, NumAchievements, Points, DateModified, and Hashes[] — the RA
// hash strings for the game's ROMs.
//
// Hashes are indexed case-insensitively (the RA hashes are lowercase hex,
// but the lookup should not depend on that). One hash can map to several
// games, so each entry points to a list.

using System.Text.Json;

namespace RetroAchievementsSharp.Cli;

/// <summary>Loader for the RetroAchievements game database snapshot used by `RetroAchievementsSharp scan --match` — the JSON file produced by the RetroAchievements.DataFetcher tool (see CSharp_</summary>
internal sealed class RetroAchievementsDatabase
{
    /// <summary>A game entry from the database.</summary>
    public sealed record Game(int Id, string Title, string ConsoleName, int NumAchievements, int Points);

    private readonly Dictionary<string, List<Game>> _gamesByHash;

    private RetroAchievementsDatabase(Dictionary<string, List<Game>> gamesByHash, int gameCount, int hashCount)
    {
        _gamesByHash = gamesByHash;
        GameCount = gameCount;
        HashCount = hashCount;
    }

    /// <summary>Number of game entries with at least one indexed hash.</summary>
    public int GameCount { get; }

    /// <summary>Number of distinct hashes indexed.</summary>
    public int HashCount { get; }

    /// <summary>Loads the database file.</summary>
    /// <param name="path">the JSON file path</param>
    /// <param name="error">the error message when loading failed</param>
    /// <returns>the loaded database, or null when the file is missing or malformed</returns>
    public static RetroAchievementsDatabase? TryLoad(string path, out string? error)
    {
        error = null;

        try
        {
            return Parse(File.ReadAllText(path), out error);
        }
        catch (Exception ex)
        {
            error = $"Cannot load RetroAchievements database \"{path}\": {ex.Message}";
            return null;
        }
    }

    /// <summary>Parses database JSON (an array of game entries with Hashes[]).</summary>
    /// <param name="json">the JSON text</param>
    /// <param name="error">the error message when parsing failed</param>
    /// <returns>the parsed database, or null when the text is malformed</returns>
    public static RetroAchievementsDatabase? Parse(string json, out string? error)
    {
        error = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                error = "The JSON does not contain an array of games";
                return null;
            }

            var map = new Dictionary<string, List<Game>>(StringComparer.OrdinalIgnoreCase);
            var gameCount = 0;
            var hashCount = 0;

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var game = new Game(
                    GetInt(element, "ID"),
                    GetString(element, "Title"),
                    GetString(element, "ConsoleName"),
                    GetInt(element, "NumAchievements"),
                    GetInt(element, "Points"));

                if (!element.TryGetProperty("Hashes", out var hashes) || hashes.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var added = false;
                foreach (var hash in hashes.EnumerateArray())
                {
                    if (hash.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var value = hash.GetString();
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    if (!map.TryGetValue(value, out var games))
                    {
                        games = [];
                        map[value] = games;
                        ++hashCount;
                    }

                    games.Add(game);
                    added = true;
                }

                if (added)
                {
                    ++gameCount;
                }
            }

            return new RetroAchievementsDatabase(map, gameCount, hashCount);
        }
        catch (Exception ex)
        {
            error = $"Cannot parse RetroAchievements database: {ex.Message}";
            return null;
        }
    }

    /// <summary>Returns the games whose hash list contains the given hash (empty when none).</summary>
    /// <param name="hash">the hash to look up</param>
    /// <returns>the matching games</returns>
    public List<Game> Lookup(string hash)
    {
        return _gamesByHash.TryGetValue(hash, out var games) ? games : [];
    }

    private static int GetInt(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) &&
               value.ValueKind == JsonValueKind.Number &&
               value.TryGetInt32(out var number)
            ? number
            : 0;
    }

    private static string GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
    }
}
