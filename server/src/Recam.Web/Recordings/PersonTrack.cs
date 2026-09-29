using Microsoft.JSInterop;
using Recam.Web.Api;

namespace Recam.Web.Recordings;

/// <summary>
/// Where the people are in the file playing, at any moment (SPECS.md 2.6). The detect service
/// looks once a second, so a box moves from one second's place to the next one's instead of
/// jumping; a box with nobody near it the next second stays put and then goes away.
/// </summary>
public sealed class PersonTrack(SegmentPeopleInfo people)
{
    /// <summary>How far, in fractions of the frame, a person may move in a second and still be the same one.</summary>
    public const double SamePersonDistance = 0.25;

    private readonly Dictionary<double, IReadOnlyList<PersonBoxInfo>> _bySecond =
        people.Seconds.GroupBy(second => second.At).ToDictionary(group => group.Key, group => group.First().People);

    /// <summary>The boxes at <paramref name="seconds"/> into the file.</summary>
    public IReadOnlyList<PersonBoxInfo> At(double seconds)
    {
        var second = Math.Floor(seconds);
        if (!_bySecond.TryGetValue(second, out var now))
        {
            return [];
        }

        var next = _bySecond.GetValueOrDefault(second + 1) ?? [];
        var fraction = seconds - second;
        var taken = new HashSet<PersonBoxInfo>();
        var boxes = new List<PersonBoxInfo>();
        foreach (var box in now)
        {
            var match = next
                .Where(candidate => !taken.Contains(candidate) && Distance(box, candidate) <= SamePersonDistance)
                .OrderBy(candidate => Distance(box, candidate))
                .FirstOrDefault();
            if (match is null)
            {
                boxes.Add(box);
                continue;
            }

            taken.Add(match);
            boxes.Add(new PersonBoxInfo(
                Lerp(box.X, match.X, fraction),
                Lerp(box.Y, match.Y, fraction),
                Lerp(box.Width, match.Width, fraction),
                Lerp(box.Height, match.Height, fraction)));
        }

        return boxes;
    }

    /// <summary>For wwwroot/js/people.js: the boxes as x, y, width, height, one after the other.</summary>
    [JSInvokable]
    public double[] Flat(double seconds) =>
        [.. At(seconds).SelectMany(box => (double[])[box.X, box.Y, box.Width, box.Height])];

    private static double Lerp(double from, double to, double fraction) => from + ((to - from) * fraction);

    private static double Distance(PersonBoxInfo a, PersonBoxInfo b)
    {
        var x = (a.X + (a.Width / 2)) - (b.X + (b.Width / 2));
        var y = (a.Y + (a.Height / 2)) - (b.Y + (b.Height / 2));
        return Math.Sqrt((x * x) + (y * y));
    }
}
