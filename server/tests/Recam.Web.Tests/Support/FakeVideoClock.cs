using Microsoft.AspNetCore.Components;
using Recam.Web.Recordings;

namespace Recam.Web.Tests.Support;

public sealed class FakeVideoClock : IVideoClock
{
    /// <summary>The file start of every FollowAsync, in order.</summary>
    public List<DateTime> Followed { get; } = [];

    public Task FollowAsync(ElementReference video, ElementReference label, DateTime fileStart)
    {
        Followed.Add(fileStart);
        return Task.CompletedTask;
    }
}
