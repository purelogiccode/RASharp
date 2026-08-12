// Phase 8 — Tier-2 parity tests.
//
// For every case in the generated corpus, both executables (RASharp.exe and
// the reference RAHasher 1.8.3 oracle) run with identical arguments and must
// produce byte-identical stdout/stderr and the same exit code. Cases that
// carry an ExpectedHash additionally pin the output to the hash recorded by
// the ported rcheevos vectors (i.e. both binaries must agree with upstream).
//
// The corpus is fully synthetic — built from the same data generators the
// unit vectors use (TestDataGen*, MockZipFile) plus the vendored CHD
// fixtures — so the suite runs offline and deterministically. Real-ROM
// coverage is documented in README.md.

using System.Security.Cryptography;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace RASharp.Tests.Parity;

/// <summary>Phase 8 — Tier-2 parity tests. For every case in the generated corpus, both executables (RASharp.exe and the reference RAHasher 1.8.3 oracle) run with identical</summary>
public sealed record ParityCase(
    string Label,
    string[] Flags,         // CLI flags before the console token; "{SYSTEM}" is replaced with the 3DS system dir
    string ConsoleKey,      // console key form ("?" for auto-detect)
    uint ConsoleId,         // numeric fallback for oracle builds without key support
    string[] Files,         // paths relative to the working dir (wildcards allowed)
    string? ExpectedHash,   // expected stdout for a plain single-file success (trimmed)
    bool ExpectSuccess,     // require exit code 0
    bool NormalizeNames = false, // replace "RAHasher" with "RASharp" before comparing (usage output embeds the exe name)
    string? WorkingDir = null); // default: corpus root

/// <summary>Phase 8 — Tier-2 parity tests. For every case in the generated corpus, both executables (RASharp.exe and the reference RAHasher 1.8.3 oracle) run with identical</summary>
public class TestParity
{
    private readonly ITestOutputHelper _output;

    public TestParity(ITestOutputHelper output) => _output = output;

    /* set by BuildCorpus before BuildCases runs (Lazy ordering); used to pin the
     * .neo expected hash to the MD5 of the payload alone */
    private static string s_neoPayloadHash = "";

    private sealed record CorpusPaths(string CorpusDir, string SystemDir);

    private static readonly Lazy<CorpusPaths> s_corpus = new(BuildCorpus);

/// <summary>cases.</summary>
/// <returns>the result</returns>
    public static IEnumerable<object[]> Cases()
    {
        foreach (ParityCase test in BuildCases(s_corpus.Value))
            yield return new object[] { test };
    }

/// <summary>parity.</summary>
/// <param name="test">the test parameter</param>
    [Theory]
    [MemberData(nameof(Cases))]
    public void Parity(ParityCase test)
    {
        if (!ParityHarness.IsOracleUsable)
        {
            _output.WriteLine("SKIPPED (no usable oracle: Windows host + References\\RAHasher.exe required): " + test.Label);
            return;
        }

        CorpusPaths corpus = s_corpus.Value;
        string workDir = test.WorkingDir ?? corpus.CorpusDir;
        string probe = Path.Combine(corpus.CorpusDir, "nes.nes");

        if (string.Equals(test.ConsoleKey, "?", StringComparison.Ordinal) && !ParityHarness.OracleAcceptsQuestion(probe))
        {
            _output.WriteLine("SKIPPED (oracle build lacks '?' auto-detect mode): " + test.Label);
            return;
        }

        string console = string.Equals(test.ConsoleKey, "?", StringComparison.Ordinal) ? "?" :
            ParityHarness.OracleAcceptsKeys(probe) ? test.ConsoleKey : test.ConsoleId.ToString();

        var args = new List<string>();
        foreach (string flag in test.Flags)
            args.Add(flag.Replace("{SYSTEM}", corpus.SystemDir));
        args.Add(console);
        args.AddRange(test.Files);

        ParityHarness.Result oracle = ParityHarness.Run(ParityHarness.OraclePath!, args, workDir);
        ParityHarness.Result cli = ParityHarness.Run(ParityHarness.CliPath, args, workDir);

        string oracleOut = ParityHarness.ToText(oracle.StdOut);
        string cliOut = ParityHarness.ToText(cli.StdOut);
        string oracleErr = ParityHarness.ToText(oracle.StdErr);
        string cliErr = ParityHarness.ToText(cli.StdErr);

        if (test.NormalizeNames)
        {
            oracleOut = oracleOut.Replace("RAHasher", "RASharp");
            oracleErr = oracleErr.Replace("RAHasher", "RASharp");
        }

        Assert.True(oracle.ExitCode == cli.ExitCode,
            $"[{test.Label}] exit code: oracle={oracle.ExitCode} cli={cli.ExitCode}\n" +
            $"oracle stdout: {oracleOut}\noracle stderr: {oracleErr}\ncli stdout: {cliOut}\ncli stderr: {cliErr}");
        Assert.True(string.Equals(oracleOut, cliOut, StringComparison.Ordinal),
            $"[{test.Label}] stdout differs.\noracle: {oracleOut}\ncli:    {cliOut}");
        Assert.True(string.Equals(oracleErr, cliErr, StringComparison.Ordinal),
            $"[{test.Label}] stderr differs.\noracle: {oracleErr}\ncli:    {cliErr}");

        if (test.ExpectSuccess)
            Assert.True(cli.ExitCode == 0, $"[{test.Label}] expected success.\nstdout: {cliOut}\nstderr: {cliErr}");

        if (test.ExpectedHash is not null)
            Assert.True(string.Equals(cliOut.Trim(), test.ExpectedHash, StringComparison.Ordinal),
                $"[{test.Label}] expected hash {test.ExpectedHash}, got {cliOut.Trim()}");
    }

