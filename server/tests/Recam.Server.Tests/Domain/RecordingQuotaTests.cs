using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class RecordingQuotaTests
{
    private const long Megabyte = RecordingQuota.BytesPerMegabyte;
    private static readonly DateTimeOffset Start = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Porch = Guid.NewGuid();
    private static readonly Guid Garage = Guid.NewGuid();

    private static RecordingSegment Segment(Guid camera, int minute, long megabytes) =>
        new(camera, $"{minute}.mp4", Start.AddMinutes(minute), megabytes * Megabyte);

    private static RecordingQuota QuotaOf(int megabytes)
    {
        var quota = RecordingQuota.CreateDefault();
        quota.ChangeTo(megabytes, 0, long.MaxValue / 2);
        return quota;
    }

    [Fact(DisplayName = "Recordings within the quota are all kept")]
    public void PlanCleanup_WithinQuota_DeletesNothing()
    {
        // arrange
        var quota = QuotaOf(100);
        RecordingSegment[] segments = [Segment(Porch, 0, 40), Segment(Porch, 1, 40)];

        // act
        var toDelete = quota.PlanCleanup(segments, new HashSet<Guid> { Porch });

        // assert
        Assert.Empty(toDelete);
    }

    [Fact(DisplayName = "Over the quota, the oldest segments go first, from any camera")]
    public void PlanCleanup_OverQuota_DeletesOldestAcrossCameras()
    {
        // arrange
        var quota = QuotaOf(100);
        RecordingSegment[] segments =
        [
            Segment(Porch, 0, 30), Segment(Garage, 1, 30), Segment(Porch, 2, 30),
            Segment(Garage, 3, 30), Segment(Porch, 4, 30),
        ];

        // act
        var toDelete = quota.PlanCleanup(segments, new HashSet<Guid> { Porch, Garage });

        // assert
        Assert.Equal([segments[0], segments[1]], toDelete);
    }

    [Fact(DisplayName = "The newest segment of each camera stays, because it may still be written")]
    public void PlanCleanup_EvenFarOverQuota_KeepsNewestPerCamera()
    {
        // arrange
        var quota = QuotaOf(100);
        RecordingSegment[] segments = [Segment(Porch, 0, 500), Segment(Porch, 1, 500), Segment(Garage, 2, 500)];

        // act
        var toDelete = quota.PlanCleanup(segments, new HashSet<Guid> { Porch, Garage });

        // assert
        Assert.Equal([segments[0]], toDelete);
    }

    [Fact(DisplayName = "Everything a removed camera left is deleted, newest included")]
    public void PlanCleanup_FromRemovedCamera_DeletesAll()
    {
        // arrange
        var quota = QuotaOf(2048);
        RecordingSegment[] segments = [Segment(Garage, 0, 1), Segment(Garage, 1, 1), Segment(Porch, 2, 1)];

        // act
        var toDelete = quota.PlanCleanup(segments, new HashSet<Guid> { Porch });

        // assert
        Assert.Equal([segments[0], segments[1]], toDelete);
    }

    [Fact(DisplayName = "The quota cannot be larger than what is used plus the free disk")]
    public void ChangeTo_MoreThanUsedPlusFree_ReturnsQuotaTooLarge()
    {
        // arrange
        var quota = RecordingQuota.CreateDefault();

        // act
        var result = quota.ChangeTo(1000, usedBytes: 200 * Megabyte, freeBytes: 700 * Megabyte);

        // assert
        Assert.Equal(RecordingErrors.QuotaTooLarge, result.Error);
        Assert.Equal(RecordingQuota.DefaultMegabytes, quota.Megabytes);
    }

    [Fact(DisplayName = "The quota can take all the used plus free space")]
    public void ChangeTo_ExactlyUsedPlusFree_Succeeds()
    {
        // arrange
        var quota = RecordingQuota.CreateDefault();

        // act
        var result = quota.ChangeTo(900, usedBytes: 200 * Megabyte, freeBytes: 700 * Megabyte);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(900, quota.Megabytes);
    }
}
