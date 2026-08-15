// New command — `RetroAchievementsSharp fetch-db` (a RetroAchievementsSharp extension; RAHasher 1.8.3 has
// no subcommands, so the legacy positional interface and its byte-exact
// parity surface are untouched).
//
// Downloads (or copies) a RetroAchievements database snapshot — the
// RetroAchievements.json produced by the RetroAchievements.DataFetcher tool
// — validates it with the same loader scan --match uses, and saves it
// atomically (temp file + rename), so a failed or truncated download never
// clobbers a good snapshot.

using System.Globalization;
using Serilog;

namespace RetroAchievementsSharp.Cli;

/// <summary>New command — `RetroAchievementsSharp fetch-db` (a RetroAchievementsSharp extension; RAHasher 1.8.3 has no subcommands, so the legacy positional interface and its byte-exact parity surface are u</summary>
internal static class FetchDbCommand
{
    /// <summary>The subcommand name (`fetch-db`), used for CLI dispatch.</summary>
    internal const string Name = "fetch-db";

    private const string DefaultOutFile = "RetroAchievements.json";

    /// <summary>Runs the fetch-db subcommand.</summary>
    /// <param name="args">the arguments after `fetch-db`</param>
    /// <returns>0 on success; 1 on any failure</returns>
    internal static int Run(string[] args)
    {
        try
        {
            return RunCore(args);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{Command} command failed", Name);
            return 1;
        }
    }

    private static int RunCore(string[] args)
    {
        var outFile = DefaultOutFile;
        string? source = null;

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

                case "--out":
                    if (argi + 1 >= args.Length)
                    {
                        Usage();
                        return 1;
                    }

                    outFile = args[++argi];
                    break;

                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        Usage();
                        return 1;
                    }

                    if (source != null)
                    {
                        Console.Error.WriteLine("Unexpected argument \"{0}\"", arg);
                        return 1;
                    }

                    source = arg;
                    break;
            }

            ++argi;
        }

        if (source == null)
        {
            Usage();
            return 1;
        }

        string json;
        string origin;

        if (File.Exists(source))
        {
            try
            {
                json = File.ReadAllText(source);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Cannot read \"{0}\": {1}", source, ex.Message);
                Log.Error(ex, "fetch-db: cannot read \"{Source}\"", source);
                return 1;
            }

            origin = source;
        }
        else
        {
            json = RaApi.SendGet(source) ?? "";
            origin = source;
            if (json.Length == 0)
            {
                Console.Error.WriteLine("Cannot download \"{0}\" — check the URL and your connection", source);
                Log.Error("fetch-db: cannot download \"{Source}\" — check the URL and your connection", source);
                return 1;
            }
        }

        var database = RetroAchievementsDatabase.Parse(json, out var error);
        if (database == null || database.GameCount == 0)
        {
            Console.Error.WriteLine("Refusing to save \"{0}\": {1}", outFile,
                database == null ? error : "the file contains no games with hashes");
            return 1;
        }

        try
        {
            var temp = outFile + ".tmp";
            File.WriteAllText(temp, json);
            File.Move(temp, outFile, overwrite: true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Cannot save \"{0}\": {1}", outFile, ex.Message);
            Log.Error(ex, "fetch-db: cannot save \"{OutFile}\"", outFile);
            return 1;
        }

        Console.WriteLine("Saved {0} — {1} game(s), {2} hash(es) from {3}",
            outFile, database.GameCount.ToString(CultureInfo.InvariantCulture),
            database.HashCount.ToString(CultureInfo.InvariantCulture), origin);
        return 0;
    }

    private static void Usage()
    {
        Console.WriteLine("Usage: RetroAchievementsSharp {0} <url-or-path> [options]", Name);
        Console.WriteLine();
        Console.WriteLine("Downloads (or copies) a RetroAchievements database snapshot — the");
        Console.WriteLine("RetroAchievements.json produced by the RetroAchievements.DataFetcher");
        Console.WriteLine("tool — validates it, and saves it atomically. Pairs with the --match");
        Console.WriteLine("option of the scan command and the --db option of identify.");
        Console.WriteLine();
        Console.WriteLine("  <url-or-path>    http(s) URL or a local file path");
        Console.WriteLine("  --out <file>     destination file (default: {0})", DefaultOutFile);
        Console.WriteLine("  -h, --help       show this help");
        Console.WriteLine();
        Console.WriteLine("The download is validated with the same parser scan --match uses; a");
        Console.WriteLine("malformed or empty result is refused. Exit code 0 on success, 1 on");
        Console.WriteLine("failure.");
    }
}
