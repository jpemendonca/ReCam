using Recam.Web.Api;

namespace Recam.Web.Cameras;

/// <summary>
/// The newest recorded picture of each camera, for the mosaic (bullet 13.1). The page asks again
/// every <see cref="RefreshEvery"/> and when a camera comes online or starts or stops sending.
/// </summary>
public sealed class ThumbnailController(IRecamApi api)
{
    public static readonly TimeSpan RefreshEvery = TimeSpan.FromMinutes(1);

    private readonly Dictionary<Guid, ThumbnailInfo> _pictures = [];
    private string _seen = string.Empty;

    public event Action? Changed;

    /// <summary>The camera's picture, or null while it has none.</summary>
    public ThumbnailInfo? Of(Guid cameraId) => _pictures.GetValueOrDefault(cameraId);

    /// <summary>Asks again for every camera's picture.</summary>
    public async Task RefreshAsync(IReadOnlyList<CameraInfo> cameras, CancellationToken cancellationToken)
    {
        _seen = Fingerprint(cameras);
        foreach (var camera in cameras)
        {
            try
            {
                if (await api.GetThumbnailAsync(camera.Id, cancellationToken) is { } picture)
                {
                    _pictures[camera.Id] = picture;
                }
            }
            catch (HttpRequestException)
            {
                // Keeps the last picture; the next refresh tries again.
            }
        }

        foreach (var gone in _pictures.Keys.Where(id => cameras.All(camera => camera.Id != id)).ToList())
        {
            _pictures.Remove(gone);
        }

        Changed?.Invoke();
    }

    /// <summary>Refreshes only when a camera came, went, or changed what it does.</summary>
    public Task FollowAsync(IReadOnlyList<CameraInfo> cameras, CancellationToken cancellationToken) =>
        Fingerprint(cameras) == _seen ? Task.CompletedTask : RefreshAsync(cameras, cancellationToken);

    private static string Fingerprint(IReadOnlyList<CameraInfo> cameras) =>
        string.Join(';', cameras.Select(camera => $"{camera.Id}:{camera.Online}:{camera.Publishing}"));
}
