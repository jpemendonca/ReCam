using Recam.Server.Domain;

namespace Recam.Server.Features.Pairing;

public static class CreatePairingTokenRequestValidator
{
    /// <summary>Only camera tokens can be requested for now.</summary>
    public static Result<DeviceRole> Validate(CreatePairingTokenRequest request)
    {
        if (!string.Equals(request.Role, nameof(DeviceRole.Camera), StringComparison.OrdinalIgnoreCase))
        {
            return DomainError.Validation(new Dictionary<string, string[]>
            {
                ["role"] = ["Role must be 'camera'."],
            });
        }

        return DeviceRole.Camera;
    }
}
