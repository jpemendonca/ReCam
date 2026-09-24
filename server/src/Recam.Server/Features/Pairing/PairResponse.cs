using Recam.Server.Domain;

namespace Recam.Server.Features.Pairing;

public sealed record PairResponse(Guid DeviceId, string Credential, DeviceRole Role, string ServerName);
