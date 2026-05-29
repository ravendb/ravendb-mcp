using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RavenDB.Mcp.Tests;

internal sealed class FakeRavenHttpServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly Dictionary<string, Func<HttpListenerRequest, FakeResponse>> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Task _loop;
    private readonly CancellationTokenSource _shutdown = new();

    public FakeRavenHttpServer()
    {
        Url = $"http://127.0.0.1:{GetFreePort()}/";
        _listener.Prefixes.Add(Url);
        _listener.Start();
        _loop = Task.Run(Listen);
    }

    public string Url { get; }

    public List<string> Requests { get; } = [];

    public void Json(string path, string json)
    {
        _routes[path] = _ => new FakeResponse(200, "application/json", Encoding.UTF8.GetBytes(json));
    }

    public void Text(string path, string text, string contentType = "text/plain")
    {
        _routes[path] = _ => new FakeResponse(200, contentType, Encoding.UTF8.GetBytes(text));
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        _listener.Stop();

        try
        {
            await _loop;
        }
        catch (HttpListenerException)
        {
        }
        catch (ObjectDisposedException)
        {
        }

        _listener.Close();
        _shutdown.Dispose();
    }

    private async Task Listen()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            var context = await _listener.GetContextAsync();
            var requestPath = context.Request.Url?.AbsolutePath ?? "/";

            lock (Requests)
                Requests.Add(requestPath);

            if (!_routes.TryGetValue(requestPath, out var route))
            {
                await Write(context.Response, new FakeResponse(404, "text/plain", Encoding.UTF8.GetBytes("Not found")));
                continue;
            }

            await Write(context.Response, route(context.Request));
        }
    }

    private static async Task Write(HttpListenerResponse response, FakeResponse value)
    {
        response.StatusCode = value.StatusCode;
        response.ContentType = value.ContentType;
        response.ContentLength64 = value.Body.Length;
        await response.OutputStream.WriteAsync(value.Body);
        response.Close();
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed record FakeResponse(int StatusCode, string ContentType, byte[] Body);
}
