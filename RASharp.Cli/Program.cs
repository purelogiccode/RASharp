// New implementation, behavior parity with RALibretro RAHasher (GPL-3.0,
// used as reference only) — src/RAHasher.cpp. The console metadata table is
// factual data (see Consoles.cs); everything else is written fresh to match
// the observable CLI behavior.

using RASharp.Core;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace RASharp.Cli;


using RASharp.Core.Models;
/// <summary>New implementation, behavior parity with RALibretro RAHasher (GPL-3.0, used as reference only) — src/RAHasher.cpp. The console metadata table is factual data (s</summary>
internal static class Program
{
    internal const string Version = "1.8.3";

/// <summary>main.</summary>
/// <param name="args">the command-line arguments</param>
/// <returns>the result</returns>
    private static int Main(string[] args)
    {
        try
        {
            ConfigureLogging();
            ApplicationStatsReporter.ReportUsage();
            return Run(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception");
            Console.Error.WriteLine("Unhandled exception: {0}", ex.Message);
            return 1;
        }
        finally
        {
            ApplicationStatsReporter.Flush();
            Log.CloseAndFlush();
        }
    }

    /* Serilog wiring. The console sink reproduces the original's byte-exact
     * output (message + platform newline, no themes); the bug-report sink
     * forwards Warning+ events. The API key comes from Constants (decoded at
     * startup) unless RASHARP_BUGREPORT_API_KEY overrides it;
     * RASHARP_BUGREPORT_DISABLE=1 forces forwarding off. The sink never
     * writes to stdout/stderr, so parity output is unaffected. */
/// <summary>Serilog wiring. The console sink reproduces the original's byte-exact output (message + platform newline, no themes); the bug-report sink forwards Warning+ even</summary>
/// <returns>the result</returns>
    private static void ConfigureLogging()
    {
        var configuration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: "{Message:l}{NewLine}",
                theme: ConsoleTheme.None,
                standardErrorFromLevel: LogEventLevel.Error);

        string? apiKey = Environment.GetEnvironmentVariable("RASHARP_BUGREPORT_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            apiKey = Constants.BugReportApiKey;

        bool disabled = string.Equals(Environment.GetEnvironmentVariable("RASHARP_BUGREPORT_DISABLE"), "1", StringComparison.Ordinal);
        if (!disabled)
        {
            string url = Environment.GetEnvironmentVariable("RASHARP_BUGREPORT_URL") ?? BugReportSink.DefaultUrl;
            configuration.WriteTo.Sink(new BugReportSink(url, apiKey), restrictedToMinimumLevel: LogEventLevel.Warning);
        }

        Log.Logger = configuration.CreateLogger();
    }

/// <summary>Executes the CLI argument loop.</summary>
/// <param name="args">the command-line arguments</param>
/// <returns>the result</returns>
    private static int Run(string[] args)
    {
        int consoleId = 0;
        bool singleFile = true;
        string systemDirectory = ".";

        /* C's argi counts argv[0]; C# args start at 0, so use 0-based indexes
         * and translate the C bounds checks by one */
        int argi = 0;
        while (argi < args.Length && args[argi].StartsWith('-'))
        {
            if (string.Equals(args[argi], "-v", StringComparison.Ordinal))
            {
                RcHash.InitVerboseMessageCallback(RhashLog);
                ++argi;
            }
            else if (string.Equals(args[argi], "-s", StringComparison.Ordinal))
            {
                /* C reads argv[++argi] even at end-of-args (segfault); harden to usage */
                if (argi + 1 >= args.Length)
                {
                    Usage(Environment.ProcessPath ?? "RASharp");
                    return 1;
                }

                systemDirectory = args[++argi];
                ++argi;
            }
            else
            {
                Usage(Environment.ProcessPath ?? "RASharp");
                return 1;
            }
        }

        /* C: argi + 2 > argc. C's argi counts argv[0] (argi_c = argi_cs + 1) and
         * argc = args.Length + 1, so the C# form is argi + 2 > args.Length */
        if (argi + 2 > args.Length)
        {
            Usage(Environment.ProcessPath ?? "RASharp");
            return 1;
        }

        string consoleKey = args[argi++];
        consoleId = string.Equals(consoleKey, "?", StringComparison.Ordinal) ? 1 + ConsoleIds.RC_CONSOLE_MAX : FindConsoleId(consoleKey);
        if (consoleId == 0)
        {
            Usage(Environment.ProcessPath ?? "RASharp");
            return 1;
        }

        RcHash.InitErrorMessageCallback(RhashLogErrorMessage);

        if (consoleId == ConsoleIds.RC_CONSOLE_NINTENDO_3DS)
        {
            Hash3DS.InitHash3DS(systemDirectory);
        }

        /* C: argi + 1 < argc  <=>  argi + 1 < args.Length */
        if (argi + 1 < args.Length)
        {
            if (consoleId > ConsoleIds.RC_CONSOLE_MAX)
            {
                Console.WriteLine("Specific console must be specified when processing multiple files");
                return 1;
            }

            singleFile = false;
        }
        else
        {
            string file = args[argi];
            if (file.Contains('*') || file.Contains('?'))
            {
                if (consoleId > ConsoleIds.RC_CONSOLE_MAX)
                {
                    Console.WriteLine("Specific console must be specified when using wildcards");
                    return 1;
                }

                singleFile = false;
            }
        }

        if (!singleFile)
        {
            /* verbose logging not allowed when processing multiple files */
            RcHash.InitVerboseMessageCallback(null);
        }

        while (argi < args.Length)
        {
            string file = args[argi++];

            if (file.Contains('*') || file.Contains('?'))
            {
                if (ProcessFiles(consoleId, file) == 0)
                    return 1;
            }
            else
            {
                int result = ProcessFile(consoleId, file);

                if (singleFile)
                    Console.WriteLine();
                else
                    Console.WriteLine(" {0}", FileUtil.FileNameWithExtension(file));

                if (result == 0)
                    return 1;
            }
        }

        return 0;
    }

/// <summary>Resolves a console key or numeric id to a console id.</summary>
/// <param name="key">the console key or numeric id</param>
/// <returns>the console id, or 0 when unknown</returns>
    private static int FindConsoleId(string key)
    {
        foreach (var console in Consoles.All)
        {
            if (console.Group != null && string.Equals(key, console.Key, StringComparison.OrdinalIgnoreCase))
                return (int)console.Id;
        }

        /* falling back to original behaviour: atoi(key) */
        return Atoi(key);
    }

    /* C atoi semantics: optional sign, leading digits, 0 when none */
/// <summary>C atoi semantics: optional sign, leading digits, 0 when none</summary>
/// <param name="s">the s parameter</param>
/// <returns>the result</returns>
    private static int Atoi(string s)
    {
        int i = 0;
        bool negative = false;
        if (i < s.Length && (s[i] == '-' || s[i] == '+'))
        {
            negative = s[i] == '-';
            ++i;
        }

        int value = 0;
        while (i < s.Length && s[i] >= '0' && s[i] <= '9')
        {
            value = value * 10 + (s[i] - '0');
            ++i;
        }

        return negative ? -value : value;
    }

/// <summary>Prints the usage banner and console table.</summary>
/// <param name="appname">the application name</param>
/// <returns>the result</returns>
    private static void Usage(string appname)
    {
        Console.WriteLine("RASharp {0}", Version);
        Console.WriteLine("====================");
        Console.WriteLine("Usage: {0} [-v] [-s systempath] system filepath...", FileUtil.FileName(appname));
        Console.WriteLine();
        Console.WriteLine("  -v             (optional) enables verbose messages for debugging");
        Console.WriteLine("  -s systempath  (optional) specifies where supplementary files are stored (typically a path to RetroArch/system)");
        Console.WriteLine("  system         specifies the system key or id associated to the game (which hash algorithm to use)");
        Console.WriteLine("  filepath       specifies the path to the game file (file may include wildcards, path may not)");
        Console.WriteLine();
        Console.WriteLine(" ID Key     Group    Name");
        Console.WriteLine(" -- ------- -------- ---------------------------");

        string? lastGroup = null;
        foreach (var console in Consoles.All)
        {
            if (console.Group != null && lastGroup != null && !string.Equals(lastGroup, console.Group, StringComparison.Ordinal))
                Console.WriteLine();

            Console.WriteLine(" {0,2} {1,-7} {2,-8} {3}", console.Id, console.Key, console.Group ?? "", console.Name);

            if (console.Group != null)
                lastGroup = console.Group;
        }

        Console.WriteLine();
        Console.WriteLine("For a single file, console ID can be specified as '?' (to attempt guessing by extension)");
        Console.WriteLine("Warning: consoles with a 'blank' group are currently not supported by RA!");
    }

/// <summary>Verbose message callback routed through Serilog.</summary>
/// <param name="message">the message text</param>
/// <returns>the result</returns>
    private static void RhashLog(string message)
    {
        /* parity-critical: the console sink must emit message + newline only */
        Log.Information("{Message}", message);
    }

/// <summary>Error message callback routed through Serilog (stderr).</summary>
/// <param name="message">the message text</param>
/// <returns>the result</returns>
    private static void RhashLogErrorMessage(string message)
    {
        /* parity-critical: goes to stderr via standardErrorFromLevel */
        Log.Error("{Message}", message);
    }

/// <summary>Processes a single file for a console.</summary>
/// <param name="consoleId">the console identifier</param>
/// <param name="file">the file path</param>
/// <returns>the result</returns>
    private static int ProcessFile(int consoleId, string file)
    {
        string filePath = FileUtil.FullPath(file);
        string ext = FileUtil.Extension(file);
        string hash;

        if (consoleId != ConsoleIds.RC_CONSOLE_ARCADE && consoleId <= ConsoleIds.RC_CONSOLE_MAX &&
            ext.Length == 4 && char.ToLowerInvariant(ext[1]) == 'z' && char.ToLowerInvariant(ext[2]) == 'i' && char.ToLowerInvariant(ext[3]) == 'p')
        {
            byte[]? data = FileUtil.LoadZippedFile(filePath, out _);
            if (data != null)
            {
                if (RcHash.GenerateFromBuffer(out hash, (uint)consoleId, data, data.Length))
                {
                    Console.Write(hash);
                    return 1;
                }
            }

            return 0;
        }

        if (ext.Length == 4 && char.ToLowerInvariant(ext[1]) == 'c' && char.ToLowerInvariant(ext[2]) == 'h' && char.ToLowerInvariant(ext[3]) == 'd')
        {
            ChdCdReader.InitChdCdreader();
        }
        else
        {
            RcHash.InitDefaultCdreader();
        }

        if (consoleId > ConsoleIds.RC_CONSOLE_MAX)
        {
            var iterator = new RcHashIterator();
            HashIterator.InitializeIterator(iterator, filePath, null, 0);
            int count = 0;
            while (HashIterator.Iterate(out hash, iterator) != 0)
            {
                Console.Write(hash);
                ++count;
            }

            HashIterator.DestroyIterator(iterator);
            return count;
        }

        if (RcHash.GenerateFromFile(out hash, (uint)consoleId, filePath))
        {
            Console.Write(hash);
            return 1;
        }

        return 0;
    }

/// <summary>Processes one wildcard match, printing the hash and filename.</summary>
/// <param name="consoleId">the console identifier</param>
/// <param name="file">the file path</param>
/// <returns>the result</returns>
    private static int ProcessIteratedFile(int consoleId, string file)
    {
        int result = ProcessFile(consoleId, file);
        if (result == 0)
            Console.Write("????????????????????????????????");

        Console.WriteLine(" {0}", FileUtil.FileNameWithExtension(file));
        return result;
    }

/// <summary>Expands a wildcard pattern and processes every match.</summary>
/// <param name="consoleId">the console identifier</param>
/// <param name="pattern">the wildcard pattern</param>
/// <returns>the result</returns>
    private static int ProcessFiles(int consoleId, string pattern)
    {
        int count = 0;

        /* util::directory splits on '\' only (Windows) */
        string path = FileUtil.Directory(pattern);
        if (string.Equals(path, pattern, StringComparison.Ordinal)) /* no backslash found. scan is in current directory */
            path = ".";

        /* FindFirstFileA scans the full pattern (forward slashes accepted); the
         * per-file path is then built from the backslash-split directory, which
         * reproduces the original's behavior for patterns like "dir/*.bin" (the
         * match is found, but the open path is ".\<name>"). Directory-less
         * patterns scan the current directory. */
        string? patternDir = Path.GetDirectoryName(pattern);
        if (string.IsNullOrEmpty(patternDir))
            patternDir = ".";
        string patternName = Path.GetFileName(pattern) ?? "*";

        /* Note: FindFirstFileA also matches directories; a directory literally named
         * "x.gb" would produce a "????" line in the original but is skipped here.
         * Pre-existing edge case, kept for simplicity. */
        foreach (string entry in System.IO.Directory.EnumerateFiles(patternDir, patternName))
        {
            count += ProcessIteratedFile(consoleId, path + "\\" + Path.GetFileName(entry));
        }

        if (count == 0)
            Console.WriteLine("No matches found");

        return count;
    }
}