    /* ========================================================================= */

    private static List<ParityCase> BuildCases(CorpusPaths corpus)
    {
        var cases = new List<ParityCase>();

        void Add(string label, string key, uint id, string expected, params string[] files) =>
            cases.Add(new ParityCase(label, Array.Empty<string>(), key, id, files, expected, ExpectSuccess: true));

        void AddFail(string label, string key, uint id, params string[] files) =>
            cases.Add(new ParityCase(label, Array.Empty<string>(), key, id, files, ExpectedHash: null, ExpectSuccess: false));

        /* ---- generic whole-file consoles (vector table from test_hash_rom.c) ---- */
        Add("whole/A2001", "A2001", 73, "572686c3a073162e4ec6eff86e6f6e3a", "w_A2001.bin");
        Add("whole/2600", "2600", 25, "02c3f2fa186388ba8eede9147fb431c4", "w_2600.bin");
        Add("whole/JAG", "JAG", 17, "a247ec8a8c42e18fcb80702dfadac14b", "w_JAG.bin");
        Add("whole/CV", "CV", 44, "455f07d8500f3fabc54906737866167f", "w_CV.bin");
        Add("whole/ELEK", "ELEK", 75, "572686c3a073162e4ec6eff86e6f6e3a", "w_ELEK.bin");
        Add("whole/CHF", "CHF", 57, "02c3f2fa186388ba8eede9147fb431c4", "w_CHF.bin");
        Add("whole/GB", "GB", 4, "a0f425b23200568132ba76b2405e3933", "w_GB.bin");
        Add("whole/GBC", "GBC", 6, "cf86acf519625a25a17b1246975e90ae", "w_GBC.bin");
        Add("whole/GBA", "GBA", 5, "a247ec8a8c42e18fcb80702dfadac14b", "w_GBA.bin");
        Add("whole/GG", "GG", 15, "68f0f13b598e0b66461bc578375c3888", "w_GG.bin");
        Add("whole/INTV", "INTV", 45, "ce1127f881b40ce6a67ecefba50e2835", "w_INTV.bin");
        Add("whole/VC4000", "VC4000", 74, "02c3f2fa186388ba8eede9147fb431c4", "w_VC4000.bin");
        Add("whole/MO2", "MO2", 23, "572686c3a073162e4ec6eff86e6f6e3a", "w_MO2.bin");
        Add("whole/SMS", "SMS", 11, "a0f425b23200568132ba76b2405e3933", "w_SMS.bin");
        Add("whole/MD", "MD", 1, "da9461b3b0f74becc3ccf6c2a094c516", "w_MD.bin");
        Add("whole/DUCK", "DUCK", 69, "8e6576cd5c21e44e0bbfc4480577b040", "w_DUCK.bin");
        Add("whole/NGP", "NGP", 14, "cf86acf519625a25a17b1246975e90ae", "w_NGP.bin");
        Add("whole/Oric", "32", 32, "953a2baa3232c63286aeae36b2172cef", "w_Oric.bin");
        Add("whole/MINI", "MINI", 24, "68f0f13b598e0b66461bc578375c3888", "w_MINI.bin");
        Add("whole/32X", "32X", 10, "07d733f252896ec41b4fd521fe610e2c", "w_32X.bin");
        Add("whole/SG1K", "SG1K", 33, "6a2305a2b6675a97ff792709be1ca857", "w_SG1K.bin");
        Add("whole/TI83", "79", 79, "bfb6048395a425c69743900785987c42", "w_TI83.bin");
        Add("whole/TIC-80", "65", 65, "79b96f4ffcedb3ce8210a83b22cd2c69", "w_TIC80.bin");
        Add("whole/UZE", "UZE", 80, "a9aab505e92edc034d3c732869159789", "w_UZE.bin");
        Add("whole/VECT", "VECT", 46, "572686c3a073162e4ec6eff86e6f6e3a", "w_VECT.bin");
        Add("whole/VB", "VB", 28, "68f0f13b598e0b66461bc578375c3888", "w_VB.bin");
        Add("whole/WSV", "WSV", 63, "6a2305a2b6675a97ff792709be1ca857", "w_WSV.bin");
        Add("whole/WASM4", "WASM4", 72, "bce38bb5f05622fc7e0e56757059d180", "w_WASM4.bin");
        Add("whole/WS", "WS", 53, "68f0f13b598e0b66461bc578375c3888", "w_WS.bin");

        /* ---- cartridge algorithms ---- */
        Add("cart/NES", "NES", 7, "6a2305a2b6675a97ff792709be1ca857", "nes.nes");
        Add("cart/NES-FDS", "NES", 7, "fd770d4d34c00760fabda6ad294a8f0b", "fds.fds");
        /* RC_CONSOLE_FDS is not in the 1.8.3 dispatch table — both binaries reject it */
        cases.Add(new ParityCase("cart/FDS", Array.Empty<string>(), "FDS", 81, new[] { "fds.fds" }, null, ExpectSuccess: false));
        Add("cart/7800", "7800", 51, "455f07d8500f3fabc54906737866167f", "a78.a78");
        Add("cart/NDS", "DS", 18, "56b30c276cba4affa886bd38e8e34d7e", "nds.nds");
        /* ESCV is a NULL-group console: the 1.8.3 CLI only accepts keys for group != NULL
         * consoles (others fall back to atoi), so the corpus uses the numeric id */
        Add("cart/SCV", "55", 55, "4309c9844b44f9ff8256dfc04687b8fd", "escv.cart");
        cases.Add(new ParityCase("cart/SNES", Array.Empty<string>(), "SNES", 3, new[] { "sn.sfc" }, null, ExpectSuccess: true));
        cases.Add(new ParityCase("cart/N64", Array.Empty<string>(), "N64", 2, new[] { "n64.z64" }, null, ExpectSuccess: true));
        cases.Add(new ParityCase("cart/Lynx", Array.Empty<string>(), "Lynx", 13, new[] { "lynx.lnx" }, null, ExpectSuccess: true));

        /* ---- disc consoles ---- */
        Add("disc/PSX", "PS1", 12, "db433fb038cde4fb15c144e8c7dea6e3", "psx.cue");
        Add("disc/PSX-homebrew", "PS1", 12, "e494c79a7315be0dc3e8571c45df162c", "psx_homebrew.cue");
        cases.Add(new ParityCase("disc/PSX-bin", Array.Empty<string>(), "PS1", 12, new[] { "psx.bin" }, null, ExpectSuccess: true));
        Add("disc/PS2", "PS2", 21, "01a517e4ad72c6c2654d1b839be7579d", "ps2.iso");
        Add("disc/PSP", "PSP", 41, "27ec2f9b7238b2ef29af31ddd254f201", "psp.iso");
        cases.Add(new ParityCase("disc/PSP-pbp", Array.Empty<string>(), "PSP", 41, new[] { "eboot.pbp" }, null, ExpectSuccess: true));
        Add("disc/SegaCD", "SCD", 9, "574498e1453cb8934df60c4ab906e783", "sega.cue");
        Add("disc/Saturn", "SAT", 39, "4cd9c8e41cd8d137be15bbe6a93ae1d8", "saturn.cue");
        Add("disc/3DO", "3DO", 43, "59622882e3261237e8a1e396825ae4f5", "3do.bin");
        Add("disc/JaguarCD", "JCD", 77, "c324d95dc5831c2d5c470eefb18c346b", "jaguar.cue");
        Add("disc/PCE-CD", "PCCD", 76, "6565819195a49323e080e7539b54f251", "pce.cue");
        Add("disc/PC-FX", "PC-FX", 49, "0a03af66559b8529c50c4e7788379598", "pcfx.cue");
        Add("disc/Dreamcast", "DC", 40, "2a550500caee9f06e5d061fe10a46f6e", "dc.gdi");
        Add("disc/NeoGeoCD", "NGCD", 56, "96f35b20c6cf902286da45e81a50b2a3", "ngcd.cue");
        Add("disc/GameCube", "GC", 16, "c7803b704fa43d22d8f6e55f4789cb45", "gc.iso");

        /* ---- CHD (vendored fixtures) ---- */
        Add("chd/PSX", "PS1", 12, "db433fb038cde4fb15c144e8c7dea6e3", "chd/psx.chd");
        Add("chd/PSP", "PSP", 41, "a7070bf07f5c1a0afb2b2d202d7e3893", "chd/psp.chd");
        cases.Add(new ParityCase("chd/pregap", Array.Empty<string>(), "PS1", 12, new[] { "chd/pregap.chd" }, null, ExpectSuccess: false)); /* fixture is a pregap test disc, not a PSX game */
        AddFail("chd/multi-pcfx", "PC-FX", 49, "chd/multi.chd"); /* LARGEST-track quirk: both report "Not a PC-FX CD" */

        /* ---- zip (DOS is a NULL-group console; numeric id only) ---- */
        Add("zip/arduboy", "ARD", 71, "e696445c353e9d6b3d60bf5d194b82cf", "arduboy.arduboy");
        Add("zip/dosz", "26", 26, "59a255662262f5ada32791b8c36e8ea7", "dosz.dosz");
        Add("zip/dosz-zip64", "26", 26, "927dad0a57a2860267ab7bcdb8bc3f61", "dosz64.dosz");
        Add("zip/dosz-dosc", "26", 26, "dd0c0b0c170c30722784e5e962764c35", "game.dosz");
        /* parent-chain: real-file resolution differs from the mock vectors (the mock's
         * directory semantics don't match the real path building); oracle==cli is the
         * assertion here, the mock semantics stay covered by the unit tests */
        cases.Add(new ParityCase("zip/parent-chain", Array.Empty<string>(), "26", 26, new[] { "child.dosz" }, null, ExpectSuccess: true));
        cases.Add(new ParityCase("zip/parent-chain+dosc", Array.Empty<string>(), "26", 26, new[] { "child.dosz" }, null, ExpectSuccess: true));
        cases.Add(new ParityCase("zip/parent-chain+child-dosc", Array.Empty<string>(), "26", 26, new[] { "child.dosz" }, null, ExpectSuccess: true));

        /* ---- 3DS (NULL-group console; numeric id only — the "3DS" key resolves via
         * atoi to console 3 (SNES) in the original CLI) ---- */
        string[] threeDs = { "-s", "{SYSTEM}" };
        cases.Add(new ParityCase("3ds/encrypted", threeDs, "62", 62, new[] { "3ds/encrypted.ncch" }, "eb334fea757807e4a4b81ee99905437c", ExpectSuccess: true));
        cases.Add(new ParityCase("3ds/encrypted-v1", threeDs, "62", 62, new[] { "3ds/encrypted_v1.ncch" }, "552ef040edf82bffada8b7615b8b2faa", ExpectSuccess: true));
        cases.Add(new ParityCase("3ds/seed", threeDs, "62", 62, new[] { "3ds/seed.ncch" }, "29b0b5a9e83ac39e635c792a5142f5e4", ExpectSuccess: true));
        cases.Add(new ParityCase("3ds/unaligned", threeDs, "62", 62, new[] { "3ds/unaligned.ncch" }, "3e2d3dfe1808dd0498ecf6c77e36ea46", ExpectSuccess: true));
        cases.Add(new ParityCase("3ds/encrypted-cia", threeDs, "62", 62, new[] { "3ds/encrypted.cia" }, "eb334fea757807e4a4b81ee99905437c", ExpectSuccess: true));
        cases.Add(new ParityCase("3ds/plain-cia", threeDs, "62", 62, new[] { "3ds/plain.cia" }, "eb334fea757807e4a4b81ee99905437c", ExpectSuccess: true));
        cases.Add(new ParityCase("3ds/homebrew", threeDs, "62", 62, new[] { "3ds/homebrew.3dsx" }, "ca7161a502db8be8089d16a8b2280970", ExpectSuccess: true));
        cases.Add(new ParityCase("3ds/junk", threeDs, "62", 62, new[] { "3ds/junk.bin" }, null, ExpectSuccess: false));

        /* ---- .neo (Geolith Neo Geo cart; Part II) ---- */
        Add("cart/neogeo-neo", "ARC", 27, s_neoPayloadHash, "game.neo");
        Add("cart/neogeo-neo-variant", "ARC", 27, s_neoPayloadHash, "game_alt.neo");
        cases.Add(new ParityCase("cart/neogeo-neo-badmagic", Array.Empty<string>(), "ARC", 27, new[] { "bad.neo" }, null, ExpectSuccess: false));
        cases.Add(new ParityCase("args/iterate-neo", Array.Empty<string>(), "?", 91, new[] { "game.neo" }, null, ExpectSuccess: true));
        cases.Add(new ParityCase("args/iterate-sms", Array.Empty<string>(), "?", 91, new[] { "game.sms" }, null, ExpectSuccess: true));

        /* ---- malformed GDI (12.4.0 bounds checks) ---- */
        cases.Add(new ParityCase("disc/gdi-unterminated-quote", Array.Empty<string>(), "DC", 40, new[] { "gdi_badquote.gdi" }, null, ExpectSuccess: false));
        cases.Add(new ParityCase("disc/gdi-long-filename", Array.Empty<string>(), "DC", 40, new[] { "gdi_longname.gdi" }, null, ExpectSuccess: false));

        /* ---- m3u ---- */
        Add("m3u/MD", "MD", 1, "da9461b3b0f74becc3ccf6c2a094c516", "play.m3u");

        /* ---- CLI arg modes ---- */
        cases.Add(new ParityCase("args/iterate-nes", Array.Empty<string>(), "?", 91, new[] { "nes.nes" }, null, ExpectSuccess: true));
        cases.Add(new ParityCase("args/iterate-psx", Array.Empty<string>(), "?", 91, new[] { "psx.cue" }, null, ExpectSuccess: true));
        cases.Add(new ParityCase("args/wildcard", Array.Empty<string>(), "GB", 4, new[] { "wild\\*.gb" }, null, ExpectSuccess: true));
        /* directory-less wildcard scans the current directory ("*.gb" in the wild/ dir) */
        cases.Add(new ParityCase("args/wildcard-cwd", Array.Empty<string>(), "GB", 4, new[] { "*.gb" }, null, ExpectSuccess: true,
            WorkingDir: Path.Combine(s_corpus.Value.CorpusDir, "wild")));
        cases.Add(new ParityCase("args/multi-file", Array.Empty<string>(), "GB", 4, new[] { "w_GB.bin", "w_GB.bin" }, null, ExpectSuccess: true));
        cases.Add(new ParityCase("args/verbose", new[] { "-v" }, "PS1", 12, new[] { "psx.cue" }, null, ExpectSuccess: true));
        cases.Add(new ParityCase("args/usage", Array.Empty<string>(), "GB", 4, Array.Empty<string>(), null, ExpectSuccess: false, NormalizeNames: true));
        /* "0" resolves to console id 0 on every build (keys and numeric alike) -> usage + exit 1 */
        cases.Add(new ParityCase("args/unknown-key", Array.Empty<string>(), "0", 0, new[] { "w_GB.bin" }, null, ExpectSuccess: false, NormalizeNames: true));
        cases.Add(new ParityCase("args/unknown-flag", new[] { "-x" }, "GB", 4, new[] { "w_GB.bin" }, null, ExpectSuccess: false, NormalizeNames: true));
        cases.Add(new ParityCase("args/missing-file", Array.Empty<string>(), "GB", 4, new[] { "nope.bin" }, null, ExpectSuccess: false));

        return cases;
    }

