// New command — `RASharp scan` (a RASharp extension; RAHasher 1.8.3 has no
// subcommands, so the legacy positional interface and its byte-exact parity
// surface are untouched).
//
// Recursively hashes a ROM library with per-file console auto-detection
// (the same extension-based iteration the '?' system key uses for a single
// file) and emits one manifest row per file. Output goes to stdout only;
// progress/summary goes to stderr, so the manifest can be piped or
// redirected. Exit code 0 when every file hashed, 1 when any failed.
//
// `--match <retroachievements.json>` loads the RA game database snapshot
// (see RetroAchievementsDatabase.cs) and annotates rows whose hash belongs
// to a game with achievements. `--move <dir>` then moves the matched files
// into <dir>/<console-key>/<filename>, so the games you can actually earn
// achievements for end up in a "Compatible Games" folder.

using System.Globalization;
using System.Text.Json;
using RASharp.Core;
using RASharp.Core.Models;
using Serilog;

namespace RASharp.Cli;

/// <summary>New command — `RASharp scan` (a RASharp extension; RAHasher 1.8.3 has no subcommands, so the legacy positional interface and its byte-exact parity surface are un</summary>
internal static class ScanCommand
{
    internal const string Name = "scan";

    private const string FailedHash = "????????????????????????????????"; /* legacy '?'-mode failure marker */

    /* one manifest row; ConsoleId is 0 and Hash is empty when hashing failed;
     * Matches holds the RA games for the hash (empty when none matched) */
    private sealed record ScanRow(string File, string FullPath, uint ConsoleId, string Hash, List<RetroAchievementsDatabase.Game> Matches);

    /// <summary>Runs the scan subcommand.</summary>
    /// <param name="args">the arguments after `scan`</param>
    /// <returns>0 when every file hashed; 1 when any failed or usage was wrong</returns>
    public static int Run(string[] args)
    {
        var format = "text";
        var recursive = true;
        var systemDirectory = ".";
        var systemDirectoryProvided = false;
        string? matchPath = null;
        string? moveDir = null;
        var dryRun = false;
        var paths = new List<string>();

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
                    if (format is not ("text" or "csv" or "json"))
                    {
                        Console.Error.WriteLine("Unknown scan format \"{0}\" (expected text, csv, or json)", format);
                        return 1;
                    }

                    break;

                case "-s":
                    if (argi + 1 >= args.Length)
                    {
                        Usage();
                        return 1;
                    }

                    systemDirectory = args[++argi];
                    systemDirectoryProvided = true;
                    break;

                case "--match":
                    if (argi + 1 >= args.Length)
                    {
                        Usage();
                        return 1;
                    }

                    matchPath = args[++argi];
                    break;

                case "--move":
                    if (argi + 1 >= args.Length)
                    {
                        Usage();
                        return 1;
                    }

                    moveDir = args[++argi];
                    break;

                case "--dry-run":
                    dryRun = true;
                    break;

                case "--no-recursive":
                    recursive = false;
                    break;

                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        Usage();
                        return 1;
                    }

