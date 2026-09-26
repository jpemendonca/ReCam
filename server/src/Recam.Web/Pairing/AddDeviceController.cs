using Recam.Web.Api;

namespace Recam.Web.Pairing;

/// <summary>
/// Shows a QR code that pairs a phone as a camera or a Monitor, replaces it with a new one when it
/// expires, and asks the server whether someone already paired with it. Same behavior as the
/// app; the page drives <see cref="Tick"/> every second and <see cref="CheckPairedAsync"/> every
/// two seconds.
/// </summary>
public sealed class AddDeviceController(IRecamApi api)
{
    private Guid _tokenId;
    private bool _checking;

    public DeviceKind Kind { get; private set; }

    public AddDeviceState State { get; private set; } = AddDeviceState.Loading;

    public string? QrUri { get; private set; }

    public string? QrSvg { get; private set; }

    public TimeSpan Remaining { get; private set; }

    public event Action? Changed;

    public async Task StartAsync(DeviceKind kind)
    {
        Kind = kind;
        State = AddDeviceState.Loading;
        Changed?.Invoke();
        try
        {
            var token = await api.CreatePairingTokenAsync(kind, CancellationToken.None);
            _tokenId = token.Id;
            QrUri = token.QrUri;
            QrSvg = Pairing.QrSvg.Render(token.QrUri);
            Remaining = token.ValidFor;
            State = AddDeviceState.Ready;
        }
        catch (HttpRequestException)
        {
            State = AddDeviceState.Failed;
        }

        Changed?.Invoke();
    }

    /// <summary>Counts down; an expired QR is replaced by a new one.</summary>
    public async Task Tick(TimeSpan elapsed)
    {
        if (State != AddDeviceState.Ready)
        {
            return;
        }

        Remaining -= elapsed;
        if (Remaining <= TimeSpan.Zero)
        {
            await StartAsync(Kind);
            return;
        }

        Changed?.Invoke();
    }

    public async Task CheckPairedAsync()
    {
        if (State != AddDeviceState.Ready || _checking)
        {
            return;
        }

        _checking = true;
        try
        {
            var tokenId = _tokenId;
            if (await api.IsPairingTokenUsedAsync(tokenId, CancellationToken.None) && tokenId == _tokenId && State == AddDeviceState.Ready)
            {
                State = AddDeviceState.Paired;
                Changed?.Invoke();
            }
        }
        catch (HttpRequestException)
        {
            // The next check tries again.
        }
        finally
        {
            _checking = false;
        }
    }
}
