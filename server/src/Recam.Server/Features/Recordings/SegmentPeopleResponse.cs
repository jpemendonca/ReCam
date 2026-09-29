namespace Recam.Server.Features.Recordings;

/// <summary>The people in one recording file, second by second (SPECS.md 2.6).</summary>
public sealed record SegmentPeopleResponse(IReadOnlyList<PeopleSecondResponse> Seconds);

/// <param name="At">Seconds from the start of the file.</param>
/// <param name="People">Empty when nobody was there.</param>
public sealed record PeopleSecondResponse(double At, IReadOnlyList<PersonBoxResponse> People);

/// <summary>A box in fractions of the frame, from its top left corner.</summary>
public sealed record PersonBoxResponse(double X, double Y, double Width, double Height);
