// New command — `RetroAchievementsSharp checkkeys` (a RetroAchievementsSharp extension; RAHasher 1.8.3
// has no subcommands, so the legacy positional interface and its byte-exact
// parity surface are untouched).
//
// Validates the 3DS key material in a system directory (the `-s` argument
// of the legacy CLI): aes_keys.txt must exist and carry the keys the
// engine's 3DS decryption needs (slot0x2CKeyX for NCCH, slot0x3DKeyX for
// CIA, and at least one common<slot>= key), and seeddb.bin — optional, only
// needed for seed-encrypted titles — must not be corrupt.
//
// The checks mirror the loader's own semantics in Hash3DS.cs: a key line is
// "present" when its 32-hex value parses to a first byte != 0, exactly as
// the KeyIsPresent() check in the engine.

using System.Globalization;

namespace RetroAchievementsSharp.Cli;

/// <summary>New command — `RetroAchievementsSharp checkkeys` (a RetroAchievementsSharp extension; RAHasher 1.8.3 has no subcommands, so the legacy positional interface and its byte-exact parity surface are un</summary>
internal static class CheckKeysCommand
{
    internal const string Name = "checkkeys";

    /// <summary>Runs the checkkeys subcommand.</summary>
    /// <param name="args">the arguments after `checkkeys`</param>
    /// <returns>0 when the 3DS key files are usable; 1 otherwise</returns>
    public static int Run(string[] args)
    {
        var systemDir = ".";

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

                case "-s":
                    if (argi + 1 >= args.Length)
                    {
                        Usage();
                        return 1;
                    }

                    systemDir = args[++argi];
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

        Console.WriteLine("Checking 3DS key files in \"{0}\":", systemDir);
        var aesOk = CheckAesKeys(systemDir);
        var seedOk = CheckSeedDb(systemDir);

        Console.WriteLine(aesOk && seedOk ? "3DS keys OK" : "3DS keys INVALID");
        return aesOk && seedOk ? 0 : 1;
    }

    /* ========================================================================= */

    private static bool CheckAesKeys(string systemDir)
    {
        var path = Path.Combine(systemDir, "aes_keys.txt");
        if (!File.Exists(path))
        {
            Console.WriteLine("aes_keys.txt   MISSING — 3DS hashing will fail");
            return false;
        }

        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine("aes_keys.txt   UNREADABLE — {0}", ex.Message);
            return false;
        }

        var hasSlot2C = false; /* NCCH primary key */
        var hasSlot3D = false; /* CIA key */
        var commonCount = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith("slot0x2CKeyX=", StringComparison.Ordinal))
            {
                hasSlot2C = KeyPresent(line, 13);
            }
            else if (line.StartsWith("slot0x3DKeyX=", StringComparison.Ordinal))
            {
                hasSlot3D = KeyPresent(line, 13);
            }
            else if (line.Length > 7 && line.StartsWith("common", StringComparison.Ordinal) &&
                     char.IsAsciiDigit(line[6]) && line[7] == '=')
            {
                if (KeyPresent(line, 8))
                {
                    ++commonCount;
                }
            }
        }

        var missing = new List<string>();
        if (!hasSlot2C)
        {
            missing.Add("slot0x2CKeyX");
        }

        if (!hasSlot3D)
        {
            missing.Add("slot0x3DKeyX");
        }

        if (commonCount == 0)
        {
            missing.Add("common<slot> keys");
        }

        if (missing.Count == 0)
        {
            Console.WriteLine("aes_keys.txt   OK — slot0x2CKeyX present, slot0x3DKeyX present, {0} common key(s)", commonCount);
            return true;
        }

        Console.WriteLine("aes_keys.txt   INVALID — missing {0}", string.Join(", ", missing));
        return false;
    }

    /* mirrors Hash3DS.Read128BitHex + KeyIsPresent: the first parsed byte of
     * the 32-hex value decides presence */
    private static bool KeyPresent(string line, int valueStart)
    {
        var hex = line.Substring(valueStart);
        if (hex.Length < 2)
        {
            return false;
        }

        return byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var first) &&
               first != 0;
    }

    /* seeddb.bin layout (Hash3DS.cs): 4-byte little-endian seed count, 12
     * bytes padding, then count entries of 8 (programId) + 16 (seed) + 8
     * (padding) bytes. Missing is fine (optional); corrupt is not. */
    private static bool CheckSeedDb(string systemDir)
    {
        var path = Path.Combine(systemDir, "seeddb.bin");
        if (!File.Exists(path))
        {
            Console.WriteLine("seeddb.bin    missing (optional — only needed for seed-encrypted titles)");
            return true;
        }

        byte[] data;
        try
        {
            data = File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine("seeddb.bin    UNREADABLE — {0}", ex.Message);
            return false;
        }

        if (data.Length < 16)
        {
            Console.WriteLine("seeddb.bin    INVALID — file is too short to hold the seed table header");
            return false;
        }

        var count = (uint)(data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24));
        if (16L + count * 24 > data.Length)
        {
            Console.WriteLine("seeddb.bin    INVALID — header claims {0} seed(s) but the file is truncated", count);
            return false;
        }

        Console.WriteLine("seeddb.bin    OK — {0} seed(s)", count);
        return true;
    }

    private static void Usage()
    {
        Console.WriteLine("Usage: RetroAchievementsSharp {0} [options]", Name);
        Console.WriteLine();
        Console.WriteLine("Checks the 3DS key files (aes_keys.txt, seeddb.bin) in the system");
        Console.WriteLine("directory, the same way the 3DS hashing engine will use them.");
        Console.WriteLine();
        Console.WriteLine("  -s <systempath>  supplementary files directory (default: current)");
        Console.WriteLine("  -h, --help       show this help");
        Console.WriteLine();
        Console.WriteLine("Exit code 0 when the key files are usable, 1 otherwise. A missing");
        Console.WriteLine("seeddb.bin is only a warning — it is required just for seed-encrypted");
        Console.WriteLine("titles.");
    }
}
