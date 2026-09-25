using Microsoft.Net.Http.Headers;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Http;

namespace Recam.Server.Features.Media;

/// <summary>
/// Relays WHIP (camera publishes) and WHEP (viewer plays) signaling to MediaMTX. Only SDP
/// travels here; the video itself goes over UDP straight to MediaMTX (SPECS.md 5.5).
/// </summary>
public static class MediaProxyEndpoints
{
    public const string HttpClientName = "mediamtx";

    private const int MaxSignalingBytes = 64 * 1024;
    private static readonly string[] SessionMethods = [HttpMethods.Patch, HttpMethods.Delete];

    public static IServiceCollection AddMediaProxy(this IServiceCollection services, ServerSettings settings)
    {
        services.AddHttpClient(HttpClientName, client => client.BaseAddress = settings.MediaMtxUrl);
        return services;
    }

    public static IEndpointRouteBuilder MapMediaProxyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var whip = endpoints.MapGroup("/whip/{cameraId:guid}").RequireAuthorization(AuthExtensions.CameraOnly);
        whip.MapPost(string.Empty, (Guid cameraId, HttpContext context, IHttpClientFactory clients) =>
            PublishAsync(cameraId, null, context, clients));
        whip.MapMethods("{session}", SessionMethods, (Guid cameraId, string session, HttpContext context, IHttpClientFactory clients) =>
            PublishAsync(cameraId, session, context, clients));

        var whep = endpoints.MapGroup("/whep/{cameraId:guid}").RequireAuthorization(AuthExtensions.ViewerOrOwner);
        whep.MapPost(string.Empty, (Guid cameraId, HttpContext context, IHttpClientFactory clients) =>
            ForwardAsync(cameraId, MediaProtocol.Whep, null, context, clients));
        whep.MapMethods("{session}", SessionMethods, (Guid cameraId, string session, HttpContext context, IHttpClientFactory clients) =>
            ForwardAsync(cameraId, MediaProtocol.Whep, session, context, clients));
        return endpoints;
    }

    private static Task<IResult> PublishAsync(Guid cameraId, string? session, HttpContext context, IHttpClientFactory clients)
    {
        if (context.User.GetDeviceId() != cameraId)
        {
            return Task.FromResult(MediaErrors.NotYourCamera.ToHttpResult());
        }

        return ForwardAsync(cameraId, MediaProtocol.Whip, session, context, clients);
    }

    private static async Task<IResult> ForwardAsync(
        Guid cameraId, MediaProtocol protocol, string? session, HttpContext context, IHttpClientFactory clients)
    {
        var body = await ReadBodyAsync(context.Request, context.RequestAborted);
        if (body is null)
        {
            return MediaErrors.SignalingTooLarge.ToHttpResult();
        }

        using var upstreamRequest = new HttpRequestMessage(
            new HttpMethod(context.Request.Method),
            UpstreamPath(cameraId, protocol, session));
        if (body.Length > 0 || HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPatch(context.Request.Method))
        {
            upstreamRequest.Content = new ByteArrayContent(body);
            if (context.Request.ContentType is { } contentType)
            {
                upstreamRequest.Content.Headers.TryAddWithoutValidation(HeaderNames.ContentType, contentType);
            }
        }

        if (context.Request.Headers.IfMatch.ToString() is { Length: > 0 } ifMatch)
        {
            upstreamRequest.Headers.TryAddWithoutValidation(HeaderNames.IfMatch, ifMatch);
        }

        using var upstream = await clients.CreateClient(HttpClientName)
            .SendAsync(upstreamRequest, context.RequestAborted);
        var responseBody = await upstream.Content.ReadAsByteArrayAsync(context.RequestAborted);

        var response = context.Response;
        response.StatusCode = (int)upstream.StatusCode;
        CopyHeader(upstream, response, HeaderNames.ETag);
        CopyHeader(upstream, response, "Accept-Patch");
        if (upstream.Headers.Location is { } location)
        {
            response.Headers.Location = ProxyLocation(cameraId, protocol, location);
        }

        if (responseBody.Length > 0)
        {
            response.ContentType = upstream.Content.Headers.ContentType?.ToString();
            await response.Body.WriteAsync(responseBody, context.RequestAborted);
        }

        return Results.Empty;
    }

    // MediaMTX names each stream "cam-<id>" (deploy/mediamtx.yml only accepts that shape).
    private static string UpstreamPath(Guid cameraId, MediaProtocol protocol, string? session) =>
        session is null
            ? $"/cam-{cameraId:N}/{ProtocolSegment(protocol)}"
            : $"/cam-{cameraId:N}/{ProtocolSegment(protocol)}/{Uri.EscapeDataString(session)}";

    // MediaMTX answers with its own path for the session; clients must only see the proxy path.
    private static string ProxyLocation(Guid cameraId, MediaProtocol protocol, Uri location)
    {
        var path = location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString.Split('?')[0];
        var session = path.TrimEnd('/').Split('/')[^1];
        return $"/{ProtocolSegment(protocol)}/{cameraId}/{session}";
    }

    private static string ProtocolSegment(MediaProtocol protocol) => protocol == MediaProtocol.Whip ? "whip" : "whep";

    private static void CopyHeader(HttpResponseMessage upstream, HttpResponse response, string name)
    {
        if (upstream.Headers.TryGetValues(name, out var values))
        {
            response.Headers[name] = values.ToArray();
        }
    }

    /// <summary>Reads the SDP body, or returns null when it is larger than any real offer.</summary>
    private static async Task<byte[]?> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength > MaxSignalingBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > MaxSignalingBytes)
            {
                return null;
            }
        }

        return buffer.ToArray();
    }

    private enum MediaProtocol
    {
        Whip,
        Whep,
    }
}
