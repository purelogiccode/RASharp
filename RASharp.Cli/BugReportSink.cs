// Bug report forwarding sink.
//
// Forwards Warning+ log events to the bug report API
// (AspNet_BugReportEmailService — see its InstructionsToSendBugs.md):
//   POST {RASHARP_BUGREPORT_URL or default} with header X-API-KEY.
//
// Every report carries the required detail blocks:
//   === Environment Details ===  Date, Application Name, Application Version,
//     OS Version, Architecture, Bitness, Windows Version, Processor Count,
//     Base Directory, Temp Path
//   === Error Details ===        the log message
//   === Exception Details ===    Type, Message, Source, StackTrace (when present)
//
// Design rules:
//  * The sink NEVER writes to stdout/stderr — parity output is untouched.
//  * All failures are swallowed (best-effort reporting; the API may be down,
//    rate-limited, or unreachable) — logging must never break the app.
//  * The API key is the decoded Constants.BugReportApiKey unless overridden
//    by RASHARP_BUGREPORT_API_KEY; RASHARP_BUGREPORT_DISABLE=1 forces the
//    sink off (the parity harness sets it so test runs never report).
//  * A small cooldown keeps us under the API's 10 req/min rate limit.

using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Serilog.Core;
using Serilog.Events;

namespace RASharp.Cli;

/// <summary>Bug report forwarding sink. Forwards Warning+ log events to the bug report API (AspNet_BugReportEmailService — see its InstructionsToSendBugs.md): POST {RASHARP</summary>
internal sealed class BugReportSink : ILogEventSink, IDisposable
{
    internal const string DefaultUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";
    private const int MaxMessageLength = 4000;
    private const int MaxStackTraceLength = 8000;

    private static readonly HttpClient SHttp = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly Lock SRateLock = new();
    private static DateTime _sLastSentUtc = DateTime.MinValue;

    private readonly string _url;
    private readonly string _apiKey;
    private readonly Lock _pendingLock = new();
    private readonly List<Task> _pending = new();

    public BugReportSink(string url, string apiKey)
    {
        _url = url;
        _apiKey = apiKey;
    }

    /// <summary>Forwards a log event to the bug report API (Warning+ events).</summary>
    /// <param name="logEvent">the log event</param>
    public void Emit(LogEvent logEvent)
    {
        try
        {
            lock (SRateLock)
            {
                if ((DateTime.UtcNow - _sLastSentUtc).TotalSeconds < 5)
                    return; /* rate-limit guard (API allows 10/min per IP) */

                _sLastSentUtc = DateTime.UtcNow;
            }

            var message = BuildReport(logEvent);
            var payload = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["message"] = message,
                ["applicationName"] = "RASharp",
                ["version"] = Program.Version,
                ["environment"] = "cli",
                ["stackTrace"] = logEvent.Exception is null ? null : Truncate(logEvent.Exception.ToString(), MaxStackTraceLength)
            };

            var json = JsonSerializer.Serialize(payload);
            lock (_pendingLock)
            {
                _pending.Add(Task.Run(() => PostAsync(json)));
            }
        }
        catch
        {
            /* never let logging break the application */
        }
    }

    private async Task PostAsync(string json)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _url);
            request.Headers.Add("X-API-KEY", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await SHttp.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            /* best-effort reporting — swallow transport/API failures */
        }
    }

    /// <summary>Releases the mounted filesystem.</summary>
    public void Dispose()
    {
        Task[] pending;
        lock (_pendingLock)
        {
            pending = _pending.ToArray();
        }

        if (pending.Length > 0)
        {
            try
            {
                Task.WaitAll(pending, TimeSpan.FromSeconds(2));
            }
            catch
            {
                /* never throw from dispose */
            }
        }
    }

    /* The required detail blocks (see InstructionsToSendBugs.md) */
    private static string BuildReport(LogEvent logEvent)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== Environment Details ===");
        sb.Append("Date: ").AppendLine(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
        sb.Append("Application Name: ").AppendLine("RASharp");
        sb.Append("Application Version: ").AppendLine(Program.Version);
        sb.Append("OS Version: ").AppendLine(Environment.OSVersion.VersionString);
        sb.Append("Architecture: ").AppendLine(RuntimeInformation.OSArchitecture.ToString("G"));
        sb.Append("Bitness: ").AppendLine(Environment.Is64BitOperatingSystem ? "64" : "32");
        sb.Append("Windows Version: ").AppendLine(Environment.OSVersion.Version.ToString(4));
        sb.Append("Processor Count: ").AppendLine(Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture));
        sb.Append("Base Directory: ").AppendLine(AppContext.BaseDirectory);
        sb.Append("Temp Path: ").AppendLine(Path.GetTempPath());

        sb.AppendLine();
        sb.AppendLine("=== Error Details ===");
        sb.AppendLine(logEvent.RenderMessage());

        if (logEvent.Exception is not null)
        {
            sb.AppendLine();
            sb.AppendLine("=== Exception Details ===");
            sb.Append("Type: ").AppendLine(logEvent.Exception.GetType().FullName);
            sb.Append("Message: ").AppendLine(logEvent.Exception.Message);
            sb.Append("Source: ").AppendLine(logEvent.Exception.Source);
            sb.Append("StackTrace: ").AppendLine(logEvent.Exception.StackTrace);
        }

        return Truncate(sb.ToString(), MaxMessageLength);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
