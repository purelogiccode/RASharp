// New implementation, behavior parity with RALibretro RAHasher (GPL-3.0,
// used as reference only). The console metadata is factual data copied from
// RAHasher.cpp's CONSOLES[]; group NULL marks "not supported by RA".

namespace RASharp.Cli.Models;

/// <summary>New implementation, behavior parity with RALibretro RAHasher (GPL-3.0, used as reference only). The console metadata is factual data copied from RAHasher.cpp's </summary>
public sealed record ConsoleInfo(uint Id, string Key, string? Group, string Name);
