namespace Recam.Server.Domain;

/// <summary>How much of the picture must change for motion to count (SPECS.md 2.4).</summary>
public enum MotionSensitivity
{
    Low,
    Medium,
    High,
}
