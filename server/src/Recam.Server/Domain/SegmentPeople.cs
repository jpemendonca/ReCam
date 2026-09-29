namespace Recam.Server.Domain;

/// <summary>
/// What the detect service wrote for one segment. <see cref="Samples"/> is null while the segment
/// was not analyzed: the service is not installed, it has not got to it yet, or it came too late.
/// </summary>
public sealed record SegmentPeople(DateTimeOffset StartsAt, IReadOnlyList<PeopleSample>? Samples);
