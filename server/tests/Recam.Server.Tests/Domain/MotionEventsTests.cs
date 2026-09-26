using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class MotionEventsTests
{
    private static readonly DateTimeOffset Start = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private static MotionSample At(double seconds, double changed) => new(Start.AddSeconds(seconds), changed);

    [Theory(DisplayName = "The sensitivity decides how much of the picture must change")]
    [InlineData(MotionSensitivity.High, 1)]
    [InlineData(MotionSensitivity.Medium, 1)]
    [InlineData(MotionSensitivity.Low, 0)]
    public void Find_SmallChange_DependsOnSensitivity(MotionSensitivity sensitivity, int expected)
    {
        // arrange
        MotionSample[] samples = [At(1, 0.02), At(1.5, 0.0), At(2, 0.001)];

        // act
        var events = MotionEvents.Find(samples, sensitivity, []);

        // assert
        Assert.Equal(expected, events.Count);
    }

    [Fact(DisplayName = "Motion less than ten seconds apart is one event, with its peak; more apart is two")]
    public void Find_Gaps_MergeOrSplit()
    {
        // arrange
        MotionSample[] samples = [At(0, 0.05), At(8, 0.2), At(12, 0.03), At(30, 0.04)];

        // act
        var events = MotionEvents.Find(samples, MotionSensitivity.Medium, []);

        // assert
        Assert.Equal(2, events.Count);
        Assert.Equal(new MotionEvent(Start, Start.AddSeconds(12), 0.2), events[0]);
        Assert.Equal(Start.AddSeconds(30), events[1].Start);
    }

    [Fact(DisplayName = "The seconds after the flashlight switches are not motion")]
    public void Find_RightAfterTorch_Ignored()
    {
        // arrange
        MotionSample[] samples = [At(10, 0.6), At(10.5, 0.4), At(40, 0.05)];

        // act
        var events = MotionEvents.Find(samples, MotionSensitivity.Medium, [Start.AddSeconds(9.5)]);

        // assert
        Assert.Equal(Start.AddSeconds(40), Assert.Single(events).Start);
    }
}
