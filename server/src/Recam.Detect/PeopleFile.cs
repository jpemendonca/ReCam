using System.Globalization;
using System.Text;

namespace Recam.Detect;

/// <summary>
/// The <c>.people</c> file written next to a recording segment (SPECS.md 2.6): one line per
/// second looked at, "seconds" followed by five numbers per person, "confidence x y width height".
/// A second with nobody is just the seconds. An empty file means the segment had nothing to look at.
/// </summary>
public static class PeopleFile
{
    public const string Suffix = ".people";

    public static string Format(IEnumerable<(int Second, IReadOnlyList<PersonBox> People)> seconds)
    {
        var text = new StringBuilder();
        foreach (var (second, people) in seconds.OrderBy(entry => entry.Second))
        {
            text.Append(second.ToString(CultureInfo.InvariantCulture));
            foreach (var person in people)
            {
                foreach (var value in (double[])[person.Confidence, person.X, person.Y, person.Width, person.Height])
                {
                    text.Append(' ').Append(value.ToString("0.####", CultureInfo.InvariantCulture));
                }
            }

            text.Append('\n');
        }

        return text.ToString();
    }
}
