using Recam.Server.Domain;

namespace Recam.Server.Features.Devices;

public sealed record MeResponse(Guid DeviceId, string Name, DeviceRole Role);
