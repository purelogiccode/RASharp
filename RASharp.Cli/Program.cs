// New implementation, behavior parity with RALibretro RAHasher (GPL-3.0,
// used as reference only) — src/RAHasher.cpp. The console metadata table is
// factual data (see Consoles.cs); everything else is written fresh to match
// the observable CLI behavior.

using RASharp.Core;

namespace RASharp.Cli;

internal static class Program
{
    private const string Version = "1.8.3";

    private static int Main(string[] args)
    {
        int consoleId = 0;
        bool singleFile = true;
        string systemDirectory = ".";

        /* C's argi counts argv[0]; C# args start at 0, so use 0-based indexes
         * and translate the C bounds checks by one */
        int argi = 0;
        while (argi < args.Length && args[argi].StartsWith('-'))
        {
            if (args[argi] == "-v")
            {
                RcHash.InitVerboseMessageCallback(RhashLog);
                ++argi;
            }
            else if (args[argi] == "-s")
            {
                systemDirectory = args[++argi];
                ++argi;
            }
            else
            {
                Usage(Environment.ProcessPath ?? "RASharp");
                return 1;
            }
        }

        /* C: argi + 2 > argc  <=>  argi + 1 > args.Length (argc includes argv[0]) */
        if (argi + 1 > args.Length)
        {
            Usage(Environment.ProcessPath ?? "RASharp");
            return 1;
        }

        string consoleKey = args[argi++];
        consoleId = consoleKey == "?" ? 1 + ConsoleIds.RC_CONSOLE_MAX : FindConsoleId(consoleKey);
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

    private static void Usage(string appname)
    {
        Console.WriteLine("RASharp {0}", Version);
        Console.WriteLine("====================");
        Console.WriteLine();
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
            if (console.Group != null && lastGroup != null && lastGroup != console.Group)
                Console.WriteLine();

            Console.WriteLine(" {0,2} {1,-7} {2,-8} {3}", console.Id, console.Key, console.Group ?? "", console.Name);

            if (console.Group != null)
                lastGroup = console.Group;
        }

        Console.WriteLine();
        Console.WriteLine("For a single file, console ID can be specified as '?' (to attempt guessing by extension)");
        Console.WriteLine("Warning: consoles with a 'blank' group are currently not supported by RA!");
    }

    private static void RhashLog(string message)
    {
        Console.WriteLine(message);
    }

    private static void RhashLogErrorMessage(string message)
    {
        Console.Error.WriteLine(message);
    }

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

    private static int ProcessIteratedFile(int consoleId, string file)
    {
        int result = ProcessFile(consoleId, file);
        if (result == 0)
            Console.Write("????????????????????????????????");

        Console.WriteLine(" {0}", FileUtil.FileNameWithExtension(file));
        return result;
    }

    private static int ProcessFiles(int consoleId, string pattern)
    {
        int count = 0;

        string path = FileUtil.Directory(pattern);
        if (path == pattern) /* no backslash found. scan is in current directory */
            path = ".";

        string filePattern = FileUtil.FileNameWithExtension(pattern);

        foreach (string entry in System.IO.Directory.EnumerateFiles(path, filePattern))
        {
            count += ProcessIteratedFile(consoleId, entry);
        }

        if (count == 0)
            Console.WriteLine("No matches found");

        return count;
    }
}
