using Recam.Server.Domain;

namespace Recam.Server.Features.Pairing;

public sealed record PairRequest(string? Token, string? Name, IReadOnlyList<DeviceRole>? ExpectedRoles);
