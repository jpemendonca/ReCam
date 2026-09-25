using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Recam.Server.Tests.Support;

/// <summary>
/// Stands in for MediaMTX where a test only needs to see which path the proxy asked for.
/// Answers a POST like MediaMTX does, with a Location naming a new session.
/// </summary>
public sealed class FakeMediaMtx : IDisposable
{
    public const string SessionId = "3f2c1a9e-0000-4000-8000-000000000001";

    private readonly HttpListener _listener = new();
    private readonly ConcurrentQueue<string> _requests = new();
    private readonly Task _loop;

    public FakeMediaMtx()
    {
        Url = new Uri($"http://127.0.0.1:{FreePort()}/");
        _listener.Prefixes.Add(Url.ToString());
        _listener.Start();
        _loop = Task.Run(ServeAsync);
    }

    public Uri Url { get; }

    /// <summary>"METHOD /path" of every request received.</summary>
    public IReadOnlyCollection<string> Requests => _requests;

    public void Dispose()
    {
        _listener.Stop();
        _listener.Close();
        _loop.Wait(TimeSpan.FromSeconds(5));
    }

    private static int FreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }

    private async Task ServeAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            var path = context.Request.Url!.AbsolutePath;
            _requests.Enqueue($"{context.Request.HttpMethod} {path}");
            if (context.Request.HttpMethod == "POST")
            {
                context.Response.StatusCode = (int)HttpStatusCode.Created;
                context.Response.Headers["Location"] = $"{path.TrimEnd('/')}/{SessionId}";
            }

            context.Response.Close();
        }
    }
}
