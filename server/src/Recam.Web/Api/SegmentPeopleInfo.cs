namespace Recam.Web.Api;

/// <summary>The people the optional detect service found in one recording file, second by second.</summary>
public sealed record SegmentPeopleInfo(IReadOnlyList<PeopleSecondInfo> Seconds);

/// <param name="At">Seconds from the start of the file.</param>
/// <param name="People">Empty when nobody was there.</param>
public sealed record PeopleSecondInfo(double At, IReadOnlyList<PersonBoxInfo> People);

/// <summary>A box in fractions of the frame, from its top left corner.</summary>
public sealed record PersonBoxInfo(double X, double Y, double Width, double Height);