                    paths.Add(arg);
                    break;
            }

            ++argi;
        }

        if (paths.Count == 0)
        {
            Usage();
            return 1;
        }

        if (moveDir != null && matchPath == null)
        {
            Console.Error.WriteLine("--move requires --match (only matched files are moved)");
            return 1;
        }

        if (dryRun && moveDir == null)
        {
            Console.Error.WriteLine("--dry-run requires --move (it previews the move plan)");
            return 1;
        }

        /* 3DS key lookup funcs are cheap to register and harmless when no
         * 3DS files show up; without them .cia/.3ds/.3dsx rows would fail.
         * Register whenever -s was given — even for "-s ." (keys in the
         * current directory), matching the legacy CLI's behavior. */
        if (systemDirectoryProvided)
        {
            Hash3Ds.InitHash3Ds(systemDirectory);
        }

        RetroAchievementsDatabase? database = null;
        if (matchPath != null)
        {
            database = RetroAchievementsDatabase.TryLoad(matchPath, out var dbError);
            if (database == null)
            {
                Console.Error.WriteLine(dbError);
                return 1;
            }
        }

        var rows = new List<ScanRow>();
        var errorCount = 0;

        foreach (var root in paths)
        {
            var fullRoot = FileUtil.FullPath(root);
            if (Directory.Exists(fullRoot))
            {
                ScanDirectory(fullRoot, recursive, rows, database);
            }
            else if (File.Exists(fullRoot))
            {
                /* display a single-file argument as given, so
                 * "scan a/x.gb b/x.gb" yields distinct row paths */
                ScanFile(fullRoot, root, rows, database);
            }
            else
            {
                Console.Error.WriteLine("No such file or directory: {0}", root);
                ++errorCount;
            }
        }

        var failed = rows.Count(row => row.Hash.Length == 0);
        EmitManifest(format, rows, database != null);
        Console.Error.WriteLine(
            "Scanned {0} file(s): {1} hashed, {2} failed",
            rows.Count, rows.Count - failed, failed);

        var moveErrors = 0;
        if (moveDir != null)
        {
            var moved = MoveMatched(rows, moveDir, dryRun, ref moveErrors);
            Console.Error.WriteLine(dryRun
                ? "Would move {0} file(s) to {1} (dry run)"
                : "Moved {0} file(s) to {1}", moved, moveDir);
        }

        return errorCount == 0 && failed == 0 && moveErrors == 0 ? 0 : 1;
    }

    /* ========================================================================= */

    private static void ScanDirectory(string root, bool recursive, List<ScanRow> rows, RetroAchievementsDatabase? database)
    {
        /* deterministic output: enumerate then sort by relative path.
         * Hidden/system files and reparse points (symlink loops, junctions)
         * are skipped; inaccessible subdirectories are ignored. */
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
            IgnoreInaccessible = true
        };

        foreach (var file in Directory.EnumerateFiles(root, "*", options)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            ScanFile(file, Path.GetRelativePath(root, file), rows, database);
        }
    }

    private static void ScanFile(string path, string displayPath, List<ScanRow> rows, RetroAchievementsDatabase? database)
    {
        var ok = TryHashAutoDetect(path, out var hash, out var consoleId);
        var matches = ok && database != null ? database.Lookup(hash) : [];
        rows.Add(new ScanRow(displayPath, path, ok ? consoleId : 0, ok ? hash : "", matches));
    }

    /* mirrors the legacy '?' flow (Program.ProcessFile with consoleId > max):
     * extension-based console iteration, CHD cdreader selected per file */
    private static bool TryHashAutoDetect(string path, out string hash, out uint consoleId)
    {
        hash = "";
        consoleId = 0;

        try
        {
            var ext = FileUtil.Extension(path);
            if (string.Equals(ext, ".chd", StringComparison.OrdinalIgnoreCase))
            {
                ChdCdReader.InitChdCdreader();
            }
            else
            {
                RcHash.InitDefaultCdreader();
            }

            var iterator = new RcHashIterator();
            HashIterator.InitializeIterator(iterator, path, null, 0);
            var result = HashIterator.Iterate(out hash, iterator);

            /* Iterate advances Index past the console that accepted the file */
            if (result != 0 && iterator.Index > 0)
            {
                consoleId = iterator.Consoles[iterator.Index - 1];
            }

            HashIterator.DestroyIterator(iterator);
            return result != 0;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "scan: hashing failed for {Path}", path);
            return false;
        }
    }

    /* ========================================================================= */
    /* manifest emitters                                                        */

    private static void EmitManifest(string format, List<ScanRow> rows, bool withMatch)
    {
        switch (format)
        {
            case "csv":
                EmitCsv(rows, withMatch);
                break;

            case "json":
                EmitJson(rows, withMatch);
                break;

            default:
                EmitText(rows, withMatch);
                break;
        }
    }

    /* one line per file: <hash> <console-key> <path>; failures use the 32-'?'
     * marker and a '?' console, matching the legacy wildcard output style.
     * With --match, matched rows get "=> <Title> (ID <id>)" appended. */
    private static void EmitText(List<ScanRow> rows, bool withMatch)
    {
        foreach (var row in rows)
        {
            var line = string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}",
                row.Hash.Length == 32 ? row.Hash : FailedHash,
                ConsoleKey(row.ConsoleId),
                row.File);

            if (withMatch && row.Matches.Count > 0)
            {
                var game = row.Matches[0];
                line += string.Format(CultureInfo.InvariantCulture, " => {0} (ID {1})", game.Title, game.Id);
                if (row.Matches.Count > 1)
                {
                    line += string.Format(CultureInfo.InvariantCulture, " (+{0} more)", row.Matches.Count - 1);
                }
            }

            Console.WriteLine(line);
        }
    }

    /* With --match the header gains game_id,game_title and game_matches
     * (first match only for the id/title; the count covers all matches) */
    private static void EmitCsv(List<ScanRow> rows, bool withMatch)
    {
        Console.WriteLine(withMatch ? "file,console,hash,game_id,game_title,game_matches" : "file,console,hash");
        foreach (var row in rows)
        {
            var hash = row.Hash.Length == 32 ? row.Hash : "";
            if (!withMatch)
            {
                Console.WriteLine("{0},{1},{2}",
                    CsvField(row.File),
                    CsvField(ConsoleKey(row.ConsoleId)),
                    hash);
                continue;
            }

            var gameId = "";
            var gameTitle = "";
            var gameMatches = "0";
            if (row.Matches.Count > 0)
            {
                gameId = row.Matches[0].Id.ToString(CultureInfo.InvariantCulture);
                gameTitle = row.Matches[0].Title;
                gameMatches = row.Matches.Count.ToString(CultureInfo.InvariantCulture);
            }

            Console.WriteLine("{0},{1},{2},{3},{4},{5}",
                CsvField(row.File),
                CsvField(ConsoleKey(row.ConsoleId)),
                hash,
                CsvField(gameId),
                CsvField(gameTitle),
                gameMatches);
        }
    }

    /* With --match each row gains a "games" array (id, title, consoleName,
     * numAchievements, points) — empty when the hash is not in the database */
    private static void EmitJson(List<ScanRow> rows, bool withMatch)
    {
        var array = rows.Select(row => new
        {
            file = row.File,
            console = row.ConsoleId == 0 ? null : ConsoleKey(row.ConsoleId),
            consoleId = row.ConsoleId,
            hash = row.Hash.Length == 32 ? row.Hash : null,
            games = withMatch
                ? row.Matches.Select(game => new
                {
                    id = game.Id,
                    title = game.Title,
                    consoleName = game.ConsoleName,
                    numAchievements = game.NumAchievements,
                    points = game.Points
                }).ToArray()
                : null
        });

        Console.WriteLine(JsonSerializer.Serialize(array, new JsonSerializerOptions { WriteIndented = true }));
    }

    /* RFC 4180-style quoting: quote fields containing comma, quote, or newline */
    private static string CsvField(string field)
    {
        if (field.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return field;
        }

        return "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    /* ========================================================================= */
    /* --move / --dry-run: relocate matched files into <dir>/<console-key>/<filename>.
     * The destination is planned identically in both modes (collision suffixes
     * included), so a dry run previews exactly what a real move would do. */
    private static int MoveMatched(List<ScanRow> rows, string moveDir, bool dryRun, ref int moveErrors)
    {
        var moved = 0;

        foreach (var row in rows)
        {
            if (row.Matches.Count == 0)
            {
                continue;
            }

            var consoleDir = Path.Combine(moveDir, ConsoleKey(row.ConsoleId));
            var destination = Path.Combine(consoleDir, Path.GetFileName(row.File));

            /* never overwrite: append " (1)", " (2)", ... when the name is taken */
            if (File.Exists(destination))
            {
                var name = Path.GetFileNameWithoutExtension(destination);
                var ext = Path.GetExtension(destination);
                for (var i = 1; File.Exists(destination); ++i)
                {
                    destination = Path.Combine(consoleDir, string.Format(CultureInfo.InvariantCulture, "{0} ({1}){2}", name, i, ext));
                }
            }

            if (dryRun)
            {
                Console.Error.WriteLine("Would move \"{0}\" to \"{1}\"", row.FullPath, destination);
                ++moved;
                continue;
            }

            try
            {
                Directory.CreateDirectory(consoleDir);
                File.Move(row.FullPath, destination);
                ++moved;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Cannot move \"{0}\" to \"{1}\": {2}", row.FullPath, destination, ex.Message);
                ++moveErrors;
            }
        }

        return moved;
    }

    private static string ConsoleKey(uint consoleId)
    {
        return ConsoleTable.Key(consoleId);
    }

    private static void Usage()
    {
        Console.WriteLine("Usage: RASharp {0} [options] <path>...", Name);
        Console.WriteLine();
        Console.WriteLine("Scans files or directories and prints one manifest row per file:");
        Console.WriteLine("hash, detected console, and path. Consoles are auto-detected per");
        Console.WriteLine("file the same way the '?' system key works for a single file.");
        Console.WriteLine();
        Console.WriteLine("  -f, --format <text|csv|json>  output format (default: text)");
        Console.WriteLine("  -s <systempath>               supplementary files directory (3DS keys)");
        Console.WriteLine("      --match <db.json>         RetroAchievements database snapshot");
        Console.WriteLine("                                (RetroAchievements.json); rows whose hash");
        Console.WriteLine("                                belongs to a game are annotated with it");
        Console.WriteLine("      --move <dir>              move matched files into <dir>/<console-key>/");
        Console.WriteLine("                                <filename> (requires --match); existing files");
        Console.WriteLine("                                are renamed with a (1), (2) suffix");
        Console.WriteLine("      --dry-run                 preview --move without moving anything");
        Console.WriteLine("                                (requires --move)");
        Console.WriteLine("      --no-recursive            do not descend into subdirectories");
        Console.WriteLine("  -h, --help                    show this help");
        Console.WriteLine();
        Console.WriteLine("Hidden, system, and reparse-point files are skipped. The manifest is");
        Console.WriteLine("written to stdout; the summary is written to stderr. Exit code 0 when");
        Console.WriteLine("every file hashed, 1 when any failed.");
        Console.WriteLine();
        Console.WriteLine("Note: .zip files hash by filename (Arcade) during auto-detection, so");
        Console.WriteLine("they rarely match the database — extract them first.");
    }
}
