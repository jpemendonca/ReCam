namespace Recam.Detect.Tests;

public sealed class MotionSecondsTests
{
    [Fact(DisplayName = "Only seconds where enough of the picture changed are looked at")]
    public void Parse_MixedScores_KeepsMovingSeconds()
    {
        // arrange
        string[] lines = ["0.5 0.0000", "1.0 0.0030", "1.5 0.0500", "2.0 0.0010", "7.5 0.2000", "broken line", ""];

        // act
        var seconds = MotionSeconds.Parse(lines);

        // assert
        Assert.Equal([1, 7], seconds);
    }
}
