// Adapter over VideoGameFileSystemParser (NuGet 1.2.0) — the alternative
// filesystem backend to the engine's ISO9660 mini-parser
// (HashDisc.CdFindFileSector). Resolves a disc path to (sector, size).
//
// v1.2.0 constraint: the library's parsers are CHD-bound — `ParserFactory`
// and `ChdContainer` both take a `SectorReader`/`ChdFile` rooted in CHDSharp,
// and there is no raw-file (ISO/bin) entry point. So this backend is wired in
// where CHD images are hashed (Phase 5: ChdCdReader + rc_hash_init_chd_cdreader);
// the agreement test against the mini-parser runs on CHD vectors once those
// exist. Path semantics mirror the mini-parser: case-insensitive, backslash
// separated, optional leading backslash, root-relative.

using VideoGameFileSystemParser.Models;
using VideoGameFileSystemParser.Parsers;

namespace RASharp.Core;

public sealed class FileSystemResolver : IDisposable
{
    private readonly ChdContainer _container;

    public FileSystemResolver(string chdPath, ConsoleType consoleType)
    {
        _container = new ChdContainer(chdPath);
        if (!_container.MountAndParse(consoleType))
        {
            _container.Dispose();
            throw new InvalidOperationException($"Could not parse {chdPath} as {consoleType}");
        }
    }

    public ConsoleType ConsoleType => _container.ConsoleType;

    public IReadOnlyList<FileEntry> Entries => _container.Entries;

    /* resolve a path to the first extent of the file, like
     * rc_cd_find_file_sector's (sector, size) out params */
    public bool TryResolve(string path, out uint lba, out ulong size)
    {
        lba = 0;
        size = 0;

        FileEntry? entry = Find(path);
        if (entry == null)
            return false;

        lba = entry.Lba;
        size = entry.Size;
        return true;
    }

    public FileEntry? Find(string path)
    {
        string normalized = path.Replace('/', '\\');
        if (normalized.Length == 0 || normalized[0] != '\\')
            normalized = "\\" + normalized;

        return _container.FindFile(normalized);
    }

    public void Dispose()
    {
        _container.Dispose();
    }
}
