namespace Recam.Web.Realtime;

/// <summary>What a hub method answers (the server's HubResult).</summary>
public sealed record HubCallResult(bool Ok, string? Code, string? Message)
{
    public static readonly HubCallResult Success = new(true, null, null);
}
