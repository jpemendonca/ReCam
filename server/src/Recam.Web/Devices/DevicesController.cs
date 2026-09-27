using Recam.Web.Api;
using Recam.Web.Realtime;

namespace Recam.Web.Devices;

/// <summary>
/// The devices on the server, cameras and Monitors, and removing one after the person confirms.
/// This browser is marked and cannot remove itself; for that there is Sign out.
/// </summary>
public sealed class DevicesController(IRecamApi api, IDeviceHub hub) : IDisposable
{
    private bool _listening;
    private List<DeviceInfo> _devices = [];

    public IReadOnlyList<DeviceInfo> Cameras => [.. _devices.Where(device => device.IsCamera)];

    public IReadOnlyList<DeviceInfo> Monitors => [.. _devices.Where(device => !device.IsCamera)];

    public Guid? ThisBrowser { get; private set; }

    public bool Loading { get; private set; } = true;

    public bool Failed { get; private set; }

    /// <summary>The device waiting for the person to confirm its removal.</summary>
    public DeviceInfo? Confirming { get; private set; }

    public bool RemoveFailed { get; private set; }

    public event Action? Changed;

    /// <summary>Loads the list and keeps it current: the server says when it changes.</summary>
    public async Task LoadAsync()
    {
        if (!_listening)
        {
            hub.DevicesChanged += OnDevicesChanged;
            _listening = true;
        }

        Loading = true;
        Failed = false;
        Changed?.Invoke();
        try
        {
            ThisBrowser = (await api.GetMeAsync(CancellationToken.None))?.DeviceId;
            _devices = [.. await api.GetDevicesAsync(CancellationToken.None)];
        }
        catch (HttpRequestException)
        {
            Failed = true;
        }

        Loading = false;
        Changed?.Invoke();
    }

    public void AskToRemove(DeviceInfo device)
    {
        if (device.Id == ThisBrowser)
        {
            return;
        }

        Confirming = device;
        RemoveFailed = false;
        Changed?.Invoke();
    }

    public void Cancel()
    {
        Confirming = null;
        Changed?.Invoke();
    }

    public async Task ConfirmRemoveAsync()
    {
        if (Confirming is not { } device)
        {
            return;
        }

        try
        {
            RemoveFailed = !await api.RemoveDeviceAsync(device.Id, CancellationToken.None);
        }
        catch (HttpRequestException)
        {
            RemoveFailed = true;
        }

        Confirming = null;
        await LoadAsync();
    }

    public void Dispose() => hub.DevicesChanged -= OnDevicesChanged;

    // A quiet reload: the list stays on screen while the new one arrives.
    private void OnDevicesChanged() => _ = RefreshAsync();

    private async Task RefreshAsync()
    {
        try
        {
            _devices = [.. await api.GetDevicesAsync(CancellationToken.None)];
            Failed = false;
        }
        catch (HttpRequestException)
        {
            // The next change, or reopening the page, tries again.
            return;
        }

        if (Confirming is { } device && _devices.All(other => other.Id != device.Id))
        {
            Confirming = null;
        }

        Changed?.Invoke();
    }
}
