using Recam.Server.Domain;

namespace Recam.Server.Features.Devices;

/// <summary>A device as the Monitor's device list shows it.</summary>
public sealed record DeviceResponse(Guid Id, string Name, DeviceRole Role, bool Online, DateTimeOffset? LastSeenAt);
