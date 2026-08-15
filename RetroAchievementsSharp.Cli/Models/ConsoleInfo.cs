// New implementation, behavior parity with RALibretro RAHasher (GPL-3.0,
// used as reference only). The console metadata is factual data copied from
// RAHasher.cpp's CONSOLES[]; group NULL marks "not supported by RA".

namespace RetroAchievementsSharp.Cli.Models;

/// <summary>New implementation, behavior parity with RALibretro RAHasher (GPL-3.0, used as reference only). The console metadata is factual data copied from RAHasher.cpp's </summary>
/// <param name="Id">the console identifier</param>
/// <param name="Key">the CLI console key ("NES", "GB", ...)</param>
/// <param name="Group">the console group ("Nintendo", "Sony", ...), or null when not supported by RA</param>
/// <param name="Name">the display name</param>
internal sealed record ConsoleInfo(uint Id, string Key, string? Group, string Name);