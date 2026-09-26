using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Http;
using Recam.Server.Infrastructure.Persistence;

namespace Recam.Server.Features.Setup;

/// <summary>"Connect browser": a Monitor phone lets another browser in (SPECS.md 2.5 and 5.5).</summary>
public static class BrowserLinkEndpoints
{
    public const string QrScheme = "recam://connect-browser";

    public static IEndpointRouteBuilder MapBrowserLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/browser-links", CreateAsync).RequireRateLimiting(SetupEndpoints.RateLimitPolicy);
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
        DeviceCookie.Append(context.Response, claimed.Value.Credential, request.Remember, now);
        return TypedResults.NoContent();
    }
}
