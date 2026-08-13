// Tests for the `RASharp checkkeys` subcommand (a RASharp extension — not
// part of the RAHasher 1.8.3 parity surface). It validates the 3DS key
// files (aes_keys.txt, seeddb.bin) the way the hashing engine will use them.

using RASharp.Cli;

namespace RASharp.Tests;

/// <summary>Tests for the `RASharp checkkeys` subcommand (a RASharp extension — not part of the RAHasher 1.8.3 parity surface). It validates the 3DS key files (aes_keys.txt, seeddb</summary>
public class TestCheckKeys : IDisposable
{
    private readonly string _root;

    public TestCheckKeys()
    {
        _root = Path.Combine(Path.GetTempPath(), "rasharp_checkkeys_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            /* best effort cleanup */
        }
    }

    private static (int ExitCode, string StdOut, string StdErr) RunCheckKeys(params string[] args)
    {
        var oldOut = Console.Out;
        var oldErr = Console.Error;
        try
        {
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var exit = CheckKeysCommand.Run(args);
            return (exit, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetError(oldErr);
        }
    }

    private void WriteAesKeys(params string[] lines)
    {
        File.WriteAllLines(Path.Combine(_root, "aes_keys.txt"), lines);
    }

    /* a seeddb.bin with the given seed count and a matching byte length */
    private void WriteSeedDb(int seedCount, bool truncated)
    {
        var data = new byte[16 + seedCount * 24 - (truncated ? 8 : 0)];
        data[0] = (byte)(seedCount & 0xFF);
        data[1] = (byte)((seedCount >> 8) & 0xFF);
        File.WriteAllBytes(Path.Combine(_root, "seeddb.bin"), data);
    }

    private const string Key2C = "01000000000000000000000000000000"; /* first byte != 0 -> present */
    private const string Key3D = "02000000000000000000000000000000";
    private const string KeyCommon = "03000000000000000000000000000000";

    private static string[] GoodAesLines =>
    [
        "slot0x2CKeyX=" + Key2C,
        "slot0x3DKeyX=" + Key3D,
        "common0=" + KeyCommon,
        "common1=" + KeyCommon
    ];

    /* ========================================================================= */

    /// <summary>Tests a valid key set.</summary>
    [Fact]
    public void CheckKeysOk()
    {
        WriteAesKeys(GoodAesLines);
        WriteSeedDb(3, truncated: false);

        var (exit, stdout, _) = RunCheckKeys("-s", _root);

        Assert.Equal(0, exit);
        Assert.Contains("aes_keys.txt   OK", stdout, StringComparison.Ordinal);
        Assert.Contains("2 common key(s)", stdout, StringComparison.Ordinal);
        Assert.Contains("seeddb.bin    OK — 3 seed(s)", stdout, StringComparison.Ordinal);
        Assert.Contains("3DS keys OK", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests a missing aes_keys.txt.</summary>
    [Fact]
    public void CheckKeysMissingAesKeys()
    {
        var (exit, stdout, _) = RunCheckKeys("-s", _root);

        Assert.Equal(1, exit);
        Assert.Contains("aes_keys.txt   MISSING", stdout, StringComparison.Ordinal);
        Assert.Contains("3DS keys INVALID", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests that a missing required key slot fails the check.</summary>
    [Fact]
    public void CheckKeysMissingSlot2C()
    {
        WriteAesKeys("slot0x3DKeyX=" + Key3D, "common0=" + KeyCommon);

        var (exit, stdout, _) = RunCheckKeys("-s", _root);

        Assert.Equal(1, exit);
        Assert.Contains("INVALID — missing slot0x2CKeyX", stdout, StringComparison.Ordinal);
    }

    /* the engine treats a key whose first byte is 0 as absent (KeyIsPresent) */
    /// <summary>Tests that all-zero keys count as missing.</summary>
    [Fact]
    public void CheckKeysAllZeroKeysCountAsMissing()
    {
        WriteAesKeys(
            "slot0x2CKeyX=00000000000000000000000000000000",
            "slot0x3DKeyX=00000000000000000000000000000000",
            "common0=00000000000000000000000000000000");

        var (exit, stdout, _) = RunCheckKeys("-s", _root);

        Assert.Equal(1, exit);
        Assert.Contains("missing slot0x2CKeyX, slot0x3DKeyX, common<slot> keys", stdout, StringComparison.Ordinal);
    }

    /* seeddb.bin is optional — its absence is only a warning */
    /// <summary>Tests that a missing seeddb.bin is a warning, not a failure.</summary>
    [Fact]
    public void CheckKeysSeedDbMissingIsWarning()
    {
        WriteAesKeys(GoodAesLines);

        var (exit, stdout, _) = RunCheckKeys("-s", _root);

        Assert.Equal(0, exit);
        Assert.Contains("seeddb.bin    missing (optional", stdout, StringComparison.Ordinal);
        Assert.Contains("3DS keys OK", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests that a truncated seeddb.bin fails the check.</summary>
    [Fact]
    public void CheckKeysSeedDbTruncated()
    {
        WriteAesKeys(GoodAesLines);
        WriteSeedDb(5, truncated: true);

        var (exit, stdout, _) = RunCheckKeys("-s", _root);

        Assert.Equal(1, exit);
        Assert.Contains("seeddb.bin    INVALID", stdout, StringComparison.Ordinal);
        Assert.Contains("truncated", stdout, StringComparison.Ordinal);
    }

    /* unexpected positional arguments are rejected via stderr (Console.Error) */
    /// <summary>Tests that an unexpected positional argument fails with a stderr message.</summary>
    [Fact]
    public void CheckKeysUnexpectedArgument()
    {
        var (exit, _, stderr) = RunCheckKeys("bogus.txt");

        Assert.Equal(1, exit);
        Assert.Contains("Unexpected argument", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests the default current-directory behavior and help.</summary>
    [Fact]
    public void CheckKeysDefaultDirAndHelp()
    {
        var (exit, stdout, _) = RunCheckKeys("--help");

        Assert.Equal(0, exit);
        Assert.Contains("Usage: RASharp checkkeys", stdout, StringComparison.Ordinal);
    }
}
