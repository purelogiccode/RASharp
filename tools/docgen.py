#!/usr/bin/env python3
"""Insert XML doc comments on every undocumented public/internal type and
method in the solution. Idempotent: members already carrying a '///' comment
directly above are skipped. Summaries are lifted from existing C-style
comments when present (file headers for classes), otherwise generated from a
domain dictionary or the member name."""
import os
import re

ROOTS = ['RetroAchievementsSharp', 'RetroAchievementsSharp.Cli', 'RetroAchievementsSharp.Tests']

# files where private methods are part of the documented surface (CLI entry/flow)
DOC_PRIVATE = {'RetroAchievementsSharp.Cli/Program.cs'}

CONSOLE = {
    '7800': 'Atari 7800', 'Arcade': 'arcade (romset zip or .neo)', 'NeogeoCart': 'Neo Geo cart (.neo)',
    'Arduboy': 'Arduboy', 'ArduboyFx': 'Arduboy FX (.arduboy zip)', 'Lynx': 'Atari Lynx',
    'Nes': 'NES/Famicom', 'N64': 'Nintendo 64', 'NintendoDs': 'Nintendo DS', 'Dsi': 'Nintendo DSi',
    'Pce': 'PC Engine', 'Scv': 'Super Cassette Vision', 'Psx': 'PlayStation', 'Ps2': 'PlayStation 2',
    'Psp': 'PSP', 'SegaCd': 'Sega CD', 'Saturn': 'Sega Saturn', 'Dreamcast': 'Dreamcast',
    'JaguarCd': 'Atari Jaguar CD', 'PceCd': 'PC Engine CD', 'Pcfx': 'PC-FX', 'NeogeoCd': 'Neo Geo CD',
    'Gamecube': 'GameCube', 'Wii': 'Wii', 'Wiiware': 'WiiWare', '3Do': '3DO',
    'Nintendo3Ds': 'Nintendo 3DS', 'MsDos': 'MS-DOS (DOSZ/DOSC)', 'Cia': '3DS CIA',
    'Ncch': '3DS NCCH', 'Ncsd': '3DS NCSD', '3Dsx': '3DSX',
}

