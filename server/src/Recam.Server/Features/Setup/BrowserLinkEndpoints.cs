using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Hosting;
using Recam.Server.Infrastructure.Http;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Realtime;

namespace Recam.Server.Features.Setup;

/// <summary>
/// "Connect browser": a Monitor phone lets another browser in, or a Monitor invites one with a link
/// (SPECS.md 2.5 and 5.5).
/// </summary>
public static class BrowserLinkEndpoints
{
    public const string QrScheme = "recam://connect-browser";

    /// <summary>The web Monitor page that collects an invitation; the link and claim follow in the fragment.</summary>
    public const string InvitePath = "/connect";

    public static IEndpointRouteBuilder MapBrowserLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/browser-links", CreateAsync).RequireRateLimiting(SetupEndpoints.RateLimitPolicy);
        endpoints.MapPost("/api/browser-links/invite", InviteAsync).RequireAuthorization(AuthExtensions.ViewerOrOwner);
        endpoints.MapPost("/api/browser-links/{id:guid}/approve", ApproveAsync).RequireAuthorization(AuthExtensions.ViewerOrOwner);
        endpoints.MapPost("/api/browser-links/{id:guid}/claim", ClaimAsync);
        return endpoints;
    }

    private static async Task<Created<BrowserLinkResponse>> CreateAsync(
        IDbContextFactory<RecamDbContext> databaseFactory, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        await database.BrowserLinks.Where(link => link.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken);
        var issued = BrowserLink.Issue(now);
        database.BrowserLinks.Add(issued.Link);
        await database.SaveChangesAsync(cancellationToken);
        var qrUri = $"{QrScheme}?v=1&l={issued.Link.Id:N}&s={issued.Approval}";
        return TypedResults.Created(
            $"/api/browser-links/{issued.Link.Id}", new BrowserLinkResponse(issued.Link.Id, qrUri, issued.Claim, issued.Link.ExpiresAt));
    }

    /// <summary>
    /// The link goes to the first public URL when the server has one, otherwise to the address
    /// this Monitor uses: the browser's cookie belongs to the address that created it.
    /// </summary>
    private static async Task<IResult> InviteAsync(
        ClaimsPrincipal user,
        HttpRequest request,
        ServerSettings settings,
        IDbContextFactory<RecamDbContext> databaseFactory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var inviterId = user.GetDeviceId();
        var inviter = await database.Devices.SingleAsync(device => device.Id == inviterId, cancellationToken);
        var invited = BrowserLink.Invite(inviter, now);
        if (invited.IsFailure)
        {
            return invited.Error.ToHttpResult();
        }

        var link = invited.Value.Link;
        await database.BrowserLinks.Where(expired => expired.ExpiresAt <= now).ExecuteDeleteAsync(cancellationToken);
        database.BrowserLinks.Add(link);
        await database.SaveChangesAsync(cancellationToken);
        var address = settings.PublicUrls.Count > 0
            ? settings.PublicUrls[0].GetLeftPart(UriPartial.Authority)
            : $"{request.Scheme}://{request.Host}";
        var url = $"{address}{InvitePath}#l={link.Id:N}&c={invited.Value.Claim}";
        return TypedResults.Created($"/api/browser-links/{link.Id}", new BrowserInviteResponse(link.Id, url, link.ExpiresAt));
    }

    private static async Task<IResult> ApproveAsync(
        Guid id,
        ApproveBrowserLinkRequest request,
        ClaimsPrincipal user,
        IDbContextFactory<RecamDbContext> databaseFactory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var link = await database.BrowserLinks.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (link is null)
        {
            return BrowserLinkErrors.NotFound.ToHttpResult();
        }

        var approverId = user.GetDeviceId();
        var approver = await database.Devices.SingleAsync(device => device.Id == approverId, cancellationToken);
        var approved = link.Approve(request.Secret, approver, timeProvider.GetUtcNow());
        if (approved.IsFailure)
        {
            return approved.Error.ToHttpResult();
        }

        await database.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> ClaimAsync(
        Guid id,
        ClaimBrowserLinkRequest request,
        HttpContext context,
        IDbContextFactory<RecamDbContext> databaseFactory,
        TimeProvider timeProvider,
        IDeviceListChanges listChanges,
        CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var link = await database.BrowserLinks.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (link is null)
        {
            return BrowserLinkErrors.NotFound.ToHttpResult();
        }

        var now = timeProvider.GetUtcNow();
        var name = BrowserDeviceName.From(context.Request.Headers.UserAgent, context.Request.GetTypedHeaders().AcceptLanguage);
        var claimed = link.Claim(request.Claim, name, now);
        if (claimed.IsFailure)
        {
            return claimed.Error.ToHttpResult();
        }

        database.Devices.Add(claimed.Value.Device);
        await database.SaveChangesAsync(cancellationToken);
        await listChanges.DevicesChangedAsync();
        DeviceCookie.Append(context.Response, claimed.Value.Credential, request.Remember, now);
        return TypedResults.NoContent();
    }
}
