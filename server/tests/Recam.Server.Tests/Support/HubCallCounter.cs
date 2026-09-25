using Microsoft.AspNetCore.SignalR.Client;

namespace Recam.Server.Tests.Support;

/// <summary>Counts calls the server makes to one parameterless client method.</summary>
public sealed class HubCallCounter : IDisposable
{
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

    private readonly SemaphoreSlim _signal = new(0);
    private int _count;

    public HubCallCounter(HubConnection connection, string method) =>
        connection.On(method, () =>
        {
            Interlocked.Increment(ref _count);
            _signal.Release();
        });

    public int Count => Volatile.Read(ref _count);

    public async Task WaitForCallAsync()
    {
        using var timeout = new CancellationTokenSource(Wait);
        await _signal.WaitAsync(timeout.Token);
    }

    /// <summary>True when no call arrives within a short real-time window.</summary>
    public async Task<bool> StaysSilentAsync() => !await _signal.WaitAsync(TimeSpan.FromMilliseconds(500));

    public void Dispose() => _signal.Dispose();
}
