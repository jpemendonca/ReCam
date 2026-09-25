using Recam.Server.Domain;

namespace Recam.Server.Features.Pairing;

public static class CreatePairingTokenRequestValidator
{
    /// <summary>A token can be asked for a camera or a viewer; the owner role is never on offer.</summary>
    public static Result<DeviceRole> Validate(CreatePairingTokenRequest request)
    {
        if (string.Equals(request.Role, nameof(DeviceRole.Camera), StringComparison.OrdinalIgnoreCase))
        {
            return DeviceRole.Camera;
        }

        if (string.Equals(request.Role, nameof(DeviceRole.Viewer), StringComparison.OrdinalIgnoreCase))
        {
            return DeviceRole.Viewer;
        }

        return DomainError.Validation(new Dictionary<string, string[]>
        {
            ["role"] = ["Role must be 'camera' or 'viewer'."],
        });
    }
}
