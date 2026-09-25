namespace Recam.Server.Infrastructure.Tls;

/// <summary>
/// What goes in the QR code's <c>f</c>: the SHA-256 of the server certificate, or null when a
/// reverse proxy terminates TLS and the app should trust the system CAs instead.
/// </summary>
public sealed record CertificatePin(string? Fingerprint);
