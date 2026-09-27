using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class RecordingStatesTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 20, 0, 0, TimeSpan.Zero);
    private const long PlentyFree = 10L * 1024 * 1024 * 1024;

    public static TheoryData<string, bool, bool, bool, long, int?, int, RecordingState> Cases => new()
    {
        // name, enabled, canRecord, online, free bytes, seconds since last write, seconds waiting, expected
        { "no H.264 wins over everything", true, false, true, PlentyFree, 1, 0, RecordingState.NeedsH264 },
        { "switched off", false, true, true, PlentyFree, 1, 0, RecordingState.Off },
        { "camera away", true, true, false, PlentyFree, null, 0, RecordingState.Offline },
        { "a file written seconds ago", true, true, true, PlentyFree, 2, 0, RecordingState.Recording },
        { "waiting for the first file", true, true, true, PlentyFree, null, 10, RecordingState.Starting },
        { "waited too long", true, true, true, PlentyFree, null, 31, RecordingState.Stalled },
        { "files stopped long ago", true, true, true, PlentyFree, 120, 31, RecordingState.Stalled },
        { "no files and a full disk", true, true, true, 1024, null, 0, RecordingState.NoSpace },
    };

    [Theory(DisplayName = "The recording state follows what reaches the disk, not the switch alone")]
    [MemberData(nameof(Cases))]
    public void Of_Situation_GivesState(
        string situation, bool enabled, bool canRecord, bool online, long freeBytes, int? writtenSecondsAgo, int waitingSeconds, RecordingState expected)
    {
        // act
        var state = RecordingStates.Of(
            enabled, canRecord, online, freeBytes,
            writtenSecondsAgo is { } ago ? Now.AddSeconds(-ago) : null,
            Now.AddSeconds(-waitingSeconds),
            Now);

        // assert
        Assert.True(expected == state, situation);
    }
}
