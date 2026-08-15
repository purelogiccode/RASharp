// Grants the hand-rolled slow test project (RetroAchievementsSharp.Slow.Tests, kept out of
// the solution) access to the internal test helpers shared by both suites
// (TestDataGen3Ds, TestHashNeo.GenerateNeoFile, ...).

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RetroAchievementsSharp.Slow.Tests")]