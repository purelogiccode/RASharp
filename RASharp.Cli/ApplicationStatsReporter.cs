// Application usage telemetry.
//
// Reports a usage hit to the ApplicationStats API at application launch
// (AspNet_ApplicationStats — see its InstructionsToUseApiEndpoints.md):
//   POST {url}            body: {"applicationId":"RASharp","version":"..."}
//   Authorization: Bearer {api key}
//
// Design rules (same contract as the bug-report sink):
//  * NEVER writes to stdout/stderr — parity output is untouched.
//  * All failures are swallowed; the server rate-limits (1 call/hour/IP/app,
//    HTTP 429) and returns 200 with an error body on auth failure — neither
//    is treated as an error here.
//  * The API key is the decoded Constants.BugReportApiKey unless
//    RASHARP_STATS_API_KEY overrides it; endpoint override via
//    RASHARP_STATS_URL; force-off via RASHARP_STATS_DISABLE=1 (the parity
//    harness sets it so test runs never report).
//  * Flush() gives a pending report up to 2 seconds to complete at exit.

using System.Text;
using System.Text.Json;

namespace RASharp.Cli;

/* System.Threading.Lock is net9+ only; on net8 the alias degrades to a
 * plain object monitor. Either way the `lock` statements below use the
 * best primitive available on the target framework. */
#if NET9_0_OR_GREATER
using LockObject = Lock;

#else
using LockObject = object;
#endif

/// <summary>Application usage telemetry. Reports a usage hit to the ApplicationStats API at application launch (AspNet_ApplicationStats — see its InstructionsToUseApiEndpoi</summary>
internal static class ApplicationStatsReporter
{
    internal const string DefaultUrl = "https://www.purelogiccode.com/ApplicationStats/stats";

    private static readonly HttpClient SHttp = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly LockObject SPendingLock = new();
    private static readonly List<Task> SPending = new();
    private static bool _sEnabled = true;

    /* called once at application launch; never blocks the main flow */
    /// <summary>called once at application launch; never blocks the main flow</summary>
    public static void ReportUsage()
    {
        if (!_sEnabled)
            return;

        if (string.Equals(Environment.GetEnvironmentVariable("RASHARP_STATS_DISABLE"), "1", StringComparison.Ordinal))
        {
            _sEnabled = false;
            return;
        }

        var url = Environment.GetEnvironmentVariable("RASHARP_STATS_URL") ?? DefaultUrl;
        var apiKey = Environment.GetEnvironmentVariable("RASHARP_STATS_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            apiKey = Constants.BugReportApiKey;
        }

        var json = JsonSerializer.Serialize(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["applicationId"] = "RASharp",
            ["version"] = Program.Version
        });

        lock (SPendingLock)
        {
            SPending.Add(Task.Run(() => PostAsync(url, apiKey, json)));
        }
    }

    /* give a pending report up to 2 seconds to finish before process exit */
    /// <summary>give a pending report up to 2 seconds to finish before process exit</summary>
    public static void Flush()
    {
        Task[] pending;
        lock (SPendingLock)
        {
            pending = SPending.ToArray();
        }

        if (pending.Length == 0)
            return;

        try
        {
            Task.WaitAll(pending, TimeSpan.FromSeconds(2));
        }
        catch
        {
            /* telemetry is best-effort */
        }
    }

    private static async Task PostAsync(string url, string apiKey, string json)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", "Bearer " + apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await SHttp.SendAsync(request).ConfigureAwait(false);
        }
        catch
        {
            /* telemetry is best-effort */
        }
    }
}
