using Recam.Server.Domain;

namespace Recam.Server.Infrastructure.Realtime;

public static class HubResultExtensions
{
    public static HubResult ToHubResult(this DomainError error) => new(false, error.Code, error.Message);
}
