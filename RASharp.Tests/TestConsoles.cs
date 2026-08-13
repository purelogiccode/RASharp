// Tests for the `RASharp consoles` subcommand (a RASharp extension — not
// part of the RAHasher 1.8.3 parity surface). It dumps the console metadata
// table (id, key, group, name) as text, csv, or json.

using System.Text.Json;
using RASharp.Cli;
using RASharp.Tests.Parity;

namespace RASharp.Tests;

/// <summary>Tests for the `RASharp consoles` subcommand (a RASharp extension — not part of the RAHasher 1.8.3 parity surface). It dumps the console metadata table (id, key, grou</summary>
public class TestConsoles
{
    private static (int ExitCode, string StdOut, string StdErr) RunConsoles(params string[] args)
    {
        var oldOut = Console.Out;
        var oldErr = Console.Error;
        try
        {
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var exit = ConsolesCommand.Run(args);
            return (exit, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetError(oldErr);
        }
    }

    /* ========================================================================= */

    /// <summary>Tests the default text output.</summary>
    [Fact]
    public void ConsolesTextDefault()
    {
        var (exit, stdout, _) = RunConsoles();

        Assert.Equal(0, exit);
        var lines = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        /* header + separator + one row per console */
        Assert.Equal(Consoles.All.Length + 2, lines.Length);
        Assert.Equal(" ID Key     Group    Name", lines[0]);
        Assert.Contains(lines, line => line.TrimStart().StartsWith("7 NES", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.TrimStart().StartsWith("62 3DS", StringComparison.Ordinal));
    }

    /// <summary>Tests the csv output.</summary>
    [Fact]
    public void ConsolesCsv()
    {
        var (exit, stdout, _) = RunConsoles("--format", "csv");

        Assert.Equal(0, exit);
        var lines = stdout.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(Consoles.All.Length + 1, lines.Length);
        Assert.Equal("id,key,group,name", lines[0]);
        Assert.Contains("7,NES,Nintendo,NES/Famicom", lines, StringComparer.Ordinal);
        /* NULL-group consoles get an empty group column */
        Assert.Contains("62,3DS,,Nintendo 3DS", lines, StringComparer.Ordinal);
    }

    /// <summary>Tests the json output.</summary>
    [Fact]
    public void ConsolesJson()
    {
        var (exit, stdout, _) = RunConsoles("-f", "json");

        Assert.Equal(0, exit);
        using var doc = JsonDocument.Parse(stdout);
        var consoles = doc.RootElement.EnumerateArray().ToArray();
        Assert.Equal(Consoles.All.Length, consoles.Length);

        var nes = consoles.Single(console => console.GetProperty("id").GetInt32() == 7);
        Assert.Equal("NES", nes.GetProperty("key").GetString());
        Assert.Equal("Nintendo", nes.GetProperty("group").GetString());
        Assert.Equal("NES/Famicom", nes.GetProperty("name").GetString());

        var threeDs = consoles.Single(console => console.GetProperty("id").GetInt32() == 62);
        Assert.Equal("3DS", threeDs.GetProperty("key").GetString());
        Assert.Equal(JsonValueKind.Null, threeDs.GetProperty("group").ValueKind);
    }

    /// <summary>Tests consoles --help.</summary>
    [Fact]
    public void ConsolesHelp()
    {
        var (exit, stdout, _) = RunConsoles("--help");

        Assert.Equal(0, exit);
        Assert.Contains("Usage: RASharp consoles", stdout, StringComparison.Ordinal);
    }

    /// <summary>Tests an unknown output format.</summary>
    [Fact]
    public void ConsolesUnknownFormat()
    {
        var (exit, _, stderr) = RunConsoles("--format", "xml");

        Assert.Equal(1, exit);
        Assert.Contains("Unknown consoles format", stderr, StringComparison.Ordinal);
    }

    /// <summary>Tests an unexpected argument.</summary>
    [Fact]
    public void ConsolesUnexpectedArgument()
    {
        var (exit, _, stderr) = RunConsoles("foo");

        Assert.Equal(1, exit);
        Assert.Contains("Unexpected argument", stderr, StringComparison.Ordinal);
    }

    /* end-to-end: the Program.Run dispatch must route "consoles" to the
     * subcommand when the real CLI binary is invoked */
    /// <summary>Tests that the real CLI binary dispatches the consoles subcommand.</summary>
    [Fact]
    public void ConsolesDispatchThroughCliExe()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; /* the parity harness locates RASharp.exe (Windows apphost) */
        }

        var result = ParityHarness.Run(ParityHarness.CliPath, ["consoles"], Directory.GetCurrentDirectory());

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("NES", ParityHarness.ToText(result.StdOut), StringComparison.Ordinal);
    }
}
