namespace Recam.Detect;

/// <summary>
/// A person the model found in one frame. Position and size are fractions of the frame, from its
/// top left corner, so they fit any size the video is shown at.
/// </summary>
public sealed record PersonBox(double Confidence, double X, double Y, double Width, double Height);
