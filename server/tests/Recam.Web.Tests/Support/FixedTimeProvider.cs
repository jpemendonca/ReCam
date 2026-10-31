namespace Recam.Web.Tests.Support;

/// <summary>A clock that says what the test sets; timers still run on real time.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
