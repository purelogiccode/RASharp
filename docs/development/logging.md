# Logging & bug reports

All logging goes through **Serilog** (4.4.0). The console sink is configured
to reproduce the original's **byte-exact** output — the parity suite (which
compares raw stdout/stderr) is the contract, and it stays green.

## Levels and sinks

| Sink | Min level | Output |
|---|---|---|
| Console (stdout) | `Information` | engine verbose messages (`-v`) and program logs |
| Console (stderr) | `Error` | engine error messages (via `standardErrorFromLevel`) |
| Bug report API | `Warning` | Warning+ events, forwarded to the bug report service |

Logging levels used by the code:

| Level | Used for |
|---|---|
| `Information` | engine verbose callback (registered only with `-v` — parity-gated) |
| `Error` | engine error callback (stderr — parity-gated) |
| `Fatal` | unhandled exceptions in `Program.Main` |
| `Debug` | expected-handling catch blocks (`FileUtil`, `Hash3DS`) — below the console threshold, so parity output is never affected |

## Bug report forwarding

`Warning+` events are POSTed to the bug report API
([AspNet_BugReportEmailService](https://www.purelogiccode.com/bugreport)
— see its `InstructionsToSendBugs.md`):

```text
POST {url}/api/send-bug-report
X-API-KEY: {api key}
Content-Type: application/json
```

### Configuration (environment variables)

| Variable | Default | Purpose |
|---|---|---|
| `RASHARP_BUGREPORT_API_KEY` | *(decoded `Constants.BugReportApiKey`)* | optional override of the built-in API key |
| `RASHARP_BUGREPORT_URL` | `https://www.purelogiccode.com/bugreport/api/send-bug-report` | endpoint override |
| `RASHARP_BUGREPORT_DISABLE` | *(unset)* | set to `1` to force forwarding off |

The built-in API key lives in `RASharp.Cli/Constants.cs` **double-encoded**
(Base64 applied twice) so it is not readable in the source tree; the
application decodes it at startup and uses the real value. An explicit
`RASHARP_BUGREPORT_API_KEY` always takes precedence.

!!! note "Test runs never report"
    The parity harness sets `RASHARP_BUGREPORT_DISABLE=1` on every child
    process, so `dotnet test` never POSTs to the real API.

### Every report contains

```text
=== Environment Details ===
Date: 2026-08-12 01:08:03 -03:00
Application Name: RASharp
Application Version: 1.8.3
OS Version: Microsoft Windows NT 10.0.26200.0
Architecture: X64
Bitness: 64
Windows Version: 10.0.26200.0
Processor Count: 24
Base Directory: ...
Temp Path: ...

=== Error Details ===
<the log message>

=== Exception Details ===        (only when an exception is logged)
Type: ...
Message: ...
Source: ...
StackTrace: ...
```

The payload also maps `applicationName`, `version`, `environment` and
`stackTrace` (≤ 8000 chars) to the API's structured fields; the message
block is capped at 4000 chars.

### Reliability rules

- The sink never writes to stdout/stderr — parity output is untouched.
- All transport/API failures are swallowed: reporting is best-effort and
  logging must never break the application.
- A 5-second cooldown keeps the CLI under the API's 10 req/min rate limit.
- On shutdown (`Log.CloseAndFlush`), pending reports get up to 2 seconds to
  complete.

## try/catch + Log policy

Public entry points log and degrade gracefully instead of crashing:

- `Program.Main` — wraps the whole run: `Log.Fatal(ex, ...)`, prints a
  summary to stderr, exits `1` (the original segfaulted on some malformed
  invocations; this is the documented hardening).
- `RcHash.GenerateFromFile` / `GenerateFromBuffer` — catch, `Log.Error`,
  return `false` (the API's failure contract).
- Expected-handling catch blocks (`FileUtil.FullPath/OpenFile/LoadZippedFile`,
  `Hash3DS` key loading) — `Log.Debug` with the exception; the parity error
  message is still emitted by the engine callback as before.
