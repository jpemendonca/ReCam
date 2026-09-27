namespace Recam.Web.Join;

/// <summary>The link and claim of an invitation, read from the page address's fragment ("#l=...&amp;c=...").</summary>
public sealed record InviteLink(Guid Id, string Claim)
{
    public static InviteLink? Parse(string pageUri)
    {
        var hash = pageUri.IndexOf('#', StringComparison.Ordinal);
        if (hash < 0)
        {
            return null;
        }

        var parts = pageUri[(hash + 1)..]
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .GroupBy(pair => pair[0], StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First()[1], StringComparer.Ordinal);
        return parts.TryGetValue("l", out var id) && Guid.TryParseExact(id, "N", out var linkId)
            && parts.TryGetValue("c", out var claim) && claim.Length > 0
            ? new InviteLink(linkId, claim)
            : null;
    }
}
