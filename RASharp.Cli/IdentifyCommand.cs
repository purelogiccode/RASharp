// New command — `RASharp identify` (a RASharp extension; RAHasher 1.8.3 has
// no subcommands, so the legacy positional interface and its byte-exact
// parity surface are untouched).
//
// Hashes a single file with an explicit console (the same flow as the
// legacy positional CLI, including zip content hashing and 3DS key use)
// and resolves the hash to a game with achievements:
//  * local mode (default): lookup in a RetroAchievements.json snapshot
//    (--db, defaults to RetroAchievements.json in the current directory);
//  * live mode: with --user/--api-key (or RASHARP_RA_USER /
//    RASHARP_RA_API_KEY), fetch the game list for the file's console from
//    the RetroAchievements API and look the hash up there.
//
// The public API has no hash->game endpoint, so the live lookup fetches
// API_GetGameList (the same data the DataFetcher snapshot holds) for the
// console that produced each hash. Console auto-detection is supported via
// a "?" system argument, exactly like the legacy CLI.

using System.Globalization;
using System.Text.Json;
using RASharp.Core;

namespace RASharp.Cli;

/// <summary>New command — `RASharp identify` (a RASharp extension; RAHasher 1.8.3 has no subcommands, so the legacy positional interface and its byte-exact parity surface are u</summary>
internal static class IdentifyCommand
{
    internal const string Name = "identify";

    private const string DefaultDbFile = "RetroAchievements.json";

    /// <summary>Runs the identify subcommand.</summary>
    /// <param name="args">the arguments after `identify`</param>
    /// <returns>0 when at least one hash resolved to a game; 1 otherwise</returns>
    public static int Run(string[] args)
    {
        var format = "text";
        var systemDir = ".";
        string? dbPath = null;
        string? user = null;
        string? apiKey = null;
        string? system = null;
        string? file = null;

        var argi = 0;
        while (argi < args.Length)
        {
            var arg = args[argi];
            switch (arg)
            {
                case "-h":
                case "--help":
                    Usage();
                    return 0;

                case "-f":
                case "--format":
                    if (argi + 1 >= args.Length)
                    {
                        Usage();
                        return 1;
                    }

                    format = args[++argi].ToLowerInvariant();
                    if (format is not ("text" or "json"))
                    {
                        Console.Error.WriteLine("Unknown identify format \"{0}\" (expected text or json)", format);
                        return 1;
                    }

                    break;

                case "-s":
                    if (argi + 1 >= args.Length)
                    {
                        Usage();
                        return 1;
                    }

                    systemDir = args[++argi];
                    break;

                case "--db":
                    if (argi + 1 >= args.Length)
                    {
                        Usage();
                        return 1;
                    }

                    dbPath = args[++argi];
                    break;

                case "--user":
                    if (argi + 1 >= args.Length)
                    {
                        Usage();
                        return 1;
                    }

                    user = args[++argi];
                    break;

                case "--api-key":
                    if (argi + 1 >= args.Length)
                    {
                        Usage();
                        return 1;
                    }

                    apiKey = args[++argi];
                    break;

                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        Usage();
                        return 1;
                    }

                    if (system == null)
                    {
                        system = arg;
                    }
                    else if (file == null)
                    {
                        file = arg;
                    }
                    else
                    {
                        Console.Error.WriteLine("Unexpected argument \"{0}\"", arg);
                        return 1;
                    }

                    break;
            }

            ++argi;
        }

        if (system == null || file == null)
        {
            Usage();
            return 1;
        }

        /* resolve the system exactly like the legacy CLI ("?" = auto-detect) */
        var consoleId = string.Equals(system, "?", StringComparison.Ordinal)
            ? 1 + ConsoleIds.RcConsoleMax
            : Program.FindConsoleId(system);
        if (consoleId == 0)
        {
            Console.Error.WriteLine("Unknown system \"{0}\"", system);
            return 1;
        }

        if (consoleId == ConsoleIds.RcConsoleNintendo3Ds)
        {
            Hash3Ds.InitHash3Ds(systemDir);
        }

        var fullPath = FileUtil.FullPath(file);
        if (!File.Exists(fullPath))
        {
            Console.Error.WriteLine("No such file: {0}", file);
            return 1;
        }

        var hashes = Program.GenerateHashes(consoleId, fullPath);
        if (hashes.Count == 0)
        {
            Console.Error.WriteLine("Unable to hash \"{0}\" as {1}", file, ConsoleTable.Key((uint)consoleId));
            return 1;
        }

        /* credentials (args override env) select live mode; otherwise local */
        user ??= Environment.GetEnvironmentVariable("RASHARP_RA_USER");
        apiKey ??= Environment.GetEnvironmentVariable("RASHARP_RA_API_KEY");
        var live = !string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(apiKey);

        var results = live
            ? LookupLive(hashes, user!, apiKey!)
            : LookupLocal(hashes, dbPath ?? DefaultDbFile);
        if (results == null)
        {
            return 1; /* the lookup reported its error already */
        }

