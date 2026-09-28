namespace Recam.Server.Domain;

/// <summary>
/// A person the detect service found in one frame (SPECS.md 2.6). Position and size are fractions
/// of the frame, from its top left corner.
/// </summary>
public sealed record PersonBox(double Confidence, double X, double Y, double Width, double Height);
