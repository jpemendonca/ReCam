namespace Recam.Server.Infrastructure.Realtime;

/// <summary>
/// What a hub method returns to its caller. The hub counterpart of problem details: expected
/// failures travel as data, not as exceptions.
/// </summary>
public sealed record HubResult(bool Ok, string? Code = null, string? Message = null)
{
    public static readonly HubResult Success = new(true);
}
