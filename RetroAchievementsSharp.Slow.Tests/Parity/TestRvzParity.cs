// RVZ validation: for a sample of real GameCube/Wii RVZ images, the Dolphin-
// converted ISO and the RVZ hashed live through RVZSharp (RvzFilereader) must
// produce the identical RA hash. This is the rvz->iso-free claim: hashing the
// container directly (decode-on-read) must equal hashing the converted ISO.
//
// Environment-dependent (skips with a note when the libraries, DolphinTool,
// or the built CLI are absent) — mirrors TestRealRomParity's convention. The
// portable synthetic suite in TestParity remains the primary parity gate.

using System.Diagnostics;
using System.Text;
using Xunit.Abstractions;

namespace RetroAchievementsSharp.Tests.Parity;

/// <summary>RVZ validation: for a sample of real GameCube/Wii RVZ images, the Dolphin-converted ISO hash must equal the RVZ live hash through RvzFilereader (no conversion). Skips when the libraries, DolphinTool, or the CLI are absent.</summary>
public class TestRvzParity
{
    private readonly ITestOutputHelper _output;

    public TestRvzParity(ITestOutputHelper output)
    {
        _output = output;
    }

    private static readonly string DolphinTool =
        Path.Combine(ParityHarness.RepoRoot, "References", "DolphinTool.exe");

    /* three GameCube + three Wii images (first alphabetically), exercising
     * both filereader-only disc paths */
    /// <summary>The files under test.</summary>
    /// <returns>the test rows</returns>
    public static IEnumerable<object[]> Files()
    {
        /* empty when the libraries are not mounted — discovery stays green
         * and the individual cases skip with a note */
        if (!Directory.Exists(@"F:\Nintendo GameCube") && !Directory.Exists(@"F:\Nintendo Wii"))
            yield break;

        var gc = Directory.EnumerateFiles(@"F:\Nintendo GameCube", "*.rvz", SearchOption.TopDirectoryOnly)
            .OrderBy(file => file, StringComparer.Ordinal).Take(3)
            .Select(file => (16u, file)); /* RcConsoleGamecube */
        var wii = Directory.EnumerateFiles(@"F:\Nintendo Wii", "*.rvz", SearchOption.TopDirectoryOnly)
            .OrderBy(file => file, StringComparer.Ordinal).Take(3)
            .Select(file => (19u, file)); /* RcConsoleWii */

        foreach (var (consoleId, file) in gc.Concat(wii))
            yield return [consoleId, file];
    }

    /// <summary>Converts the RV to a temp ISO with DolphinTool and asserts the direct RVZ hash equals the converted-ISO hash.</summary>
    /// <param name="consoleId">the console id (16 GameCube, 19 Wii)</param>
    /// <param name="rvzPath">the RVZ disc image under test</param>
    [Theory]
    [MemberData(nameof(Files))]
    public void RvzHashMatchesConvertedIso(uint consoleId, string rvzPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            _output.WriteLine("SKIPPED (Windows host required): " + rvzPath);
            return;
        }

        if (!File.Exists(DolphinTool))
        {
            _output.WriteLine($"SKIPPED (no References\\DolphinTool.exe): {rvzPath}");
            return;
        }

        if (!File.Exists(rvzPath))
        {
            _output.WriteLine($"SKIPPED (RVZ not present: {rvzPath})");
            return;
        }

        var isoPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(rvzPath) + ".rvztest.iso");
        try
        {
            ConvertToIso(rvzPath, isoPath);
            _output.WriteLine($"Converted {Path.GetFileName(rvzPath)} -> {Path.GetFileName(isoPath)} ({new FileInfo(isoPath).Length} bytes)");

            /* the converted ISO is hashed with the default filereader; the RVZ
             * with the RVZSharp-backed one — both through the built CLI */
            var isoHash = RunHash(consoleId, isoPath);
            var rvzHash = RunHash(consoleId, rvzPath);

            _output.WriteLine($"console {consoleId}: iso={isoHash} rvz={rvzHash}");
            Assert.True(isoHash.Length == 32, "ISO hash must be 32 hex chars: " + isoHash);
            Assert.True(rvzHash.Length == 32, "RVZ hash must be 32 hex chars: " + rvzHash);
            Assert.Equal(isoHash, rvzHash);
        }
        finally
        {
            try
            {
                File.Delete(isoPath);
            }
            catch (IOException)
            {
            }
        }
    }

    /* DolphinTool convert -i <rvz> -o <iso> -f iso; give the converter a
     * generous timeout (full-disc decode of a ~1.4 GiB image) */
    private static void ConvertToIso(string rvzPath, string isoPath)
    {
        var psi = new ProcessStartInfo(DolphinTool)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("convert");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(rvzPath);
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(isoPath);
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("iso");

        using Process process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start DolphinTool");
        string sout = process.StandardOutput.ReadToEnd();
        string serr = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(900_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"DolphinTool convert timed out for {rvzPath}");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"DolphinTool convert failed for {rvzPath}: {process.ExitCode}\n{sout}\n{serr}");
        }
    }

    /* the parity harness already spawns the freshly built CLI; the 180 s
     * bound is ample: RVZ hashing streams the decoded image at SSD speed */
    private static string RunHash(uint consoleId, string path)
    {
        ParityHarness.Result result = ParityHarness.Run(ParityHarness.CliPath, [consoleId.ToString(), path], Path.GetDirectoryName(path)!);
        Assert.Equal(0, result.ExitCode);
        var hash = Encoding.UTF8.GetString(result.StdOut).Trim();
        Assert.True(hash.Length == 32, "hash must be 32 hex chars: '" + hash + "'");
        return hash;
    }
}