DOMAIN = {
    # RcHash facade
    'InitErrorMessageCallback': 'Registers the global error message callback.',
    'InitVerboseMessageCallback': 'Registers the global verbose message callback.',
    'InitCustomFilereader': 'Registers a custom global file reader.',
    'GetDefaultCdreader': 'Fills a cdreader struct with the default CD reader handlers.',
    'InitDefaultCdreader': 'Registers the default CD reader as the global cdreader.',
    'InitCustomCdreader': 'Registers a custom global cdreader.',
    'Init3DsGetCiaNormalKeyFunc': 'Registers the 3DS CIA normal-key provider.',
    'Init3DsGetNcchNormalKeysFunc': 'Registers the 3DS NCCH normal-keys provider.',
    'GenerateFromBuffer': 'Generates the hash for a console from an in-memory buffer.',
    'GenerateFromFile': 'Generates the hash for a console from a file on disk.',
    # HashIterator
    'InitializeIterator': 'Initializes an iterator for a path or buffer.',
    'Iterate': 'Walks the handler table and returns the first console that accepts the file.',
    'DestroyIterator': 'Releases the iterator resources.',
    'GetIteratorExtHandlers': 'Returns the extension-to-console handler table.',
    # HashEngine primitives
    'WholeFile': 'Hashes the whole file (MD5), capped at MAX_BUFFER_SIZE.',
    'BufferedFile': 'Reads the file into memory (capped at MAX_BUFFER_SIZE) and dispatches to the buffer path.',
    'FileFromBuffer': 'Dispatches a buffered file to the buffer hash for the console.',
    'FromBuffer': 'Dispatches a buffer hash for the console.',
    'FromFile': 'Dispatches a file hash for the console.',
    'GenerateFromPlaylist': 'Hashes the first entry of an m3u playlist with the console.',
    'MergeCallbacks': 'Merges a partial callback set into the iterator.',
    'Finalize': 'Finalizes the MD5 and produces the 32-char hash.',
    'IteratorError': 'Reports a non-formatted error through the error callback.',
    'IteratorErrorFormatted': 'Reports a formatted error through the error callback.',
    'IteratorVerbose': 'Reports a non-formatted verbose message through the verbose callback.',
    'IteratorVerboseFormatted': 'Reports a formatted verbose message through the verbose callback.',
    'FileOpen': 'Opens a file through the iterator filereader.',
    'FileSeek': 'Seeks a file through the iterator filereader.',
    'FileTell': 'Returns the current position of a file through the iterator filereader.',
    'FileRead': 'Reads bytes from a file through the iterator filereader.',
    'FileClose': 'Closes a file through the iterator filereader.',
    'PathCompareExtension': 'Compares a path extension with a candidate (case-insensitive).',
    'PathGetExtension': 'Returns the lowercase extension of a path, including the dot.',
    'PathGetFilename': 'Returns the filename portion of a path.',
    'HashBuffer': 'Hashes a buffer region with the given MD5 state.',
    'ResetIterator': 'Clears an iterator back to its initial state.',
    'GetGlobalCdreader': 'Returns the global cdreader, if registered.',
    'HashInitErrorMessageCallback': 'Stores the global error message callback.',
    'HashInitVerboseMessageCallback': 'Stores the global verbose message callback.',
    'HashInit3DsGetCiaNormalKeyFunc': 'Stores the global 3DS CIA normal-key provider.',
    'HashInit3DsGetNcchNormalKeysFunc': 'Stores the global 3DS NCCH normal-keys provider.',
    # HashRom
    'RcHash7800': 'Hashes an Atari 7800 cartridge (128-byte header stripped when present).',
    'RcHashArcade': 'Hashes an arcade romset by filename (FBNeo semantics).',
    'RcHashNeogeoCart': 'Hashes a Geolith .neo cart by ROM content (4096-byte header skipped).',
    'RcHashArduboy': 'Hashes an Arduboy image (zip or Intel HEX text).',
    'RcHashLynx': 'Hashes an Atari Lynx cartridge (64-byte header stripped when present).',
    'RcHashNes': 'Hashes a NES/FDS image (iNES/FDS header stripped when present).',
    'RcHashN64': 'Hashes a Nintendo 64 cartridge (byte-swap + 1 MiB cap).',
    'RcHashNintendoDs': 'Hashes a Nintendo DS/DSi image (SuperCard variant supported).',
    'RcHashPce': 'Hashes a PC Engine HuCard.',
    'RcHashScv': 'Hashes a Super Cassette Vision cartridge (32-byte header stripped).',
    # HashZip
    'RcHashArduboyFx': 'Hashes an Arduboy FX .arduboy zip (hex/bin entries).',
    'RcHashMsDos': 'Hashes an MS-DOS zip (DOSZ/DOSC, parent chains).',
    # HashEncrypted
    'RcHashNintendo3Ds': 'Hashes a Nintendo 3DS file (decrypt-then-hash).',
    # HashDisc
    'RcHash3Do': 'Hashes a 3DO disc (OperaFS LAUNCHME).',
    'RcHashAtariJaguarCd': 'Hashes an Atari Jaguar CD disc (second-session boot header).',
    'RcHashDreamcast': 'Hashes a Dreamcast disc (IP.BIN / track rules).',
    'RcHashGamecube': 'Hashes a GameCube disc (partition reading).',
    'RcHashNeogeoCd': 'Hashes a Neo Geo CD disc (IPL.TXT executables).',
    'RcHashPceCd': 'Hashes a PC Engine CD disc.',
    'RcHashPcfx': 'Hashes a PC-FX disc (largest data track).',
    'RcHashPsx': 'Hashes a PlayStation disc (SYSTEM.CNF boot executable).',
    'RcHashPs2': 'Hashes a PlayStation 2 disc (BOOT2 ELF via ISO9660).',
    'RcHashPsp': 'Hashes a PSP disc (PARAM.SFO + EBOOT.BIN).',
    'RcHashSegaCd': 'Hashes a Sega CD / Saturn disc (first 512 bytes of sector 0).',
    'RcHashWii': 'Hashes a Wii disc (partition path).',
    'RcHashWiiware': 'Hashes a WiiWare title (TMD/content).',
    'CdFindFileSector': 'Locates a file inside an ISO9660 image and returns its sector and size.',
    'FindPlaystationExecutable': 'Locates the PSX/PS2 boot executable via SYSTEM.CNF.',
    'RcHashCdFile': 'Streams a file region of a disc into the MD5.',
    # CdReader
    'OpenTrack': 'Opens a track of a cue/gdi/bin disc image.',
    'OpenCueTrack': 'Parses a cue sheet and opens the requested track.',
    'OpenGdiTrack': 'Parses a gdi track table and opens the requested track.',
    'OpenBinTrack': 'Opens a single-track raw bin/iso image.',
    'GetBinSize': 'Returns the size of the bin file referenced by a cue/gdi.',
    'Matches': 'Compares a buffer region with a pattern byte-for-byte.',
    'OpenTrackIterator': 'Opens a track for an iterator (backwards-compatible track 0 = largest).',
    # FileUtil
    'FullPath': 'Returns the full path of a file, or the input when it cannot be resolved.',
    'Extension': 'Returns the extension of a path, including the dot.',
    'Directory': 'Returns the directory portion of a path (backslash-split, like the C utility).',
    'FileNameWithExtension': 'Returns the filename portion of a path with its extension.',
    'OpenFile': 'Opens a file for binary read with shared read/delete access.',
    'LoadZippedFile': 'Loads the first entry of a zip file into memory.',
    # Hash3DS
    'InitHash3DS': 'Loads the 3DS key material (aes_keys.txt + seeddb.bin) from a system directory.',
    # HashMd5
    'Append': 'Appends bytes to the MD5 state.',
    'Final': 'Returns the final MD5 digest.',
    # AesHelper
    'EncryptCbc': 'Encrypts data with AES-128-CBC (no padding), C call-pattern parity.',
    'DecryptCbc': 'Decrypts data with AES-128-CBC (no padding), C call-pattern parity.',
    'EncryptCtr': 'Encrypts/decrypts data with AES-128-CTR (C call-pattern parity).',
    # FileSystemResolver
    'Mount': 'Mounts a CHD filesystem for the given console type.',
    'FindFile': 'Finds a file path inside the mounted filesystem.',
    'Dispose': 'Releases the mounted filesystem.',
    # ChdCdReader
    'InitChdCdreader': 'Registers the CHD cdreader as the global cdreader.',
    # Cli
    'ReportUsage': 'Reports an application usage hit at launch (fire-and-forget).',
    'Flush': 'Waits up to two seconds for pending reports to finish.',
    'Emit': 'Forwards a log event to the bug report API (Warning+ events).',
    'ConfigureLogging': 'Builds the Serilog logger (console + bug-report sinks).',
    'Usage': 'Prints the usage banner and console table.',
    'FindConsoleId': 'Resolves a console key or numeric id to a console id.',
    'Atoi': 'C atoi semantics: optional sign, leading digits, 0 when none.',
    'ProcessFile': 'Processes a single file for a console.',
    'ProcessIteratedFile': 'Processes one wildcard match, printing the hash and filename.',
    'ProcessFiles': 'Expands a wildcard pattern and processes every match.',
    'Run': 'Executes the CLI argument loop.',
    'RhashLog': 'Verbose message callback routed through Serilog.',
    'RhashLogErrorMessage': 'Error message callback routed through Serilog (stderr).',
}

