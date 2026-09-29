using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class PeopleInMotionTests
{
    private static readonly DateTimeOffset Noon = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "A confident person during the event marks it as having a person")]
    public void HasPerson_ConfidentPersonInside_ReturnsTrue()
    {
        // arrange
        var motion = new MotionEvent(Noon.AddSeconds(10), Noon.AddSeconds(15), 0.05);
        SegmentPeople[] segments = [new(Noon, [Sample(12, 0.4), Sample(13, 0.8)])];

        // act
        var person = PeopleInMotion.HasPerson(motion, segments);

        // assert
        Assert.True(person);
    }

    [Fact(DisplayName = "An analyzed event with only weak boxes, or none, has no person")]
    public void HasPerson_OnlyWeakBoxes_ReturnsFalse()
    {
        // arrange
        var motion = new MotionEvent(Noon.AddSeconds(10), Noon.AddSeconds(15), 0.05);
        SegmentPeople[] segments = [new(Noon, [Sample(11, 0.49), new PeopleSample(Noon.AddSeconds(12), [])])];

        // act
        var person = PeopleInMotion.HasPerson(motion, segments);

        // assert
        Assert.False(person);
    }

    [Fact(DisplayName = "An event in a segment the detect service did not analyze stays unknown")]
    public void HasPerson_NotAnalyzed_ReturnsNull()
    {
        // arrange
        var motion = new MotionEvent(Noon.AddSeconds(10), Noon.AddSeconds(15), 0.05);
        SegmentPeople[] segments = [new(Noon, null)];

        // act
        var person = PeopleInMotion.HasPerson(motion, segments);

        // assert
        Assert.Null(person);
    }

    [Fact(DisplayName = "A person a second outside the event still belongs to it, two seconds out does not")]
    public void HasPerson_PersonAtEdge_UsesMargin()
    {
        // arrange
        var motion = new MotionEvent(Noon.AddSeconds(10), Noon.AddSeconds(15), 0.05);
        SegmentPeople[] justOutside = [new(Noon, [Sample(16, 0.9)])];
        SegmentPeople[] farOutside = [new(Noon, [Sample(17, 0.9)])];

        // act
        var near = PeopleInMotion.HasPerson(motion, justOutside);
        var far = PeopleInMotion.HasPerson(motion, farOutside);

        // assert
        Assert.True(near);
        Assert.False(far);
    }

    [Fact(DisplayName = "An event across two segments is unknown until both were analyzed")]
    public void HasPerson_AcrossSegments_NeedsBoth()
    {
        // arrange
        var motion = new MotionEvent(Noon.AddSeconds(55), Noon.AddSeconds(65), 0.05);
        SegmentPeople[] half = [new(Noon, [Sample(56, 0.1)]), new(Noon.AddSeconds(60), null)];
        SegmentPeople[] both = [new(Noon, [Sample(56, 0.1)]), new(Noon.AddSeconds(60), [])];
        SegmentPeople[] personInSecond = [new(Noon, [Sample(56, 0.1)]), new(Noon.AddSeconds(60), [Sample(64, 0.7)])];

        // act
        var unknown = PeopleInMotion.HasPerson(motion, half);
        var nobody = PeopleInMotion.HasPerson(motion, both);
        var somebody = PeopleInMotion.HasPerson(motion, personInSecond);

        // assert
        Assert.Null(unknown);
        Assert.False(nobody);
        Assert.True(somebody);
    }

    private static PeopleSample Sample(int second, double confidence) =>
        new(Noon.AddSeconds(second), [new PersonBox(confidence, 0.1, 0.2, 0.3, 0.4)]);
}
