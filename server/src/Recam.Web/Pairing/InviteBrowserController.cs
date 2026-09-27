using Recam.Web.Api;

namespace Recam.Web.Pairing;

/// <summary>
/// "Add Monitor › In a browser": shows an invitation link, as a QR code and as text, that another
/// browser opens to become a Monitor. Every <see cref="CheckEvery"/> it asks the server whether the
/// link was used, so the page can leave by itself; when it runs out, it says so. The page drives
/// <see cref="Tick"/> every second.
/// </summary>
public sealed class InviteBrowserController(IRecamApi api, IClipboard clipboard)
{
    public static readonly TimeSpan CheckEvery = TimeSpan.FromSeconds(2);

    private Guid _inviteId;
    private TimeSpan _sinceCheck;

    public InviteState State { get; private set; } = InviteState.Loading;

    public string? Url { get; private set; }

    public string? QrSvg { get; private set; }

    /// <summary>Scheme, host and port of the link: the browser's access only works there.</summary>
    public string? Address { get; private set; }

    public TimeSpan Remaining { get; private set; }

    /// <summary>Null before any copy; false when the browser refused to copy.</summary>
    public bool? Copied { get; private set; }

    public event Action? Changed;

    public async Task StartAsync()
    {
        State = InviteState.Loading;
        Copied = null;
        Changed?.Invoke();
        try
        {
            var invite = await api.CreateBrowserInviteAsync(CancellationToken.None);
            _inviteId = invite.Id;
            _sinceCheck = TimeSpan.Zero;
            Url = invite.Url;
            QrSvg = Pairing.QrSvg.Render(invite.Url);
            Address = new Uri(invite.Url).GetLeftPart(UriPartial.Authority);
            Remaining = invite.ValidFor;
            State = InviteState.Ready;
        }
        catch (HttpRequestException)
        {
            State = InviteState.Failed;
        }

        Changed?.Invoke();
    }

    /// <summary>Counts down, and now and then asks whether a browser used the link.</summary>
    public async Task Tick(TimeSpan elapsed)
    {
        if (State != InviteState.Ready)
        {
            return;
        }

        Remaining -= elapsed;
        _sinceCheck += elapsed;
        if (_sinceCheck >= CheckEvery || Remaining <= TimeSpan.Zero)
        {
            _sinceCheck = TimeSpan.Zero;
            State = await CheckAsync() switch
            {
                BrowserInviteState.Used => InviteState.Used,
                BrowserInviteState.Expired => InviteState.Expired,
                _ when Remaining <= TimeSpan.Zero => InviteState.Expired,
                _ => InviteState.Ready,
            };
        }

        Changed?.Invoke();
    }

    // A check that fails leaves the link as it is; the next one tries again.
    private async Task<BrowserInviteState> CheckAsync()
    {
        try
        {
            return await api.GetBrowserInviteStateAsync(_inviteId, CancellationToken.None);
        }
        catch (HttpRequestException)
        {
            return BrowserInviteState.Waiting;
        }
    }

    public async Task CopyAsync()
    {
        if (Url is null)
        {
            return;
        }

        Copied = await clipboard.WriteAsync(Url);
        Changed?.Invoke();
    }
}
