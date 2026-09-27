using System.Collections.Concurrent;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Presence;

namespace Recam.Server.Infrastructure.Recordings;

/// <summary>
/// Works out each camera's <see cref="RecordingState"/> from presence and the recordings folder,
/// and remembers since when a camera waits for its first file. In memory, like presence.
/// </summary>
public sealed class RecordingStateTracker(DevicePresence presence, RecordingStore store, TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _waitingSince = new();
    private readonly ConcurrentDictionary<Guid, RecordingState> _told = new();

    public RecordingState StateOf(Device camera)
    {
        var now = timeProvider.GetUtcNow();
        var online = presence.IsOnline(camera.Id);
        var shouldRecord = camera.RecordingEnabled && camera.CanRecord && online;
        var lastWrite = shouldRecord ? store.LastWrite(camera.Id) : null;
        var arriving = lastWrite is { } written && now - written < RecordingStates.FreshFor;
        if (!shouldRecord || arriving)
        {
            _waitingSince.TryRemove(camera.Id, out _);
        }

        var waitingSince = shouldRecord && !arriving ? _waitingSince.GetOrAdd(camera.Id, now) : now;
        return RecordingStates.Of(
            camera.RecordingEnabled, camera.CanRecord, online, shouldRecord ? store.FreeBytes() : long.MaxValue, lastWrite, waitingSince, now);
    }

    public CameraStatus StatusOf(Device camera) => camera.ToCameraStatus(
        presence.IsOnline(camera.Id), presence.IsPublishing(camera.Id), presence.IsTorchOn(camera.Id), StateOf(camera));

    /// <summary>True the first time a camera is seen in <paramref name="state"/>, so the change is told once.</summary>
    public bool IsNews(Guid cameraId, RecordingState state)
    {
        var previous = _told.GetValueOrDefault(cameraId, (RecordingState)(-1));
        _told[cameraId] = state;
        return previous != state;
    }
}
