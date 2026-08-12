// Minimal RetroAchievements API client for the identify and fetch-db
// subcommands. The public API requires the account's web API key (`y`
// param) plus username (`u` param) — see https://api-docs.retroachievements.org.
// SendGet is swappable for tests; failures are logged and reported as null.

using Serilog;

namespace RASharp.Cli;

/// <summary>Minimal RetroAchievements API client for the identify and fetch-db subcommands. The public API requires the account's web API key (`y` param) plus username (`u` param</summary>
internal static class RaApi
{
    internal const string DefaultBaseUrl = "https://retroachievements.org/API";

    private static readonly HttpClient SHttp = new() { Timeout = TimeSpan.FromSeconds(30) };

    /* test hook: replaces the HTTP GET with a canned response */
    internal static Func<string, string?>? SendGetOverride;

    /// <summary>Performs a GET and returns the response body, or null on failure.</summary>
    /// <param name="url">the url parameter</param>
    /// <returns>the response body, or null on failure</returns>
    internal static string? SendGet(string url)
    {
        if (SendGetOverride != null)
        {
            return SendGetOverride(url);
        }

        try
        {
            return SHttp.GetStringAsync(url).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "RA API request failed: {Url}", url);
            return null;
        }
    }
}
