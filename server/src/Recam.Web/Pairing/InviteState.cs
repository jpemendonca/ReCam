namespace Recam.Web.Pairing;

public enum InviteState
{
    Loading,
    Ready,
    Failed,

    /// <summary>A browser opened the link: it is a Monitor now.</summary>
    Used,

    /// <summary>Ten minutes passed unused; the person asks for another.</summary>
    Expired,
}
