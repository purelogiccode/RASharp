// Real-ROM parity tests: the first 50 files of each local console library
// (G:\Sega Genesis, G:\Nintendo 64, ...) are hashed by both the reference
// RAHasher 1.8.3 oracle and the built RASharp.exe with identical
// arguments; stdout, stderr, and exit code must match byte-for-byte.
//
// These tests are environment-dependent: they skip (with a note) when the
// library paths or the 1.8.3 oracle binary are not present, so the suite
// stays green on machines without the ROM libraries. The synthetic corpus
// in TestParity remains the portable parity suite.

using Xunit.Abstractions;

namespace RASharp.Tests.Parity;

/// <summary>Real-ROM parity tests: the first 50 files of each local console library (G:\Sega Genesis, G:\Nintendo 64, ...) are hashed by both the reference RAHasher 1.8.3 or</summary>
public class TestRealRomParity
{
    private readonly ITestOutputHelper _output;

    public TestRealRomParity(ITestOutputHelper output)
    {
        _output = output;
    }


    /* the user-specified 1.8.3 oracle (ParityHarness.OraclePath may prefer a
     * newer rcheevos build — these tests pin the 1.8.3 binary explicitly) */
    private static readonly string Oracle183 =
        Path.Combine(ParityHarness.RepoRoot, "References", "RAHasher-1.8.3", "RAHasher.exe");

