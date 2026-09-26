using Recam.Web.Api;

namespace Recam.Web.Devices;

/// <summary>
/// The devices on the server, cameras and Monitors, and removing one after the person confirms.
/// This browser is marked and cannot remove itself; for that there is Sign out.
/// </summary>
public sealed class DevicesController(IRecamApi api)
{
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

    public async Task LoadAsync()
    {
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
}
