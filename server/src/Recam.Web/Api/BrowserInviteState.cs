using System.Text.Json.Serialization;

namespace Recam.Web.Api;

/// <summary>Where an invitation stands, for the Monitor that made it.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BrowserInviteState>))]
public enum BrowserInviteState
{
    Waiting,
    Used,
    Expired,
}