    /// <summary>The console libraries under test: console id, display name, library path, recursive, system dir (3DS keys).</summary>
    /// <returns>the test rows</returns>
    public static IEnumerable<object[]> Libraries()
    {
        /* the 3DS rows need aes_keys.txt/seeddb.bin — the user-supplied files at the repo root */
        var systemDir = Path.Combine(ParityHarness.RepoRoot, "");
        yield return [1u, "Genesis/Mega Drive", @"G:\Sega Genesis", false, "", 50];
        yield return [2u, "Nintendo 64", @"G:\Nintendo 64", false, "", 50];
        yield return [3u, "SNES/Super Famicom", @"G:\Nintendo SNES", false, "", 50];
        yield return [4u, "Game Boy", @"G:\Nintendo Game Boy", false, "", 50];
        yield return [5u, "Game Boy Advance", @"G:\Nintendo Game Boy Advance", false, "", 50];
        yield return [6u, "Game Boy Color", @"G:\Nintendo Game Boy Color", false, "", 50];
        yield return [7u, "NES/Famicom", @"G:\Nintendo NES", false, "", 50];
        yield return [8u, "PC Engine/TurboGrafx-16", @"G:\NEC PC Engine", false, "", 50];
        yield return [9u, "Sega CD", @"I:\Sega Genesis CD", false, "", 50];
        yield return [10u, "32X", @"G:\Sega Genesis 32X", false, "", 50];
        yield return [11u, "Master System", @"G:\Sega Master System", false, "", 50];
        yield return [12u, "PlayStation", @"X:\Sony PlayStation 1", false, "", 50];
        yield return [13u, "Atari Lynx", @"G:\Atari Lynx", false, "", 50];
        yield return [14u, "Neo Geo Pocket", @"G:\SNK Neo Geo Pocket", false, "", 50];
        yield return [14u, "Neo Geo Pocket Color", @"G:\SNK Neo Geo Pocket Color", false, "", 50];
        yield return [15u, "Game Gear", @"G:\Sega Game Gear", false, "", 50];
        yield return [17u, "Atari Jaguar", @"G:\Atari Jaguar", false, "", 50];
        yield return [18u, "Nintendo DS", @"G:\Nintendo DS", false, "", 50];
        yield return [21u, "PlayStation 2", @"X:\Sony PlayStation 2", false, "", 50];
        yield return [22u, "Xbox", @"J:\Microsoft Xbox", false, "", 50];
        yield return [23u, "Magnavox Odyssey 2", @"G:\Magnavox Odyssey 2", false, "", 50];
        yield return [25u, "Atari 2600", @"G:\Atari 2600", false, "", 50];
        yield return [27u, "Arcade", @"G:\MAME\MAME Roms", false, "", 50];
        yield return [28u, "Virtual Boy", @"G:\Nintendo Virtual Boy", false, "", 50];
        yield return [29u, "MSX", @"G:\Microsoft MSX", false, "", 50];
        yield return [29u, "MSX2", @"G:\Microsoft MSX2", false, "", 50];
        yield return [30u, "Commodore 64", @"G:\Commodore 64", false, "", 50];
        yield return [33u, "SG-1000", @"G:\Sega SG-1000 SC-3000", false, "", 50];
        yield return [35u, "Amiga", @"G:\Commodore Amiga", false, "", 50];
        yield return [36u, "Atari ST", @"G:\Atari ST", false, "", 50];
        yield return [37u, "Amstrad CPC", @"G:\Amstrad CPC", false, "", 50];
        yield return [39u, "Saturn", @"I:\Sega Saturn", false, "", 50];
        yield return [40u, "Dreamcast", @"X:\Sega Dreamcast", false, "", 50];
        yield return [41u, "PlayStation Portable", @"X:\Sony PSP", false, "", 50];
        yield return [41u, "PSP Minis", @"X:\Sony PSP Minis", false, "", 50];
        yield return [41u, "PSP PSN", @"X:\Sony PSP PSN", false, "", 50];
        yield return [42u, "Philips CD-i", @"I:\Philips CD-i", false, "", 50];
        yield return [43u, "3DO Interactive Multiplayer", @"I:\Panasonic 3DO", false, "", 50];
        yield return [44u, "ColecoVision", @"G:\ColecoVision", false, "", 50];
        yield return [45u, "Intellivision", @"G:\Mattel Intellivision", false, "", 50];
        yield return [48u, "PC-9800", @"F:\NEC PC-98", false, "", 50];
        /* MAME software-list CHDs are stored one game per subfolder */
        yield return [48u, "PC-9800 (MAME pc98_cd)", @"G:\MAME\MAME Software List CHDs\pc98_cd", true, "", 50];
        yield return [49u, "PC-FX", @"G:\NEC PC-FX", false, "", 50];
        yield return [50u, "Atari 5200", @"G:\Atari 5200", false, "", 50];
        yield return [51u, "Atari 7800", @"G:\Atari 7800", false, "", 50];
        yield return [52u, "Sharp X68000 (MAME x68k_flop)", @"G:\MAME\MAME Software List Roms\x68k_flop", false, "", 50];
        yield return [53u, "WonderSwan", @"G:\Bandai WonderSwan", false, "", 50];
        yield return [53u, "WonderSwan Color", @"G:\Bandai WonderSwan Color", false, "", 50];
        yield return [56u, "Neo Geo CD", @"I:\SNK Neo Geo CD", false, "", 50];
        yield return [58u, "FM Towns", @"G:\Fujitsu - FM-Towns", false, "", 50];
        yield return [59u, "ZX Spectrum", @"G:\Sinclair ZX Spectrum", false, "", 50];
        yield return [68u, "Sega Pico", @"G:\Sega PICO", false, "", 50];
        yield return [70u, "Zeebo", @"G:\Zeebo", false, "", 50];
        yield return [76u, "PC Engine CD/TurboGrafx-CD", @"I:\NEC PC Engine CD", false, "", 50];
        yield return [77u, "Atari Jaguar CD", @"J:\Atari Jaguar CD", false, "", 50];
        yield return [78u, "Nintendo DSi", @"G:\Nintendo DS", false, "", 50];
        yield return [81u, "Famicom Disk System", @"G:\Nintendo Family Computer Disk System", false, "", 50];
        /* PS3 is unsupported in both engines — the case pins that both reject
         * id 82 with the same error instead of silently diverging */
        yield return [82u, "PlayStation 3 (unsupported in both)", @"X:\Sony PlayStation 3", false, "", 50];
        /* 3DS: requires the user-supplied aes_keys.txt/seeddb.bin (repo root) —
         * the CDN/DSiWare libraries are multi-entry zips that both binaries
         * reject identically (returning entire zip file -> Not a 3DS ROM) */
        yield return [62u, "Nintendo 3DS", @"F:\Nintendo 3DS", true, systemDir, 25];
        yield return [62u, "Nintendo 3DS CDN", @"F:\Nintendo 3DS CDN", true, systemDir, 25];
        yield return [62u, "Nintendo 3DS DSiWare", @"F:\Nintendo 3DS DSiWare", false, systemDir, 25];
    }

