using Recam.Web.Api;

namespace Recam.Web.Start;

/// <summary>
/// The start page: finds out whether this browser is a Monitor and, while the server has none,
/// makes it one with the first-time code from the server log.
/// </summary>
public sealed class StartController(IRecamApi api)
{
    public StartState State { get; private set; } = StartState.Loading;

    public MeInfo? Me { get; private set; }

    /// <summary>Why the last code was refused; null after a success or before any try.</summary>
    public FirstOpenOutcome? CodeError { get; private set; }

    public bool Busy { get; private set; }

    public event Action? Changed;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        await RunAsync(async () =>
        {
            Me = await api.GetMeAsync(cancellationToken);
            State = Me is not null ? StartState.Monitor
                : await api.IsFirstOpenAsync(cancellationToken) ? StartState.NeedsCode
                : StartState.ServerTaken;
        });
    }

    public async Task SubmitCodeAsync(string code, bool remember, CancellationToken cancellationToken)
    {
        if (Busy || State != StartState.NeedsCode)
        {
            return;
        }

        await RunAsync(async () =>
        {
            var outcome = await api.FirstOpenAsync(code, remember, cancellationToken);
            CodeError = outcome == FirstOpenOutcome.Opened ? null : outcome;
            if (outcome == FirstOpenOutcome.Opened)
            {
                Me = await api.GetMeAsync(cancellationToken);
                State = StartState.Monitor;
            }
            else if (outcome == FirstOpenOutcome.AlreadyHasMonitor)
            {
                State = StartState.ServerTaken;
            }
        });
    }

    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        await RunAsync(async () =>
        {
            await api.SignOutAsync(cancellationToken);
            Me = null;
            State = await api.IsFirstOpenAsync(cancellationToken) ? StartState.NeedsCode : StartState.ServerTaken;
        });
    }

    private async Task RunAsync(Func<Task> action)
    {
        Busy = true;
        Changed?.Invoke();
        try
        {
            await action();
        }
        catch (HttpRequestException)
        {
            State = StartState.Offline;
        }
        finally
        {
            Busy = false;
            Changed?.Invoke();
        }
    }
}
