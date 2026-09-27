namespace Recam.Web.Join;

public enum JoinState
{
    Loading,

    /// <summary>A valid invitation, waiting for the person to accept it.</summary>
    Ready,

    /// <summary>The address has no complete invitation.</summary>
    Invalid,

    /// <summary>This browser is a Monitor already; the invitation stays unused.</summary>
    AlreadyMonitor,

    /// <summary>The invitation expired or was used.</summary>
    Gone,

    /// <summary>This browser is now a Monitor.</summary>
    Joined,
}
