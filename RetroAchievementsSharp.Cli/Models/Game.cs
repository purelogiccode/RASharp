// A game entry from the RetroAchievements database snapshot loaded by
// RetroAchievementsDatabase (the JSON produced by the
// RetroAchievements.DataFetcher tool).

namespace RetroAchievementsSharp.Cli.Models;

/// <summary>A game entry from the database.</summary>
/// <param name="Id">the game identifier</param>
/// <param name="Title">the game title</param>
/// <param name="ConsoleName">the display name of the game's console</param>
/// <param name="NumAchievements">the number of achievements for the game</param>
/// <param name="Points">the total points of the game's achievements</param>
internal sealed record Game(int Id, string Title, string ConsoleName, int NumAchievements, int Points);