PARAM = {
    'hash': 'the generated 32-char hash', 'consoleId': 'the console identifier',
    'path': 'the file path', 'buffer': 'the buffer holding the data',
    'bufferSize': 'the size of the buffer', 'size': 'the size',
    'iterator': 'the hash iterator', 'md5': 'the MD5 state', 'data': 'the data',
    'count': 'the number of bytes', 'message': 'the message text',
    'callback': 'the callback to register', 'reader': 'the reader to register',
    'fileHandle': 'the open file handle', 'offset': 'the byte offset',
    'origin': 'the seek origin', 'trackHandle': 'the open track handle',
    'sector': 'the sector number', 'requestedBytes': 'the number of bytes requested',
    'numHandlers': 'the number of handlers in the table', 'key': 'the console key or numeric id',
    'value': 'the value', 'appname': 'the application name', 'pattern': 'the wildcard pattern',
    'file': 'the file path', 'args': 'the command-line arguments', 'apiKey': 'the API key',
    'url': 'the API endpoint URL', 'json': 'the JSON payload', 'logEvent': 'the log event',
    'ex': 'the exception',
}


def escape(text):
    return text.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')


def clean(text):
    text = re.sub(r'/\*+|\*+/|^\s*//\s?', '', text)
    return ' '.join(text.split())[:160]


