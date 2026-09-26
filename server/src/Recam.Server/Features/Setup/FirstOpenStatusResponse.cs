namespace Recam.Server.Features.Setup;

/// <summary>Whether a browser may still become the first Monitor. Says nothing else about the server.</summary>
public sealed record FirstOpenStatusResponse(bool Open);
