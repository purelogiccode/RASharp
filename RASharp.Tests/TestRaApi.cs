// Tests for the real HTTP path of RaApi.SendGet against a local TCP HTTP
// server (no production refactor needed — the client already takes a URL).
// The SendGetOverride hook itself is exercised by the fetch-db tests.

using System.Net;
using System.Net.Sockets;
using System.Text;
using RASharp.Cli;

namespace RASharp.Tests;

/// <summary>Tests for the real HTTP path of RaApi.SendGet against a local TCP HTTP server (no production refactor needed — the client already takes a URL). The SendGetOverrid</summary>
public class TestRaApi
{
    private static (string Url, Task Server) StartHttpServer(string statusLine, string responseBody, List<string>? requestLog)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var server = Task.Run(() =>
        {
            using var client = listener.AcceptTcpClient();
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);

            var requestLine = reader.ReadLine() ?? "";
            requestLog?.Add(requestLine);
            while (reader.ReadLine() is { Length: > 0 })
            {
                /* consume the remaining headers */
            }

            var body = Encoding.UTF8.GetBytes(responseBody);
            var header = statusLine + "\r\nContent-Length: " + body.Length + "\r\nConnection: close\r\n\r\n";
            stream.Write(Encoding.ASCII.GetBytes(header));
            stream.Write(body);
        });

        return ($"http://127.0.0.1:{port}/API/identify", server);
    }

    /// <summary>Tests that SendGet returns the response body on success.</summary>
    [Fact]
    public async Task SendGetReturnsResponseBody()
    {
        var requests = new List<string>();
        var (url, server) = StartHttpServer("HTTP/1.1 200 OK", "{\"ok\":true}", requests);
        try
        {
            var body = RaApi.SendGet(url);

            Assert.Equal("{\"ok\":true}", body);
            var finished = await Task.WhenAny(server, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(finished == server, "server never received the request");
            await server; /* surface any server-side exception */
            Assert.Contains(requests, line => line.StartsWith("GET /API/identify", StringComparison.Ordinal));
        }
        finally
        {
            RaApi.SendGetOverride = null;
        }
    }

    /// <summary>Tests that a non-success status is reported as null.</summary>
    [Fact]
    public async Task SendGetReturnsNullOnHttpError()
    {
        var (url, server) = StartHttpServer("HTTP/1.1 404 Not Found", "nope", null);
        try
        {
            var body = RaApi.SendGet(url);

            Assert.Null(body);
            var finished = await Task.WhenAny(server, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(finished == server, "server never received the request");
            await server; /* surface any server-side exception */
        }
        finally
        {
            RaApi.SendGetOverride = null;
        }
    }

    /// <summary>Tests that a connection failure is reported as null.</summary>
    [Fact]
    public void SendGetReturnsNullOnConnectionFailure()
    {
        /* reserve a port, then release it — nothing is listening anymore */
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        Assert.Null(RaApi.SendGet($"http://127.0.0.1:{port}/API/identify"));
    }
}
