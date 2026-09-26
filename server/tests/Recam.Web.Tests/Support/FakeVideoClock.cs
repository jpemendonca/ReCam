using Microsoft.AspNetCore.Components;
using Recam.Web.Recordings;

namespace Recam.Web.Tests.Support;

public sealed class FakeVideoClock : IVideoClock
{
    public event Action<DateTime>? HeadLeftWindow;

    /// <summary>The file start of every FollowAsync, in order.</summary>
    public List<DateTime> Followed { get; } = [];

    /// <summary>Every window the bar reported, in order.</summary>
    public List<TimelineWindow> Windows { get; } = [];

    public int Stops { get; private set; }

    public Task FollowAsync(ElementReference video, ElementReference clock, ElementReference line, DateTime fileStart)
    {
        Followed.Add(fileStart);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        Stops++;
        return Task.CompletedTask;
    }

    public Task ShowWindowAsync(TimelineWindow window, double width)
    {
        Windows.Add(window);
        return Task.CompletedTask;
    }

    /// <summary>Like the script when the video runs past the window's edge.</summary>
    public void RunPast(DateTime head) => HeadLeftWindow?.Invoke(head);
}
