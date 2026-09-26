namespace Recam.Web.Pairing;

public enum AddDeviceState
{
    Loading,
    Ready,
    Failed,

    /// <summary>A phone paired with the QR shown.</summary>
    Paired,
}