    /// <summary>Hashes the first files of a library with both executables and requires identical output.</summary>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="consoleName">the console display name</param>
    /// <param name="libraryPath">the library path</param>
    /// <param name="recursive">when true, files are enumerated recursively (MAME software-list layouts)</param>
    /// <param name="systemDir">when set, both executables receive `-s` with this system dir (3DS keys)</param>
    /// <param name="fileCount">the number of files to hash (the 3DS library uses fewer: multi-GiB extraction is slow)</param>
    [Theory]
    [MemberData(nameof(Libraries))]
    public void FirstFilesMatchOracle(uint consoleId, string consoleName, string libraryPath, bool recursive, string systemDir = "", int fileCount = 50)
    {
        if (!OperatingSystem.IsWindows())
        {
            _output.WriteLine("SKIPPED (Windows host required): " + consoleName);
            return;
        }

        if (!File.Exists(Oracle183))
        {
            _output.WriteLine(@"SKIPPED (no References\RAHasher-1.8.3\RAHasher.exe): " + consoleName);
            return;
        }

        if (!Directory.Exists(libraryPath))
        {
            _output.WriteLine($"SKIPPED (library path not present: {libraryPath}): " + consoleName);
            return;
        }

        if (systemDir.Length > 0 &&
            (!File.Exists(Path.Combine(systemDir, "aes_keys.txt")) || !File.Exists(Path.Combine(systemDir, "seeddb.bin"))))
        {
            _output.WriteLine($"SKIPPED (3DS keys not present in {systemDir}): " + consoleName);
            return;
        }

        var files = Directory.EnumerateFiles(libraryPath, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .OrderBy(file => file, StringComparer.Ordinal)
            .Take(fileCount)
            .ToArray();

        if (files.Length == 0)
        {
            _output.WriteLine("SKIPPED (library is empty): " + consoleName);
            return;
        }

        var args = new List<string>();
        if (systemDir.Length > 0)
        {
            args.Add("-s");
            args.Add(systemDir);
        }

        args.Add(consoleId.ToString());
        args.AddRange(files);

        ParityHarness.Result oracle = ParityHarness.Run(Oracle183, args, libraryPath);
        ParityHarness.Result cli = ParityHarness.Run(ParityHarness.CliPath, args, libraryPath);

        var oracleOut = ParityHarness.ToText(oracle.StdOut);
        var cliOut = ParityHarness.ToText(cli.StdOut);
        var oracleErr = ParityHarness.ToText(oracle.StdErr);
        var cliErr = ParityHarness.ToText(cli.StdErr);

        Assert.True(oracle.ExitCode == cli.ExitCode,
            $"[{consoleName}] exit code: oracle={oracle.ExitCode} cli={cli.ExitCode}\n" +
            $"oracle stdout: {oracleOut}\noracle stderr: {oracleErr}\ncli stdout: {cliOut}\ncli stderr: {cliErr}");
        Assert.True(string.Equals(oracleOut, cliOut, StringComparison.Ordinal),
            $"[{consoleName}] stdout differs across {files.Length} file(s).\noracle: {oracleOut}\ncli:    {cliOut}");
        Assert.True(string.Equals(oracleErr, cliErr, StringComparison.Ordinal),
            $"[{consoleName}] stderr differs.\noracle: {oracleErr}\ncli:    {cliErr}");

        _output.WriteLine($"{consoleName} (id {consoleId}): {files.Length} file(s) matched the oracle");
        foreach (var line in cliOut.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            _output.WriteLine("  " + line);
        }
    }
}
