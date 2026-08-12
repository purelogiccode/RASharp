// Shared console metadata lookup for the subcommands (scan, identify):
// maps a console id to its CLI key ("NES", "GB", ...) using the Consoles
// table. Unknown ids get "?" — the marker used for failed rows.

using RASharp.Cli.Models;

namespace RASharp.Cli;

/// <summary>Shared console metadata lookup for the subcommands (scan, identify): maps a console id to its CLI key ("NES", "GB", ...) using the Consoles table. Unknown ids get "?</summary>
internal static class ConsoleTable
{
    private static readonly Dictionary<uint, ConsoleInfo> ConsoleById = BuildConsoleById();

    /// <summary>Returns the CLI key for a console id ("NES"), or "?" when unknown.</summary>
    /// <param name="consoleId">the console identifier</param>
    /// <returns>the console key</returns>
    internal static string Key(uint consoleId)
    {
        return ConsoleById.TryGetValue(consoleId, out var console) ? console.Key : "?";
    }

    private static Dictionary<uint, ConsoleInfo> BuildConsoleById()
    {
        var map = new Dictionary<uint, ConsoleInfo>();
        foreach (var console in Consoles.All)
        {
            map.TryAdd(console.Id, console);
        }

        return map;
    }
}
