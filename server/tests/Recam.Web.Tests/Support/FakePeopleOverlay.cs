using Microsoft.AspNetCore.Components;
using Recam.Web.Recordings;

namespace Recam.Web.Tests.Support;

public sealed class FakePeopleOverlay : IPeopleOverlay
{
    /// <summary>The track of every FollowAsync, in order.</summary>
    public List<PersonTrack> Followed { get; } = [];

    public int Stops { get; private set; }

    public int FullScreens { get; private set; }

    public Task FollowAsync(ElementReference video, ElementReference boxes, PersonTrack track)
    {
        Followed.Add(track);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        Stops++;
        return Task.CompletedTask;
    }

    public Task FullScreenAsync(ElementReference player)
    {
        FullScreens++;
        return Task.CompletedTask;
    }
}
