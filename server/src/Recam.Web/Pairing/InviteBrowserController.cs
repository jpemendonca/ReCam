using Recam.Web.Api;

namespace Recam.Web.Pairing;

/// <summary>
/// "Add Monitor › In a browser": shows an invitation link, as a QR code and as text, that another
/// browser (an iPhone, another computer) opens to become a Monitor. Replaces it with a new one when
/// it expires; the page drives <see cref="Tick"/> every second.
/// </summary>
public sealed class InviteBrowserController(IRecamApi api, IClipboard clipboard)
{
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

    /// <summary>Counts down; an expired link is replaced by a new one.</summary>
    public async Task Tick(TimeSpan elapsed)
    {
        if (State != InviteState.Ready)
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
