using Recam.Web.Api;

namespace Recam.Web.Join;

/// <summary>
/// The page an invitation link opens: shows what it does, and on the person's go collects this
/// browser's credential, which makes it a Monitor.
/// </summary>
public sealed class JoinController(IRecamApi api)
{
    private InviteLink? _invite;

    public JoinState State { get; private set; } = JoinState.Loading;

    public bool Remember { get; set; } = true;

    public bool Busy { get; private set; }

    /// <summary>The server did not answer the last try; the page offers to try again.</summary>
    public bool Failed { get; private set; }

    public event Action? Changed;

    public async Task LoadAsync(string pageUri)
    {
        _invite = InviteLink.Parse(pageUri);
        if (_invite is null)
        {
            SetState(JoinState.Invalid);
            return;
        }

        await RunAsync(async () =>
            State = await api.GetMeAsync(CancellationToken.None) is null ? JoinState.Ready : JoinState.AlreadyMonitor);
    }

    public async Task JoinAsync()
    {
        if (_invite is not { } invite || State != JoinState.Ready || Busy)
        {
            return;
        }

        await RunAsync(async () =>
            State = await api.ClaimBrowserLinkAsync(invite.Id, invite.Claim, Remember, CancellationToken.None) == ClaimOutcome.Claimed
                ? JoinState.Joined
                : JoinState.Gone);
    }

    private async Task RunAsync(Func<Task> action)
    {
        Busy = true;
        Failed = false;
        Changed?.Invoke();
        try
        {
            await action();
        }
        catch (HttpRequestException)
        {
            Failed = true;
            if (State == JoinState.Loading)
            {
                State = JoinState.Ready;
            }
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }

    private void SetState(JoinState state)
    {
        State = state;
        Changed?.Invoke();
    }
}
