using Microsoft.Extensions.Time.Testing;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Presence;
using Recam.Server.Infrastructure.Recordings;
using Recam.Server.Tests.Support;
using static Recam.Server.Tests.Support.ApiJson;

namespace Recam.Server.Tests.Infrastructure.Recordings;

public sealed class RecordingStateTrackerTests : IDisposable
{
    private readonly TemporaryDirectory _recordings = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 9, 26, 20, 0, 0, TimeSpan.Zero));
    private readonly DevicePresence _presence = new();

    public void Dispose() => _recordings.Dispose();

    [Fact(DisplayName = "A camera that should record starts as Starting, turns Stalled after the grace, and Recording when a file arrives")]
    public void StateOf_Over_Time_FollowsTheFiles()
    {
        // arrange
        var camera = Device.Pair(PairingToken.Issue(DeviceRole.Camera, _time.GetUtcNow()).Token, "Porch", AnyRole, _time.GetUtcNow()).Value.Device;
        _presence.Connect(camera.Id, isCamera: true);
        var store = new RecordingStore(new ServerSettings(_recordings.Path, [], null) { RecordingsDirectory = _recordings.Path });
        var tracker = new RecordingStateTracker(_presence, store, _time);

        // act
        var first = tracker.StateOf(camera);
        _time.Advance(RecordingStates.StartGrace + TimeSpan.FromSeconds(1));
        var later = tracker.StateOf(camera);
        var segment = RecordingFiles.Write(_recordings.Path, camera.Id, _time.GetUtcNow(), 1000);
        File.SetLastWriteTimeUtc(segment, _time.GetUtcNow().UtcDateTime);
        var arrived = tracker.StateOf(camera);

        // assert
        Assert.Equal(RecordingState.Starting, first);
        Assert.Equal(RecordingState.Stalled, later);
        Assert.Equal(RecordingState.Recording, arrived);
    }
}
