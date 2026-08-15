// Phase 8 — Tier-2 parity harness infrastructure.
//
// Runs both executables — the RetroAchievementsSharp port and the reference RAHasher 1.8.3
// build (References\RAHasher.exe; GPL-3.0, used as test oracle only, never
// shipped) — with identical arguments and asserts byte-identical stdout and
// stderr plus equal exit codes.
//
// Oracle notes:
//  * The oracle is built from References\RAHasher-1.8.3 (Makefile.RAHasher
//    HAVE_CHD=1). It accepts console keys ("NES"), numeric IDs, and "?"
//    auto-detection, matching the ported CLI.
//  * Some prebuilt 1.8.3 binaries only accept numeric console IDs. The
//    harness probes key support once and falls back to numeric IDs.
//  * If the oracle binary is absent, parity tests skip with a note.

using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace RetroAchievementsSharp.Tests.Parity;

/// <summary>Phase 8 — Tier-2 parity harness infrastructure. Runs both executables — the RetroAchievementsSharp port and the reference RAHasher 1.8.3 build (References\RAHasher.exe; GPL-3.</summary>
public static class ParityHarness
{
    public static readonly string RepoRoot = FindRepoRoot();
    public static readonly string? OraclePath = FindOracle();
    public static readonly string CliPath = FindCli();

    /// <summary>True when the parity tests can run: a Windows host with an oracle binary.
    /// (The oracle is a Windows PE; on Linux the suite skips parity and runs the vectors.)</summary>
    public static bool IsOracleUsable => OraclePath is not null && OperatingSystem.IsWindows();

    private static bool? _sOracleAcceptsKeys;
    private static bool? _sOracleAcceptsQuestion;

    /// <summary>True when the oracle build supports "?" auto-detect mode (1.8.3-era binaries
    /// predating key support also predate it). Probed once.</summary>
    public static bool OracleAcceptsQuestion(string probeFile)
    {
        if (_sOracleAcceptsQuestion is null)
        {
            if (OraclePath is null)
            {
                _sOracleAcceptsQuestion = true;
            }
            else
            {
                Result result = Run(OraclePath, ["?", probeFile], Path.GetDirectoryName(probeFile)!);
                _sOracleAcceptsQuestion = result.ExitCode == 0 &&
                                          ToText(result.StdOut).Trim().Length == 32;
            }
        }

        return _sOracleAcceptsQuestion.Value;
    }

    /// <summary>True when the oracle build accepts console keys ("NES"); false when it only
    /// accepts numeric IDs. Probed once using nes.nes (deterministically detectable).</summary>
    public static bool OracleAcceptsKeys(string probeFile)
    {
        if (_sOracleAcceptsKeys is null)
        {
            if (OraclePath is null)
            {
                _sOracleAcceptsKeys = true;
            }
            else
            {
                Result result = Run(OraclePath, ["GB", probeFile], Path.GetDirectoryName(probeFile)!);
                _sOracleAcceptsKeys = result.ExitCode == 0 &&
                                      ToText(result.StdOut).Trim().Length == 32;
            }
        }

        return _sOracleAcceptsKeys.Value;
    }

    /// <summary>Phase 8 — Tier-2 parity harness infrastructure. Runs both executables — the RetroAchievementsSharp port and the reference RAHasher 1.8.3 build (References\RAHasher.exe; GPL-3.</summary>
    public sealed record Result(int ExitCode, byte[] StdOut, byte[] StdErr);

    /// <summary>Executes the CLI argument loop.</summary>
    /// <param name="exe">the exe parameter</param>
    /// <param name="args">the command-line arguments</param>
    /// <param name="workingDir">the working dir parameter</param>
    /// <returns>the result</returns>
    public static Result Run(string exe, IReadOnlyList<string> args, string workingDir)
    {
        var psi = new ProcessStartInfo(exe)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        /* test runs must never POST bug reports or usage stats to the real APIs */
        psi.Environment["RASHARP_BUGREPORT_DISABLE"] = "1";
        psi.Environment["RASHARP_STATS_DISABLE"] = "1";

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using Process process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start " + exe);

        var stdout = new MemoryStream();
        var stderr = new MemoryStream();
        Task copyOut = process.StandardOutput.BaseStream.CopyToAsync(stdout);
        Task copyErr = process.StandardError.BaseStream.CopyToAsync(stderr);

        if (!process.WaitForExit(180_000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
            }

            throw new TimeoutException($"{exe} {string.Join(' ', args)} did not exit within 180s");
        }

        Task.WaitAll(copyOut, copyErr);
        return new Result(process.ExitCode, stdout.ToArray(), stderr.ToArray());
    }

    /// <summary>to text.</summary>
    /// <param name="bytes">the bytes parameter</param>
    /// <returns>the generated value</returns>
    public static string ToText(byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "RetroAchievementsSharp.sln")))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("RetroAchievementsSharp.sln not found above " + AppContext.BaseDirectory);
    }

    private static string? FindOracle()
    {
        /* The definitive 1.8.3 oracle is built from the pinned sources
         * (References\RAHasher-1.8.3, Makefile.RAHasher HAVE_CHD=1). Fall back to
         * any other RAHasher 1.8.3 binary the user provides. */
        var env = Environment.GetEnvironmentVariable("RASHARP_ORACLE");
        if (!string.IsNullOrEmpty(env) && File.Exists(env))
            return env;

        /* Part II: the rcheevos 12.4.0-built oracle is the new source of truth */
        var v124 = Path.Combine(RepoRoot, "References", "rcheevos-12.4.0", "bin64", "RAHasher.exe");
        if (File.Exists(v124))
            return v124;

        var sourceBuilt = Path.Combine(RepoRoot, "References", "RAHasher-1.8.3", "bin64", "RAHasher.exe");
        if (File.Exists(sourceBuilt))
            return sourceBuilt;

        var legacy = Path.Combine(RepoRoot, "References", "RAHasher.exe");
        return File.Exists(legacy) ? legacy : null;
    }

    private static string FindCli()
    {
        var env = Environment.GetEnvironmentVariable("RASHARP_CLI");
        if (!string.IsNullOrEmpty(env) && File.Exists(env))
            return env;

        /* prefer the CLI built for the TFM the test assembly itself runs on,
         * then fall back through the remaining targets */
        var currentTfm = AppContext.TargetFrameworkName;
        var tfms = new List<string> { currentTfm! }
            .Concat(new[] { "net10.0", "net9.0", "net8.0" }.Where(tfm => !string.Equals(tfm, currentTfm, StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal).ToArray();

        foreach (var config in new[] { "Debug", "Release" })
        {
            foreach (var tfm in tfms)
            {
                var path = Path.Combine(RepoRoot, "RetroAchievementsSharp.Cli", "bin", config, tfm, "RetroAchievementsSharp.Cli.exe");
                if (File.Exists(path))
                    return path;
            }
        }

        throw new FileNotFoundException(
            "RetroAchievementsSharp.Cli.exe not found — build the solution first (dotnet build RetroAchievementsSharp.sln) or set RASHARP_CLI.");
    }
}
