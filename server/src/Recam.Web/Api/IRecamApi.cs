namespace Recam.Web.Api;

/// <summary>The server's REST API, as the browser Monitor uses it. Faked in tests.</summary>
public interface IRecamApi
{
    /// <summary>This browser's device, or null when the browser is not a Monitor (no cookie, or revoked).</summary>
    Task<MeInfo?> GetMeAsync(CancellationToken cancellationToken);

    /// <summary>True while the server has no Monitor, so this browser may become the first one.</summary>
    Task<bool> IsFirstOpenAsync(CancellationToken cancellationToken);

    Task<FirstOpenOutcome> FirstOpenAsync(string code, bool remember, CancellationToken cancellationToken);

    Task SignOutAsync(CancellationToken cancellationToken);

    /// <summary>Starts "Connect browser": a QR code for a Monitor phone to approve.</summary>
    Task<BrowserLinkInfo> CreateBrowserLinkAsync(CancellationToken cancellationToken);

    Task<ClaimOutcome> ClaimBrowserLinkAsync(BrowserLinkInfo link, bool remember, CancellationToken cancellationToken);

    Task<PairingTokenInfo> CreatePairingTokenAsync(DeviceKind kind, CancellationToken cancellationToken);

    /// <summary>Whether a phone already paired with the QR this browser created.</summary>
    Task<bool> IsPairingTokenUsedAsync(Guid tokenId, CancellationToken cancellationToken);

    /// <summary>UTC days with recordings of one camera, newest first.</summary>
    Task<IReadOnlyList<DateOnly>> GetRecordingDaysAsync(Guid cameraId, CancellationToken cancellationToken);

    /// <summary>The recorded stretches of one UTC day.</summary>
    Task<IReadOnlyList<RecordingPieceInfo>> GetRecordingsAsync(Guid cameraId, DateOnly utcDay, CancellationToken cancellationToken);

    Task<QuotaInfo> GetQuotaAsync(CancellationToken cancellationToken);

    Task SetQuotaAsync(int megabytes, CancellationToken cancellationToken);

    /// <summary>Every device still on the server, cameras first.</summary>
    Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken);

    /// <summary>Takes a device off the server. False when the server refuses (it no longer exists, or it is this browser).</summary>
    Task<bool> RemoveDeviceAsync(Guid deviceId, CancellationToken cancellationToken);

    /// <summary>The server's cameras, or null when this browser is no longer a Monitor.</summary>
    Task<IReadOnlyList<CameraInfo>?> GetCamerasAsync(CancellationToken cancellationToken);
}
