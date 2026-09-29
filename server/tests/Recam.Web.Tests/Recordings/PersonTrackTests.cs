using Recam.Web.Api;
using Recam.Web.Recordings;

namespace Recam.Web.Tests.Recordings;

public sealed class PersonTrackTests
{
    [Fact(DisplayName = "Between two seconds, a box is on its way from one place to the next")]
    public void At_BetweenSeconds_Interpolates()
    {
        // arrange
        var track = new PersonTrack(new SegmentPeopleInfo(
        [
            new PeopleSecondInfo(4, [new PersonBoxInfo(0.1, 0.2, 0.2, 0.4)]),
            new PeopleSecondInfo(5, [new PersonBoxInfo(0.3, 0.2, 0.2, 0.6)]),
        ]));

        // act
        var box = Assert.Single(track.At(4.5));

        // assert
        Assert.Equal(0.2, box.X, 6);
        Assert.Equal(0.2, box.Y, 6);
        Assert.Equal(0.5, box.Height, 6);
    }

    [Fact(DisplayName = "A box with nobody the next second stays put, then goes away")]
    public void At_NobodyNext_StaysThenGoes()
    {
        // arrange
        var track = new PersonTrack(new SegmentPeopleInfo(
        [
            new PeopleSecondInfo(4, [new PersonBoxInfo(0.1, 0.2, 0.2, 0.4)]),
            new PeopleSecondInfo(5, []),
        ]));

        // act
        var during = track.At(4.9);
        var after = track.At(5.2);
        var unlooked = track.At(9);

        // assert
        Assert.Equal(new PersonBoxInfo(0.1, 0.2, 0.2, 0.4), Assert.Single(during));
        Assert.Empty(after);
        Assert.Empty(unlooked);
    }

    [Fact(DisplayName = "Two people each go to the nearest box of the next second")]
    public void At_TwoPeople_EachFollowsTheNearest()
    {
        // arrange
        var track = new PersonTrack(new SegmentPeopleInfo(
        [
            new PeopleSecondInfo(0, [new PersonBoxInfo(0.1, 0.1, 0.1, 0.1), new PersonBoxInfo(0.7, 0.1, 0.1, 0.1)]),
            new PeopleSecondInfo(1, [new PersonBoxInfo(0.8, 0.1, 0.1, 0.1), new PersonBoxInfo(0.2, 0.1, 0.1, 0.1)]),
        ]));

        // act
        var boxes = track.At(0.5);

        // assert
        Assert.Equal([0.15, 0.75], boxes.Select(box => Math.Round(box.X, 6)));
    }

    [Fact(DisplayName = "The script gets the boxes as plain numbers")]
    public void Flat_Boxes_AreFourNumbersEach()
    {
        // arrange
        var track = new PersonTrack(new SegmentPeopleInfo([new PeopleSecondInfo(2, [new PersonBoxInfo(0.1, 0.2, 0.3, 0.4)])]));

        // act
        var flat = track.Flat(2.1);

        // assert
        Assert.Equal([0.1, 0.2, 0.3, 0.4], flat);
    }
}
