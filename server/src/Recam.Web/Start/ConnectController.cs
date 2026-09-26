using Recam.Web.Api;
using Recam.Web.Pairing;

namespace Recam.Web.Start;

/// <summary>
/// "Connect browser", on this browser's side: shows the QR code, replaces it when it expires and
/// asks every two seconds whether a Monitor phone approved. The page drives <see cref="Tick"/>
/// and <see cref="CheckAsync"/>, like the add-device panel.
/// </summary>
public sealed class ConnectController(IRecamApi api)
{
    private BrowserLinkInfo? _link;
    private bool _checking;

    public string? QrSvg { get; private set; }

    public TimeSpan Remaining { get; private set; }

    public bool Remember { get; set; } = true;

    public bool Failed { get; private set; }

    public bool Connected { get; private set; }

    public event Action? Changed;

    public async Task StartAsync()
    {
        Failed = false;
        try
        {
            _link = await api.CreateBrowserLinkAsync(CancellationToken.None);
            QrSvg = Pairing.QrSvg.Render(_link.QrUri);
            Remaining = _link.ValidFor;
        }
        catch (HttpRequestException)
        {
            _link = null;
            Failed = true;
        }

        Changed?.Invoke();
    }

    public async Task Tick(TimeSpan elapsed)
    {
        if (_link is null || Connected)
        {
            return;
        }

        Remaining -= elapsed;
        if (Remaining <= TimeSpan.Zero)
        {
            await StartAsync();
            return;
        }

        Changed?.Invoke();
    }

    public async Task CheckAsync()
    {
        if (_link is not { } link || Connected || _checking)
        {
            return;
        }

        _checking = true;
        try
        {
            switch (await api.ClaimBrowserLinkAsync(link, Remember, CancellationToken.None))
            {
                case ClaimOutcome.Claimed:
                    Connected = true;
                    Changed?.Invoke();
                    break;
                case ClaimOutcome.Gone:
                    await StartAsync();
                    break;
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
