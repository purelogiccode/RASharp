// Tier-3 real-world spot check (Phase E5): hash a sample of each local
// cartridge library with RetroAchievementsSharp.Cli.exe, look the hashes up in a snapshot of
// the official RetroAchievements game database (ID/Title/ConsoleID/Hashes —
// the schema emitted by the RetroAchievements.DataFetcher tool), and assert
// that every library produces at least one official-hash match.
//
// The snapshot is *not* committed: it is a 4 MB user-generated artifact.
// The test locates it via (1) the RASHARP_RA_DB environment variable,
// (2) the DataFetcher publish output on this machine, or (3) a copy at
// References\RetroAchievements.json. It skips (with a note) when neither
// the database nor the library paths are present, so the suite stays green
// on machines without them.

using Xunit.Abstractions;

namespace RetroAchievementsSharp.Tests.Parity;

/// <summary>Tier-3 real-world spot check (Phase E5): hash a real sample of each local cartridge library with RetroAchievementsSharp.Cli.exe, look the hash up in the official RetroAchievements game database snapshot, and assert official-hash matches per library. Skips when the snapshot or libraries are absent.</summary>
public class TestPublishedHashMatch
{
    private readonly ITestOutputHelper _output;

    public TestPublishedHashMatch(ITestOutputHelper output)
    {
        _output = output;
    }

    /* candidate locations for the official RA game database snapshot
     * (JSON with ID/Title/ConsoleID/ConsoleName/Hashes fields per game) */
    private static readonly string[] DatabaseCandidates =
    [
        Environment.GetEnvironmentVariable("RASHARP_RA_DB") ?? "",
        @"C:\Sincronizar\source\repos\CSharp_SimpleLauncher\Tools\RetroAchievements.DataFetcher\bin\Publish\win-x64\RetroAchievements.json",
        Path.Combine(ParityHarness.RepoRoot, "References", "RetroAchievements.json")
    ];

    private static readonly string? DatabasePath = Array.Find(DatabaseCandidates, path => path.Length > 0 && File.Exists(path));

    /// <summary>The cartridge libraries under test: console id, display name, library path, sample size.</summary>
    /// <returns>the test rows</returns>
    public static IEnumerable<object[]> Libraries()
    {
        yield return [1u, "Genesis/Mega Drive", @"G:\Sega Genesis", 50];
        yield return [2u, "Nintendo 64", @"G:\Nintendo 64", 50];
        yield return [3u, "SNES/Super Famicom", @"G:\Nintendo SNES", 50];
        yield return [4u, "Game Boy", @"G:\Nintendo Game Boy", 50];
        yield return [5u, "Game Boy Advance", @"G:\Nintendo Game Boy Advance", 50];
        yield return [6u, "Game Boy Color", @"G:\Nintendo Game Boy Color", 50];
        yield return [7u, "NES/Famicom", @"G:\Nintendo NES", 50];
        yield return [8u, "PC Engine/TurboGrafx-16", @"G:\NEC PC Engine", 50];
        yield return [10u, "32X", @"G:\Sega Genesis 32X", 50];
        yield return [11u, "Master System", @"G:\Sega Master System", 50];
        yield return [13u, "Atari Lynx", @"G:\Atari Lynx", 50];
        yield return [14u, "Neo Geo Pocket Color", @"G:\SNK Neo Geo Pocket Color", 50];
        yield return [15u, "Game Gear", @"G:\Sega Game Gear", 50];
        yield return [25u, "Atari 2600", @"G:\Atari 2600", 50];
        yield return [51u, "Atari 7800", @"G:\Atari 7800", 50];
    }

    /// <summary>Hashes a real sample with RetroAchievementsSharp.Cli.exe and requires at least one hash to match the published RetroAchievements database.</summary>
    /// <param name="consoleId">the console identifier</param>
    /// <param name="consoleName">the console display name</param>
    /// <param name="libraryPath">the library path</param>
    /// <param name="sampleSize">the number of files to hash</param>
    [Theory]
    [MemberData(nameof(Libraries))]
    public void SampleMatchesPublishedDatabase(uint consoleId, string consoleName, string libraryPath, int sampleSize)
    {
        if (!OperatingSystem.IsWindows())
        {
            _output.WriteLine("SKIPPED (Windows host required): " + consoleName);
            return;
        }

        if (DatabasePath is null)
        {
            _output.WriteLine("SKIPPED (no RetroAchievements.json snapshot; set RASHARP_RA_DB or copy one to References): " + consoleName);
            return;
        }

        if (!Directory.Exists(libraryPath))
        {
            _output.WriteLine($"SKIPPED (library path not present: {libraryPath}): " + consoleName);
            return;
        }

        var files = Directory.EnumerateFiles(libraryPath, "*", SearchOption.TopDirectoryOnly)
            .OrderBy(file => file, StringComparer.Ordinal)
            .Take(sampleSize)
            .ToArray();

        if (files.Length == 0)
        {
            _output.WriteLine("SKIPPED (library is empty): " + consoleName);
            return;
        }

        var matched = 0;
        var unmatched = 0;
        var unhashable = 0;
        foreach (var file in files)
        {
            ParityHarness.Result result = ParityHarness.Run(ParityHarness.CliPath, ["identify", consoleId.ToString(), file, "--db", DatabasePath], Path.GetDirectoryName(file)!);
            var outText = ParityHarness.ToText(result.StdOut);
            if (outText.Contains("=>", StringComparison.Ordinal) && outText.Contains("(ID ", StringComparison.Ordinal))
            {
                ++matched;
            }
            else if (outText.Contains("=> not found", StringComparison.Ordinal))
            {
                ++unmatched;
            }
            else
            {
                /* no hash produced — multi-file zips are refused by the engine
                 * ("contains 2 files, determining which to open is not
                 * supported") in both RAHasher and RetroAchievementsSharp; that is expected */
                ++unhashable;
            }
        }

        _output.WriteLine($"{consoleName} (id {consoleId}): {files.Length} file(s), {matched} matched, {unmatched} unmatched, {unhashable} unhashable");

        Assert.True(matched > 0,
            $"[{consoleName}] no hash matched the published database across {files.Length} file(s); " +
            $"matched={matched} unmatched={unmatched} unhashable={unhashable} — investigate before accepting.");
    }
}