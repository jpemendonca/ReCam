using Microsoft.AspNetCore.Components;
using Recam.Web.Recordings;

namespace Recam.Web.Tests.Support;

public sealed class FakeVideoClock : IVideoClock
{
    public event Action<DateTime>? HeadMoved;

    /// <summary>The file start of every FollowAsync, in order.</summary>
    public List<DateTime> Followed { get; } = [];

    public int Stops { get; private set; }

    public Task FollowAsync(ElementReference video, ElementReference clock, DateTime fileStart)
    {
        Followed.Add(fileStart);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        Stops++;
        return Task.CompletedTask;
    }

    /// <summary>Like the script when the video reaches another second.</summary>
    public void PlayTo(DateTime head) => HeadMoved?.Invoke(head);
}
