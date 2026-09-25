using Recam.Server.Domain;

namespace Recam.Server.Features.Pairing;

public sealed record ValidPairRequest(string Token, string Name, IReadOnlyList<DeviceRole> ExpectedRoles);
