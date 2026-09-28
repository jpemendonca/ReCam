namespace Recam.Detect.Tests;

public sealed class YoloxDecoderTests
{
    private const int Stride8Cells = YoloxDecoder.InputSize / 8;

    [Fact(DisplayName = "A confident person becomes a box in fractions of the frame")]
    public void Decode_ConfidentPerson_ReturnsRelativeBox()
    {
        // arrange
        var output = Empty();
        // Cell (10, 5) of the stride-8 grid: center at (84, 44), 64x128 px, in a 416x312 frame.
        SetCandidate(output, row: (5 * Stride8Cells) + 10, offsetX: 0.5f, offsetY: 0.5f, width: 64, height: 128, objectness: 0.9f, person: 0.9f);

        // act
        var boxes = YoloxDecoder.Decode(output, scaledWidth: 416, scaledHeight: 312);

        // assert
        var box = Assert.Single(boxes);
        Assert.Equal(0.81, box.Confidence, 3);
        Assert.Equal((84 - 32) / 416.0, box.X, 3);
        Assert.Equal(0, box.Y, 3);
        Assert.Equal(64 / 416.0, box.Width, 3);
        Assert.Equal((44 + 64) / 312.0, box.Height, 3);
    }

    [Fact(DisplayName = "Other classes and weak candidates are left out")]
    public void Decode_CarAndWeakPerson_ReturnsNothing()
    {
        // arrange
        var output = Empty();
        SetCandidate(output, row: 3, offsetX: 0, offsetY: 0, width: 40, height: 40, objectness: 0.9f, person: 0.01f, car: 0.95f);
        SetCandidate(output, row: 7, offsetX: 0, offsetY: 0, width: 40, height: 40, objectness: 0.5f, person: 0.5f);

        // act
        var boxes = YoloxDecoder.Decode(output, 416, 416);

        // assert
        Assert.Empty(boxes);
    }

    [Fact(DisplayName = "Two candidates for the same person become one box, the stronger one")]
    public void Decode_OverlappingCandidates_KeepsStrongest()
    {
        // arrange
        var output = Empty();
        var row = (20 * Stride8Cells) + 20;
        SetCandidate(output, row, offsetX: 0, offsetY: 0, width: 80, height: 160, objectness: 0.8f, person: 0.8f);
        SetCandidate(output, row + 1, offsetX: 0, offsetY: 0, width: 80, height: 160, objectness: 0.95f, person: 0.95f);
        SetCandidate(output, row + 30, offsetX: 0, offsetY: 0, width: 80, height: 160, objectness: 0.9f, person: 0.9f);

        // act
        var boxes = YoloxDecoder.Decode(output, 416, 416);

        // assert
        Assert.Equal(2, boxes.Count);
        Assert.Equal(0.9025, boxes[0].Confidence, 3);
        Assert.Equal(0.81, boxes[1].Confidence, 3);
    }

    [Fact(DisplayName = "A box spilling into the padding is cut at the frame's edge")]
    public void Decode_BoxIntoPadding_ClampsToFrame()
    {
        // arrange
        var output = Empty();
        // Last stride-8 row: center y near 412, far below a 416x234 frame (16:9).
        SetCandidate(output, row: (51 * Stride8Cells) + 25, offsetX: 0, offsetY: 0, width: 40, height: 400, objectness: 0.9f, person: 0.9f);

        // act
        var box = Assert.Single(YoloxDecoder.Decode(output, 416, 234));

        // assert
        Assert.Equal(1, box.Y + box.Height, 3);
    }

    private static float[] Empty() => new float[YoloxDecoder.CandidateCount * YoloxDecoder.ValuesPerCandidate];

    private static void SetCandidate(
        float[] output, int row, float offsetX, float offsetY, float width, float height, float objectness, float person, float car = 0)
    {
        var start = row * YoloxDecoder.ValuesPerCandidate;
        output[start] = offsetX;
        output[start + 1] = offsetY;
        output[start + 2] = MathF.Log(width / 8);
        output[start + 3] = MathF.Log(height / 8);
        output[start + 4] = objectness;
        output[start + 5] = person;
        output[start + 7] = car;
    }
}