    /* ========================================================================= */

    private static CorpusPaths BuildCorpus()
    {
        string corpusDir = Path.Combine(Path.GetTempPath(), "rasharp_parity_corpus_" + Guid.NewGuid().ToString("N")[..8]);
        if (Directory.Exists(corpusDir))
            Directory.Delete(corpusDir, recursive: true);
        Directory.CreateDirectory(corpusDir);

        string systemDir = Path.Combine(corpusDir, "system");
        Directory.CreateDirectory(systemDir);
        Directory.CreateDirectory(Path.Combine(corpusDir, "wild"));
        Directory.CreateDirectory(Path.Combine(corpusDir, "chd"));
        Directory.CreateDirectory(Path.Combine(corpusDir, "3ds"));

        void Write(string name, byte[] bytes) => File.WriteAllBytes(Path.Combine(corpusDir, name), bytes);
        void WriteText(string name, string text) => File.WriteAllText(Path.Combine(corpusDir, name), text, Encoding.ASCII);

        string CueMode2Raw2352(string bin) =>
            $"FILE \"{bin}\" BINARY\n  TRACK 01 MODE2/2352\n    INDEX 01 00:00:00\n";
        string CueMode1Raw2048(string bin) =>
            $"FILE \"{bin}\" BINARY\n  TRACK 01 MODE1/2048\n    INDEX 01 00:00:00\n";

        /* 2048-byte-sector ISO image -> raw 2352 sectors with an 8-byte XA subheader
         * (data at offset 24). Sector headers carry real MSF (LBA 0 = 00:02:00) —
         * the cdreader computes track_first_sector from the sector-16 header as
         * msf_to_lba - 16, which must come out as 0 for the ISO walker's absolute
         * LBA reads to pass its sector guard. */
        byte[] ToXaRaw(byte[] iso)
        {
            int numSectors = (iso.Length + 2047) / 2048;
            byte[] raw = new byte[numSectors * 2352];
            uint firstSector = 150; /* LBA 0 = MSF 00:02:00 */
            byte minutes, seconds, frames;
            frames = (byte)(firstSector % 75);
            firstSector /= 75;
            seconds = (byte)(firstSector % 60);
            minutes = (byte)(firstSector / 60);
            for (int s = 0; s < numSectors; s++)
            {
                int dst = s * 2352;
                raw[dst] = 0x00;
                for (int i = 1; i < 11; i++)
                    raw[dst + i] = 0xFF;
                raw[dst + 11] = 0x00;
                raw[dst + 12] = (byte)(((minutes / 10) << 4) | (minutes % 10));
                raw[dst + 13] = (byte)(((seconds / 10) << 4) | (seconds % 10));
                raw[dst + 14] = (byte)(((frames / 10) << 4) | (frames % 10));
                if (++frames == 75) { frames = 0; if (++seconds == 60) { seconds = 0; ++minutes; } }
                raw[dst + 15] = 2; /* mode 2 */
                /* 8-byte XA subheader (zeroed) at 16..23; data at 24 */
                int src = s * 2048;
                int chunk = Math.Min(2048, iso.Length - src);
                Array.Copy(iso, src, raw, dst + 24, chunk);
            }

            return raw;
        }

        /* (a stream-chunking 2048->2352 helper: AUDIO tracks are read headerless, so
         * the reader sees exactly the mock stream the unit vectors were computed on) */
        byte[] ChunkTo2352(byte[] iso)
        {
            int rawSize = ((iso.Length + 2351) / 2352) * 2352;
            byte[] raw = new byte[rawSize];
            Array.Copy(iso, raw, iso.Length);
            return raw;
        }

        /* ---- generic whole-file files (sizes from the test_hash_rom.c vector table) ---- */
        (string Name, int Size)[] wholeFiles =
        {
            ("w_A2001.bin", 4096), ("w_2600.bin", 2048), ("w_JAG.bin", 0x400000), ("w_CV.bin", 16384),
            ("w_ELEK.bin", 4096), ("w_CHF.bin", 2048), ("w_GB.bin", 131072), ("w_GBC.bin", 2097152),
            ("w_GBA.bin", 4194304), ("w_GG.bin", 524288), ("w_INTV.bin", 8192), ("w_VC4000.bin", 2048),
            ("w_MO2.bin", 4096), ("w_SMS.bin", 131072), ("w_MD.bin", 1048576), ("w_DUCK.bin", 65536),
            ("w_NGP.bin", 2097152), ("w_Oric.bin", 18119), ("w_MINI.bin", 524288), ("w_32X.bin", 3145728),
            ("w_SG1K.bin", 32768), ("w_TI83.bin", 1695), ("w_TIC80.bin", 67682), ("w_UZE.bin", 53654),
            ("w_VECT.bin", 4096), ("w_VB.bin", 524288), ("w_WSV.bin", 32768), ("w_WASM4.bin", 33454),
            ("w_WS.bin", 524288),
        };
        foreach ((string name, int size) in wholeFiles)
            Write(name, TestDataGen.GenerateGenericFile(size));

        /* ---- cartridges ---- */
        Write("nes.nes", TestDataGen.GenerateNesFile(32, withHeader: true, out _));
        Write("fds.fds", TestDataGen.GenerateFdsFile(2, withHeader: false, out _));
        Write("nds.nds", TestDataGen.GenerateNdsFile(2, 1234567, 654321, out _));
        Write("a78.a78", TestDataGen.GenerateAtari7800File(16, withHeader: true, out _));

        byte[] escv = TestDataGen.GenerateGenericFile(32768 + 32);
        Array.Copy(Encoding.ASCII.GetBytes("EmuSCV....CART.............................."), escv, 32);
        Write("escv.cart", escv);

        byte[] generic256k = TestDataGen.GenerateGenericFile(256 * 1024);
        Write("sn.sfc", generic256k);
        Write("n64.z64", generic256k);
        Write("lynx.lnx", generic256k);

        /* ---- PSX (raw 2352 XA sectors + real cue, same format genchdimg.c validated) ---- */
        byte[] psx2048 = TestDataGenDisc.GeneratePsxBin("SLUS_007.45", 0x07D800, out _);
        Write("psx.bin", ToXaRaw(psx2048));
        WriteText("psx.cue", CueMode2Raw2352("psx.bin"));

        /* PSX homebrew (no SYSTEM.CNF; PS-X EXE in root) */
        const uint homebrewSize = 0x12000;
        uint sectorsNeeded = ((homebrewSize + 2047) / 2048) + 20;
        byte[] homebrew2048 = TestDataGenDisc.GenerateIso9660Bin(sectorsNeeded, "HOMEBREW", out _);
        int exe = TestDataGenDisc.GenerateIso9660File(homebrew2048, "PSX.EXE", null, (int)homebrewSize);
        Encoding.ASCII.GetBytes("PS-X EXE").CopyTo(homebrew2048, exe);
        uint adjustedSize = homebrewSize - 2048;
        homebrew2048[exe + 28] = (byte)(adjustedSize & 0xFF);
        homebrew2048[exe + 29] = (byte)((adjustedSize >> 8) & 0xFF);
        homebrew2048[exe + 30] = (byte)((adjustedSize >> 16) & 0xFF);
        homebrew2048[exe + 31] = (byte)((adjustedSize >> 24) & 0xFF);
        Write("psx_homebrew.bin", ToXaRaw(homebrew2048));
        WriteText("psx_homebrew.cue", CueMode2Raw2352("psx_homebrew.bin"));

        /* ---- PS2 / PSP ---- */
        Write("ps2.iso", TestDataGenDisc.GeneratePs2Bin("SLUS_200.64", 0x07D800, out _));

        byte[] psp = TestDataGenDisc.GenerateIso9660Bin(160, "TEST", out _);
        byte[] paramSfo = TestDataGen.GenerateGenericFile(690);
        byte[] ebootBin = TestDataGen.GenerateGenericFile(273470);
        TestDataGenDisc.GenerateIso9660File(psp, "PSP_GAME\\PARAM.SFO", paramSfo, paramSfo.Length);
        TestDataGenDisc.GenerateIso9660File(psp, "PSP_GAME\\SYSDIR\\EBOOT.BIN", ebootBin, ebootBin.Length);
        Write("psp.iso", psp);
        Write("eboot.pbp", TestDataGen.GenerateGenericFile(65536));

        /* ---- Sega CD / Saturn (512-byte sector-0 magic; MODE1/2048 sector 0 data is
         * the exact GenerateGenericFile(512) vector the unit tests hashed) ---- */
        byte[] sega512 = TestDataGen.GenerateGenericFile(512);
        Encoding.ASCII.GetBytes("SEGADISCSYSTEM  ").CopyTo(sega512, 0);
        byte[] segaImg = new byte[2048 * 8];
        Array.Copy(sega512, segaImg, 512);
        Write("sega.bin", segaImg);
        WriteText("sega.cue", CueMode1Raw2048("sega.bin"));

        byte[] saturn512 = TestDataGen.GenerateGenericFile(512);
        Encoding.ASCII.GetBytes("SEGA SEGASATURN ").CopyTo(saturn512, 0);
        byte[] saturnImg = new byte[2048 * 8];
        Array.Copy(saturn512, saturnImg, 512);
        Write("saturn.bin", saturnImg);
        WriteText("saturn.cue", CueMode1Raw2048("saturn.bin"));

        /* ---- 3DO (case-insensitive launchme) ---- */
        byte[] threeDo = TestDataGenDisc.Generate3DoBin(1, 6543, out _);
        Encoding.ASCII.GetBytes("launchme").CopyTo(threeDo, 2048 + 0x14 + 0x48 + 0x20);
        Write("3do.bin", threeDo);

        /* ---- Jaguar CD (data in track 2 of session 2). AUDIO tracks are read with
         * no header (raw 2352-byte stream), so the bin is the 2048-layout generator
         * output chunked to 2352 bytes — the reader sees exactly the mock stream the
         * unit vectors were computed on. track01/03 must be empty so track 2's
         * file_first_sector math stays at 0. */
        WriteText("jaguar.cue",
            "REM SESSION 01\n" +
            "FILE \"jag01.bin\" BINARY\n" +
            "  TRACK 01 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "REM SESSION 02\n" +
            "FILE \"jag02.bin\" BINARY\n" +
            "  TRACK 02 AUDIO\n" +
            "    INDEX 01 00:00:00\n" +
            "FILE \"jag03.bin\" BINARY\n" +
            "  TRACK 03 AUDIO\n" +
            "    INDEX 01 00:00:00\n");
        Write("jag01.bin", Array.Empty<byte>());
        Write("jag02.bin", ChunkTo2352(TestDataGenDisc.GenerateJaguarcdBin(2, 60024, 0, out _)));
        Write("jag03.bin", Array.Empty<byte>());

        /* ---- PCE-CD / PC-FX (2048-byte-sector ISO content; MODE1/2048 cue) ---- */
        Write("pce.bin", TestDataGenDisc.GeneratePceCdBin(72, out _));
        WriteText("pce.cue", CueMode1Raw2048("pce.bin"));
        Write("pcfx.bin", TestDataGenDisc.GeneratePcfxBin(72, out _));
        WriteText("pcfx.cue", CueMode1Raw2048("pcfx.bin"));

        /* ---- Dreamcast (real minimal .gdi; track 3 = type 4 MODE1/2352, data at +16,
         * so ConvertTo2352 (data at +16) reproduces the 2048-layout mock stream) ---- */
        WriteText("dc.gdi",
            "3\n" +
            "1 0 0 2352 track01.bin 0\n" +
            "2 600 0 2352 track02.bin 0\n" +
            "3 45000 4 2352 track03.bin 0\n");
        Write("track01.bin", new byte[2352]);
        Write("track02.bin", new byte[2352]);
        int dcSize = 0;
        Write("track03.bin", TestDataGenDisc.ConvertTo2352(TestDataGenDisc.GenerateDreamcastBin(45000, 1458208, out dcSize), ref dcSize, 45000));

        /* ---- Neo Geo CD ---- */
        byte[] ngcd = TestDataGenDisc.GenerateIso9660Bin(160, "TEST", out _);
        byte[] ipl = Encoding.ASCII.GetBytes("FIXA.FIX,0,0\r\nPROG.PRG,0,0\r\nSOUND.PCM,0,0\r\n\x1a");
        byte[] progPrg = TestDataGen.GenerateGenericFile(273470);
        TestDataGenDisc.GenerateIso9660File(ngcd, "IPL.TXT", ipl, ipl.Length);
        TestDataGenDisc.GenerateIso9660File(ngcd, "PROG.PRG", progPrg, progPrg.Length);
        Write("ngcd.bin", ngcd);
        WriteText("ngcd.cue", CueMode1Raw2048("ngcd.bin"));

        /* ---- GameCube ---- */
        Write("gc.iso", TestDataGenDisc.GenerateGamecubeIso(32, out _));

        /* ---- CHD fixtures (vendored in TestData) ---- */
        foreach (string chd in new[] { "psx.chd", "psp.chd", "multi.chd", "pregap.chd" })
        {
            string src = Path.Combine(AppContext.BaseDirectory, "TestData", chd);
            File.Copy(src, Path.Combine(corpusDir, "chd", chd));
        }

        /* ---- zip fixtures (MockZipFile reproduces the C's mock byte-for-byte) ---- */
        void WriteZip(string name, Action<TestHashZip.MockZipFile> build, string comment)
        {
            var zip = new TestHashZip.MockZipFile(1024);
            build(zip);
            int size = zip.Finalize(comment);
            byte[] bytes = new byte[size];
            Array.Copy(zip.Buffer, 0, bytes, 0, size);
            Write(name, bytes);
        }

        WriteZip("arduboy.arduboy", zip =>
        {
            zip.AddFile("info.json", 0xA40B2541, 35);
            zip.AddFile("game.bin", 0x5AA654C0, 96);
            zip.AddFile("save.bin", 0xFF000000, 1);
            zip.AddFile("interp_s2_ArduboyFX.hex", 0x50648360, 71);
            zip.AddFile("screenshot.png", 0x30056694, 48);
        }, "");

        WriteZip("dosz.dosz", zip =>
        {
            zip.AddFile("FOLDER/", 0, 0);
            zip.AddFile("FOLDER/SUB.TXT", 0x4AD0CF31, 1);
            zip.AddFile("ROOT.TXT", 0xD3D99E8B, 1);
        }, "TORRENTZIPPED-FD07C52C");

        WriteZip("dosz64.dosz", zip =>
        {
            zip.IsZip64 = true;
            zip.AddFile("README", 0x69FFE77E, 36);
        }, "");

        WriteZip("game.dosz", zip =>
        {
            zip.AddFile("FOLDER/", 0, 0);
            zip.AddFile("FOLDER/SUB.TXT", 0x4AD0CF31, 1);
            zip.AddFile("ROOT.TXT", 0xD3D99E8B, 1);
        }, "TORRENTZIPPED-FD07C52C");
        WriteZip("game.dosc", zip =>
        {
            zip.AddFile("FOLDER/", 0, 0);
            zip.AddFile("FOLDER/SUB.TXT", 0x4AD0CF31, 1);
            zip.AddFile("ROOT.TXT", 0xD3D99E8B, 1);
        }, "TORRENTZIPPED-FD07C52C");

        /* parent chain: base.dosz (+ base.dosc / child.dosc variants) + child.dosz */
        WriteZip("base.dosz", zip =>
        {
            zip.AddFile("FOLDER/", 0, 0);
            zip.AddFile("FOLDER/SUB.TXT", 0x4AD0CF31, 1);
            zip.AddFile("ROOT.TXT", 0xD3D99E8B, 1);
        }, "TORRENTZIPPED-FD07C52C");
        WriteZip("base.dosc", zip =>
        {
            zip.AddFile("FOLDER/", 0, 0);
            zip.AddFile("FOLDER/SUB.TXT", 0x4AD0CF31, 1);
            zip.AddFile("ROOT.TXT", 0xD3D99E8B, 1);
        }, "TORRENTZIPPED-FD07C52C");
        WriteZip("child.dosz", zip =>
        {
            zip.AddFile("base.dosz.parent", 0, 0);
            zip.AddFile("CHILD.TXT", 0x22B35429, 5);
        }, "");
        WriteZip("child.dosc", zip =>
        {
            zip.AddFile("base.dosz.parent", 0, 0);
            zip.AddFile("CHILD.TXT", 0x22B35429, 5);
        }, "");

        /* ---- 3DS system dir + fixtures ---- */
        File.WriteAllText(Path.Combine(systemDir, "aes_keys.txt"), TestDataGen3ds.AesKeysTxt());
        byte[] programId = { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };
        byte[] seed = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 };
        File.WriteAllBytes(Path.Combine(systemDir, "seeddb.bin"), TestDataGen3ds.GenerateSeedDbBin(programId, seed));