        Emit(format, file, results);
        return results.Any(result => result.Games.Count > 0) ? 0 : 1;
    }

    /* ========================================================================= */

    private sealed record LookupResult(string Hash, uint ConsoleId, List<RetroAchievementsDatabase.Game> Games);

    private static List<LookupResult>? LookupLocal(List<Program.FileHash> hashes, string dbPath)
    {
        if (!File.Exists(dbPath))
        {
            Console.Error.WriteLine(
                "Cannot find \"{0}\" — pass --db <RetroAchievements.json>, or use --user/--api-key for a live lookup",
                dbPath);
            return null;
        }

        var database = RetroAchievementsDatabase.TryLoad(dbPath, out var error);
        if (database == null)
        {
            Console.Error.WriteLine(error);
            return null;
        }

        Console.Error.WriteLine("identify: local lookup in {0} ({1} games, {2} hashes)", dbPath, database.GameCount, database.HashCount);
        return hashes.Select(hash => new LookupResult(hash.Hash, hash.ConsoleId, database.Lookup(hash.Hash))).ToList();
    }

    private static List<LookupResult>? LookupLive(List<Program.FileHash> hashes, string user, string apiKey)
    {
        Console.Error.WriteLine("identify: live lookup on retroachievements.org as {0}", user);

        /* one API_GetGameList call per distinct console among the hashes */
        var perConsole = new Dictionary<uint, RetroAchievementsDatabase>();
        foreach (var consoleId in hashes.Select(hash => hash.ConsoleId).Distinct())
        {
            if (perConsole.ContainsKey(consoleId))
            {
                continue;
            }

            var url = string.Format(CultureInfo.InvariantCulture,
                "{0}/API_GetGameList.php?u={1}&y={2}&i={3}&h=1&f=1",
                RaApi.DefaultBaseUrl, Uri.EscapeDataString(user), Uri.EscapeDataString(apiKey), consoleId);

            var json = RaApi.SendGet(url);
            var database = json == null ? null : RetroAchievementsDatabase.Parse(json, out _);
            if (database == null)
            {
                Console.Error.WriteLine("Failed to fetch the game list for console {0} from the RetroAchievements API", consoleId);
                return null;
            }

            perConsole[consoleId] = database;
        }

        return hashes.Select(hash => new LookupResult(
            hash.Hash,
            hash.ConsoleId,
            perConsole.TryGetValue(hash.ConsoleId, out var database) ? database.Lookup(hash.Hash) : [])).ToList();
    }

    private static void Emit(string format, string file, List<LookupResult> results)
    {
        if (string.Equals(format, "json", StringComparison.Ordinal))
        {
            EmitJson(file, results);
            return;
        }

        foreach (var result in results)
        {
            var line = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}",
                result.Hash, ConsoleTable.Key(result.ConsoleId), file);
            if (result.Games.Count > 0)
            {
                var game = result.Games[0];
                line += string.Format(CultureInfo.InvariantCulture, " => {0} (ID {1})", game.Title, game.Id);
                if (result.Games.Count > 1)
                {
                    line += string.Format(CultureInfo.InvariantCulture, " (+{0} more)", result.Games.Count - 1);
                }
            }
            else
            {
                line += " => not found";
            }

            Console.WriteLine(line);
        }
    }

    private static void EmitJson(string file, List<LookupResult> results)
    {
        var array = results.Select(result => new
        {
            file,
            console = ConsoleTable.Key(result.ConsoleId),
            consoleId = result.ConsoleId,
            hash = result.Hash,
            games = result.Games.Select(game => new
            {
                id = game.Id,
                title = game.Title,
                consoleName = game.ConsoleName,
                numAchievements = game.NumAchievements,
                points = game.Points
            }).ToArray()
        });

        Console.WriteLine(JsonSerializer.Serialize(array, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Usage()
    {
        Console.WriteLine("Usage: RASharp {0} <system> <file> [options]", Name);
        Console.WriteLine();
        Console.WriteLine("Hashes a single file with an explicit console (the same way the");
        Console.WriteLine("legacy positional CLI does, including zip content hashing and 3DS");
        Console.WriteLine("keys) and resolves the hash to a game with achievements.");
        Console.WriteLine();
        Console.WriteLine("  system           console key or numeric id; '?' auto-detects");
        Console.WriteLine("  file             the file to identify");
        Console.WriteLine("  --db <db.json>   RetroAchievements.json snapshot for local lookup");
        Console.WriteLine("                   (default: {0} in the current directory)", DefaultDbFile);
        Console.WriteLine("  --user <name>    RetroAchievements username for a live API lookup");
        Console.WriteLine("  --api-key <key>  RetroAchievements web API key (control panel);");
        Console.WriteLine("                   env RASHARP_RA_USER / RASHARP_RA_API_KEY work too");
        Console.WriteLine("  -s <systempath>  supplementary files directory (3DS keys)");
        Console.WriteLine("  -f, --format <text|json>  output format (default: text)");
        Console.WriteLine("  -h, --help       show this help");
        Console.WriteLine();
        Console.WriteLine("With credentials the game list for the file's console is fetched");
        Console.WriteLine("live; without them the local snapshot is used. Exit code 0 when at");
        Console.WriteLine("least one hash resolves to a game, 1 otherwise.");
    }
}
