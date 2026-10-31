namespace Recam.Web.Api;

/// <summary>A camera's newest recorded picture (GET /api/cameras/{id}/thumbnail).</summary>
/// <param name="DataUrl">The JPEG as a data URL, ready for an img; the page allows data images.</param>
/// <param name="TakenAt">About when it was filmed, from the Last-Modified header.</param>
public sealed record ThumbnailInfo(string DataUrl, DateTimeOffset? TakenAt);
