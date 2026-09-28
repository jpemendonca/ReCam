namespace Recam.Detect.Tests;

public sealed class PeopleFileTests
{
    [Fact(DisplayName = "Each second is a line, with five numbers per person and none for nobody")]
    public void Format_PeopleAndEmptySeconds_WritesOneLinePerSecond()
    {
        // arrange
        (int, IReadOnlyList<PersonBox>)[] seconds =
        [
            (4, []),
            (3, [new PersonBox(0.91234, 0.25, 0.1, 0.125, 0.5), new PersonBox(0.5, 0.6, 0.2, 0.1, 0.3)]),
        ];

        // act
        var text = PeopleFile.Format(seconds);

        // assert
        Assert.Equal("3 0.9123 0.25 0.1 0.125 0.5 0.5 0.6 0.2 0.1 0.3\n4\n", text);
    }
}
