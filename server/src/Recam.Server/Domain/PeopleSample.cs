namespace Recam.Server.Domain;

/// <summary>One second the detect service looked at, with every person it found (maybe none).</summary>
public sealed record PeopleSample(DateTimeOffset At, IReadOnlyList<PersonBox> People);
