// New command — `RetroAchievementsSharp consoles` (a RetroAchievementsSharp extension; RAHasher 1.8.3 has
// no subcommands, so the legacy positional interface and its byte-exact
// parity surface are untouched).
//
// Dumps the console metadata table (id, key, group, name) that the usage
// banner shows, in a machine-readable form scripts can use to map console
// keys <-> numeric ids <-> display names.

using System.Text.Json;
using Serilog;

namespace RetroAchievementsSharp.Cli;

/// <summary>New command — `RetroAchievementsSharp consoles` (a RetroAchievementsSharp extension; RAHasher 1.8.3 has no subcommands, so the legacy positional interface and its byte-exact parity surface are un</summary>
internal static class ConsolesCommand
{
    /// <summary>The subcommand name (`consoles`), used for CLI dispatch.</summary>
    internal const string Name = "consoles";

    /// <summary>Runs the consoles subcommand.</summary>
    /// <param name="args">the arguments after `consoles`</param>
    /// <returns>0 on success; 1 on usage errors</returns>
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
        var format = "text";

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
                        Console.Error.WriteLine("Unknown consoles format \"{0}\" (expected text, csv, or json)", format);
                        return 1;
                    }

                    break;

                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        Usage();
                        return 1;
                    }

                    Console.Error.WriteLine("Unexpected argument \"{0}\"", arg);
                    return 1;
            }

            ++argi;
        }

        switch (format)
        {
            case "csv":
                EmitCsv();
                break;

            case "json":
                EmitJson();
                break;

            default:
                EmitText();
                break;
        }

        return 0;
    }

    /* ========================================================================= */

    /* the same column layout as the usage banner table, without the group
     * separators — one flat row per console, NULL-group consoles keep a
     * blank group column */
    private static void EmitText()
    {
        Console.WriteLine(" ID Key     Group    Name");
        Console.WriteLine(" -- ------- -------- ---------------------------");
        foreach (var console in Consoles.All)
        {
            Console.WriteLine(" {0,2} {1,-7} {2,-8} {3}", console.Id, console.Key, console.Group ?? "", console.Name);
        }
    }

    /* RFC 4180-style quoting: quote fields containing comma, quote, or newline */
    private static void EmitCsv()
    {
        Console.WriteLine("id,key,group,name");
        foreach (var console in Consoles.All)
        {
            Console.WriteLine("{0},{1},{2},{3}",
                CsvField(console.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                CsvField(console.Key),
                CsvField(console.Group ?? ""),
                CsvField(console.Name));
        }
    }

    private static void EmitJson()
    {
        var array = Consoles.All.Select(console => new
        {
            id = console.Id,
            key = console.Key,
            group = console.Group,
            name = console.Name
        });

        Console.WriteLine(JsonSerializer.Serialize(array, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string CsvField(string field)
    {
        if (field.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return field;
        }

        return "\"" + field.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static void Usage()
    {
        Console.WriteLine("Usage: RetroAchievementsSharp {0} [options]", Name);
        Console.WriteLine();
        Console.WriteLine("Prints the console metadata table (id, key, group, name) — the same");
        Console.WriteLine("data the usage banner shows — in a machine-readable form.");
        Console.WriteLine();
        Console.WriteLine("  -f, --format <text|csv|json>  output format (default: text)");
        Console.WriteLine("  -h, --help                    show this help");
        Console.WriteLine();
        Console.WriteLine("The manifest is written to stdout. Exit code 0 on success, 1 on");
        Console.WriteLine("usage errors.");
    }
}
