namespace Recam.Detect;

/// <summary>
/// Turns the raw output of the YOLOX model into person boxes. The model sees a square picture:
/// the frame scaled down to fit, in its top left corner, with gray padding on the rest.
/// </summary>
public static class YoloxDecoder
{
    public const int InputSize = 416;

    /// <summary>Box, objectness, then one score per COCO class; class 0 is person.</summary>
    public const int ValuesPerCandidate = 85;

    /// <summary>Kept in the file; the server decides what counts as a person (SPECS.md 2.6).</summary>
    public const double MinConfidence = 0.3;

    public const double OverlapLimit = 0.45;

    private static readonly int[] Strides = [8, 16, 32];

    /// <param name="output">The model's output, one row of <see cref="ValuesPerCandidate"/> per candidate.</param>
    /// <param name="scaledWidth">Width of the frame inside the square picture, in pixels.</param>
    /// <param name="scaledHeight">Height of the frame inside the square picture, in pixels.</param>
    public static IReadOnlyList<PersonBox> Decode(ReadOnlySpan<float> output, int scaledWidth, int scaledHeight)
    {
        var candidates = new List<PersonBox>();
        var row = 0;
        foreach (var stride in Strides)
        {
            var cells = InputSize / stride;
            for (var gridY = 0; gridY < cells; gridY++)
            {
                for (var gridX = 0; gridX < cells; gridX++, row++)
                {
                    var values = output.Slice(row * ValuesPerCandidate, ValuesPerCandidate);
                    var confidence = values[4] * values[5];
                    if (confidence < MinConfidence)
                    {
                        continue;
                    }

                    var centerX = (values[0] + gridX) * stride;
                    var centerY = (values[1] + gridY) * stride;
                    var width = MathF.Exp(values[2]) * stride;
                    var height = MathF.Exp(values[3]) * stride;
                    candidates.Add(Relative(confidence, centerX - (width / 2), centerY - (height / 2), width, height, scaledWidth, scaledHeight));
                }
            }
        }

        return SuppressOverlaps(candidates);
    }

    /// <summary>How many candidate rows the model gives for its input size.</summary>
    public static int CandidateCount => Strides.Sum(stride => (InputSize / stride) * (InputSize / stride));

    private static PersonBox Relative(double confidence, double left, double top, double width, double height, int scaledWidth, int scaledHeight)
    {
        var x = Math.Clamp(left / scaledWidth, 0, 1);
        var y = Math.Clamp(top / scaledHeight, 0, 1);
        var right = Math.Clamp((left + width) / scaledWidth, 0, 1);
        var bottom = Math.Clamp((top + height) / scaledHeight, 0, 1);
        return new PersonBox(confidence, x, y, right - x, bottom - y);
    }

    private static List<PersonBox> SuppressOverlaps(List<PersonBox> candidates)
    {
        var kept = new List<PersonBox>();
        foreach (var candidate in candidates.OrderByDescending(box => box.Confidence))
        {
            if (candidate.Width > 0 && candidate.Height > 0 && kept.All(box => Overlap(box, candidate) <= OverlapLimit))
            {
                kept.Add(candidate);
            }
        }

        return kept;
    }

    private static double Overlap(PersonBox a, PersonBox b)
    {
        var width = Math.Min(a.X + a.Width, b.X + b.Width) - Math.Max(a.X, b.X);
        var height = Math.Min(a.Y + a.Height, b.Y + b.Height) - Math.Max(a.Y, b.Y);
        if (width <= 0 || height <= 0)
        {
            return 0;
        }

        var shared = width * height;
        return shared / ((a.Width * a.Height) + (b.Width * b.Height) - shared);
    }
}