def words(name):
    name = re.sub(r'([a-z0-9])([A-Z])', r'\1 \2', name)
    name = re.sub(r'([A-Z]+)([A-Z][a-z])', r'\1 \2', name)
    return name.replace('_', ' ').strip().lower()


def summary_for(name):
    if name in DOMAIN:
        return DOMAIN[name]
    for prefix, console in CONSOLE.items():
        if name.startswith('RcHash' + prefix):
            return f'Hashes a {console} image.'
        if name.startswith('TestHash' + prefix):
            return f'Tests hashing of a {console} image.'
    if name.startswith('RcHash'):
        return 'Hashes the image for the console.'
    if name.startswith('TestHash') or name.startswith('Test'):
        return 'Tests ' + words(name[len('Test'):]) + '.'
    return words(name) + '.'


def find_params(lines, idx):
    """Extract parameter names from a possibly multi-line signature."""
    depth = 0
    sig = ''
    for line in lines[idx:]:
        s = re.sub(r'//.*$', '', line)
        sig += ' ' + s
        depth += s.count('(') - s.count(')')
        if depth <= 0 and '(' in s:
            break
    start, end = sig.find('('), sig.rfind(')')
    if start < 0 or end < start:
        return []
    parts, cur, d = [], [], 0
    for ch in sig[start + 1:end]:
        if ch in '(<[':
            d += 1
        elif ch in ')>]':
            d -= 1
        if ch == ',' and d == 0:
            parts.append(''.join(cur))
            cur = []
        else:
            cur.append(ch)
    parts.append(''.join(cur))
    params = []
    for part in parts:
        part = part.strip()
        if not part:
            continue
        for mod in ('out ', 'ref ', 'in ', 'params '):
            if part.startswith(mod):
                part = part[len(mod):]
                break
        m = re.search(r'(\w+)\s*$', part)
        if m:
            params.append(m.group(1))
    return params


def returns_doc(ret, name):
    if ret in ('bool', 'bool?'):
        return '/// <returns>true on success; otherwise false</returns>'
    if ret == 'void':
        return None
    if ret == 'string':
        return '/// <returns>the generated value</returns>'
    if name == 'FileTell':
        return '/// <returns>the current position</returns>'
    if name == 'FileRead':
        return '/// <returns>the number of bytes read</returns>'
    if name == 'FileOpen' or name == 'OpenFile':
        return '/// <returns>the handle, or null on failure</returns>'
    if name == 'Iterate':
        return '/// <returns>nonzero when a console matched; zero when none did</returns>'
    if name == 'FindConsoleId':
        return '/// <returns>the console id, or 0 when unknown</returns>'
    if name == 'OpenTrack':
        return '/// <returns>the track handle, or null when the track cannot be opened</returns>'
    if name == 'Matches':
        return '/// <returns>true when the region matches the pattern</returns>'
    return '/// <returns>the result</returns>'


TYPE_RE = re.compile(r'^\s*(public|internal)\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(class|record|struct|enum|delegate)\s+(\w+)')
METHOD_RE = re.compile(r'^\s*(public|internal)\s+(?:static\s+|sealed\s+|override\s+|virtual\s+|new\s+|async\s+|unsafe\s+)*(?:[\w<>\[\]?.,]+\s+)+(\w+)\s*\(')
PRIV_RE = re.compile(r'^\s*private\s+(?:static\s+)*(?:[\w<>\[\]?.,]+\s+)+(\w+)\s*\(')