        string threeDsDir = Path.Combine(corpusDir, "3ds");
        File.WriteAllBytes(Path.Combine(threeDsDir, "plain.ncch"), TestDataGen3ds.GenerateNcch(false, false, false, 0x01, 0, null, null, out _, out _));
        File.WriteAllBytes(Path.Combine(threeDsDir, "encrypted.ncch"), TestDataGen3ds.GenerateNcch(true, false, false, 0x01, 0, null, null, out _, out _));
        File.WriteAllBytes(Path.Combine(threeDsDir, "encrypted_v1.ncch"), TestDataGen3ds.GenerateNcch(true, false, false, 0x01, 1, null, null, out _, out _));
        File.WriteAllBytes(Path.Combine(threeDsDir, "unaligned.ncch"), TestDataGen3ds.GenerateNcch(true, false, false, 0x01, 0, null, null, out _, out _, 0x641));
        File.WriteAllBytes(Path.Combine(threeDsDir, "seed.ncch"), TestDataGen3ds.GenerateNcch(true, false, true, 0x0B, 0, programId, seed, out _, out _));

        byte[] titleKey = { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0x00 };
        byte[] titleId = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
        byte[] encNcch = TestDataGen3ds.GenerateNcch(true, false, false, 0x01, 0, null, null, out _, out _);
        File.WriteAllBytes(Path.Combine(threeDsDir, "encrypted.cia"), TestDataGen3ds.GenerateCia(encNcch, titleId, 0, titleKey, true));
        File.WriteAllBytes(Path.Combine(threeDsDir, "plain.cia"), TestDataGen3ds.GenerateCia(TestDataGen3ds.GenerateNcch(false, false, false, 0x01, 0, null, null, out _, out _), titleId, 0, titleKey, false));
        File.WriteAllBytes(Path.Combine(threeDsDir, "homebrew.3dsx"), TestDataGen3ds.Generate3Dsx());
        File.WriteAllBytes(Path.Combine(threeDsDir, "junk.bin"), new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });

        /* ---- .neo fixtures (rcheevos 12.4.0 test data) ---- */
        byte[] neoPayload = TestDataGen.GenerateGenericFile(131072);
        s_neoPayloadHash = Convert.ToHexStringLower(MD5.HashData(neoPayload));
        Write("game.neo", TestHashNeo.GenerateNeoFile(131072, "Test Game", "TestCorp"));
        Write("game_alt.neo", TestHashNeo.GenerateNeoFile(131072, "test game (alt name)", "OtherTool"));
        byte[] neoBad = TestHashNeo.GenerateNeoFile(131072, "Test Game", "TestCorp");
        neoBad[3] = 2; /* unsupported version */
        Write("bad.neo", neoBad);
        Write("game.sms", neoPayload); /* .sms iterate maps to Master System */

        /* ---- malformed GDI (12.4.0: unterminated quote / >=256-byte filename) ---- */
        WriteText("gdi_badquote.gdi",
            "3\n" +
            "1 0 0 2352 \"unterminated.bin\n" +
            "2 600 0 2352 track02.bin 0\n" +
            "3 45000 4 2352 track03.bin 0\n");
        WriteText("gdi_longname.gdi",
            "3\n" +
            $"1 0 0 2352 \"{new string('x', 300)}.bin\" 0\n" +
            "2 600 0 2352 track02.bin 0\n" +
            "3 45000 4 2352 track03.bin 0\n");

        /* ---- m3u ---- */
        WriteText("play.m3u", "test.md");
        Write("test.md", TestDataGen.GenerateGenericFile(1048576));

        /* ---- wildcard dir ---- */
        Write(Path.Combine("wild", "a.gb"), TestDataGen.GenerateGenericFile(131072));
        Write(Path.Combine("wild", "b.gb"), TestDataGen.GenerateGenericFile(131072));

        return new CorpusPaths(corpusDir, systemDir);
    }
}
