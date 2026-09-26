using Recam.Web.Api;

namespace Recam.Web.Tests.Support;

/// <summary>A server in memory: one first-time code, and at most one Monitor.</summary>
public sealed class FakeRecamApi : IRecamApi
{
    public const string Code = "ABCD-EFGH";

    public MeInfo? Me { get; set; }

    /// <summary>A Monitor that is not this browser.</summary>
    public bool OtherMonitor { get; set; }

    public bool Offline { get; set; }

    public List<bool> RememberSent { get; } = [];

    public List<CameraInfo> Cameras { get; } = [];

    /// <summary>Every pairing QR created, in order, with whether a phone used it.</summary>
    public List<(PairingTokenInfo Token, DeviceKind Kind, bool Used)> Tokens { get; } = [];

    public TimeSpan TokenValidFor { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Recorded stretches by UTC day, for every camera.</summary>
    public Dictionary<DateOnly, List<RecordingPieceInfo>> Recordings { get; } = [];

    /// <summary>The UTC days asked for, in order.</summary>
    public List<DateOnly> RecordingDaysAsked { get; } = [];

    /// <summary>Monitors besides this browser; cameras come from <see cref="Cameras"/>.</summary>
    public List<DeviceInfo> OtherMonitors { get; } = [];

    public List<Guid> Removed { get; } = [];

    public QuotaInfo Quota { get; set; } = new(2048, 300L * 1024 * 1024, 10L * 1024 * 1024 * 1024);

    /// <summary>A phone scans the last QR; a camera also shows up in the list.</summary>
    public void UseLastToken(string cameraName = "Porta")
    {
        var last = Tokens[^1];
        Tokens[^1] = last with { Used = true };
        if (last.Kind == DeviceKind.Camera)
        {
            Cameras.Add(Support.Cameras.Make(cameraName));
        }
    }

    public Task<MeInfo?> GetMeAsync(CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        return Task.FromResult(Me);
    }

    public Task<bool> IsFirstOpenAsync(CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        return Task.FromResult(Me is null && !OtherMonitor);
    }

    public Task<FirstOpenOutcome> FirstOpenAsync(string code, bool remember, CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        RememberSent.Add(remember);
        if (Me is not null || OtherMonitor)
        {
            return Task.FromResult(FirstOpenOutcome.AlreadyHasMonitor);
        }

        if (!string.Equals(code, Code, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(FirstOpenOutcome.WrongCode);
        }

        Me = new MeInfo(Guid.NewGuid(), "Navegador · Chrome no Windows", "owner");
        return Task.FromResult(FirstOpenOutcome.Opened);
    }

    public Task SignOutAsync(CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        Me = null;
        return Task.CompletedTask;
    }

    public Task<PairingTokenInfo> CreatePairingTokenAsync(DeviceKind kind, CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        var token = new PairingTokenInfo(Guid.NewGuid(), $"recam://pair?v=1&t=token{Tokens.Count + 1}&r={kind}", TokenValidFor);
        Tokens.Add((token, kind, false));
        return Task.FromResult(token);
    }

    public Task<bool> IsPairingTokenUsedAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        return Task.FromResult(Tokens.Any(entry => entry.Token.Id == tokenId && entry.Used));
    }

    public Task<IReadOnlyList<DateOnly>> GetRecordingDaysAsync(Guid cameraId, CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        return Task.FromResult<IReadOnlyList<DateOnly>>([.. Recordings.Keys.OrderDescending()]);
    }

    public Task<IReadOnlyList<RecordingPieceInfo>> GetRecordingsAsync(Guid cameraId, DateOnly utcDay, CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        RecordingDaysAsked.Add(utcDay);
        return Task.FromResult<IReadOnlyList<RecordingPieceInfo>>(Recordings.GetValueOrDefault(utcDay) ?? []);
    }

    public Task<QuotaInfo> GetQuotaAsync(CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        return Task.FromResult(Quota);
    }

    public Task SetQuotaAsync(int megabytes, CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        Quota = Quota with { QuotaMb = megabytes };
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        var devices = Cameras.Select(camera => new DeviceInfo(camera.Id, camera.Name, "camera", camera.Online))
            .Concat(Me is null ? [] : [new DeviceInfo(Me.DeviceId, Me.Name, Me.Role, true)])
            .Concat(OtherMonitors);
        return Task.FromResult<IReadOnlyList<DeviceInfo>>([.. devices]);
    }

    public Task<bool> RemoveDeviceAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        if (deviceId == Me?.DeviceId)
        {
            return Task.FromResult(false);
        }

        Removed.Add(deviceId);
        var removed = Cameras.RemoveAll(camera => camera.Id == deviceId) + OtherMonitors.RemoveAll(monitor => monitor.Id == deviceId);
        return Task.FromResult(removed > 0);
    }

    public Task<IReadOnlyList<CameraInfo>?> GetCamerasAsync(CancellationToken cancellationToken)
    {
        ThrowIfOffline();
        return Task.FromResult<IReadOnlyList<CameraInfo>?>(Me is null ? null : [.. Cameras]);
    }

    private void ThrowIfOffline()
    {
        if (Offline)
        {
            throw new HttpRequestException("The server is off.");
        }
    }
}
