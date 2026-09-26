namespace Recam.Web.Start;

public enum StartState
{
    Loading,

    /// <summary>No Monitor on the server yet: this browser asks for the first-time code.</summary>
    NeedsCode,

    /// <summary>The server has a Monitor, and it is not this browser.</summary>
    ServerTaken,

    /// <summary>This browser is a Monitor.</summary>
    Monitor,

    /// <summary>The server did not answer.</summary>
    Offline,
}