def file_header(lines):
    """First comment block of the file (used for class summaries)."""
    text = []
    for line in lines:
        s = line.strip()
        if s.startswith('//'):
            text.append(s.lstrip('/').strip())
        elif s == '' and text:
            continue
        elif s.startswith('/*'):
            text.append(s.strip('/*'))
        elif text and not s.startswith('*'):
            break
        elif s.startswith('*'):
            text.append(s.strip('*/'))
    if not text:
        return None
    return clean(' '.join(text))


def scan_up(lines, idx):
    """Scan upward from idx: skip attribute lines and blank lines; return
    (attribute_start, comment) where comment is 'HAS_DOCS' when a '///' line
    exists, otherwise the nearest comment text (or None)."""
    attr_start = idx
    comment = None
    j = idx - 1
    while j >= 0:
        s = lines[j].strip()
        if s == '' or s.startswith('['):
            if s.startswith('['):
                attr_start = j
            j -= 1
            continue
        if '///' in s:
            return attr_start, 'HAS_DOCS'
        if s.startswith('//') or s.startswith('/*') or s.startswith('*'):
            # collect the whole comment block above the declaration
            block = []
            k = j
            while k >= 0:
                t = lines[k].strip()
                if t.startswith('//') or t.startswith('/*') or t.startswith('*'):
                    block.append(t.strip('/*').strip('*/').strip())
                    k -= 1
                elif t == '' and block:
                    k -= 1
                    continue
                else:
                    break
            block.reverse()
            return attr_start, clean(' '.join(block))
        break
    return attr_start, comment


def process(path):
    rel = os.path.relpath(path).replace('\\', '/')
    doc_private = rel in DOC_PRIVATE
    with open(path, encoding='utf-8') as f:
        lines = f.read().split('\n')

    header = file_header(lines)
    out = []
    i = 0
    n = len(lines)
    while i < n:
        line = lines[i]
        m_type = TYPE_RE.match(line)
        m_meth = METHOD_RE.match(line)
        m_priv = PRIV_RE.match(line) if doc_private else None
        if not (m_type or m_meth or m_priv):
            out.append(line)
            i += 1
            continue

        if m_type:
            name = m_type.group(3)
        elif m_meth:
            name = m_meth.group(2)
        else:
            name = m_priv.group(1)
        is_class = bool(m_type)

        attr_start, comment = scan_up(lines, i)
        if comment == 'HAS_DOCS':
            out.append(line)
            i += 1
            continue

        if is_class:
            summary = comment or header or f'{words(name)}.'
            docs = [f'/// <summary>{escape(summary)}</summary>']
        else:
            summary = comment or summary_for(name)
            docs = [f'/// <summary>{escape(summary)}</summary>']
            for p in find_params(lines, i):
                docs.append(f'/// <param name="{p}">{escape(PARAM.get(p, "the " + words(p) + " parameter"))}</param>')
            ret = None
            if m_meth:
                m_ret = re.search(
                    r'^\s*(?:public|internal)\s+(?:static\s+|sealed\s+|override\s+|virtual\s+|new\s+|async\s+|unsafe\s+)*([\w<>\[\]?.,]+)\s+\w+\s*\(',
                    line)
                ret = m_ret.group(1) if m_ret else None
            r = returns_doc(ret or '', name)
            if r:
                docs.append(r)

        # lines[attr_start:i] were already appended as non-matches; pull them
        # back so the docs can be inserted before the attributes
        pulled = out[len(out) - (i - attr_start):] if attr_start < i else []
        if attr_start < i:
            del out[len(out) - (i - attr_start):]
        out.extend(docs)
        out.extend(pulled)
        out.append(line)
        i += 1

    with open(path, 'w', encoding='utf-8', newline='') as f:
        f.write('\n'.join(out))
    return True


if __name__ == '__main__':
    changed = []
    for root in ROOTS:
        for dirpath, _, files in os.walk(root):
            if 'obj' in dirpath or 'bin' in dirpath:
                continue
            for fn in sorted(files):
                if not fn.endswith('.cs'):
                    continue
                p = os.path.join(dirpath, fn)
                if process(p):
                    changed.append(os.path.relpath(p).replace('\\', '/'))
    print(f'{len(changed)} files updated:')
    for c in changed:
        print(' ', c)
