using System.Globalization;

namespace Recam.Web.Brighten;

/// <summary>
/// Brightness and contrast applied to the video on this screen only ("Brighten"); the camera,
/// what it sends and the recordings stay as they are. Same ranges as the app.
/// </summary>
public sealed record ImageAdjustment(double Brightness = 1, double Contrast = 1)
{
    public const double MinBrightness = 1;
    public const double MaxBrightness = 3;
    public const double MinContrast = 0.5;
    public const double MaxContrast = 2;

    public static readonly ImageAdjustment Normal = new();

    public bool IsNormal => Brightness == 1 && Contrast == 1;

    public ImageAdjustment With(double? brightness = null, double? contrast = null) => new(
        Math.Clamp(brightness ?? Brightness, MinBrightness, MaxBrightness),
        Math.Clamp(contrast ?? Contrast, MinContrast, MaxContrast));

    public string Encode() => string.Create(CultureInfo.InvariantCulture, $"{Brightness};{Contrast}");

    public static ImageAdjustment Decode(string? value)
    {
        var parts = value?.Split(';') ?? [];
        return parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var brightness)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var contrast)
            ? Normal.With(brightness, contrast)
            : Normal;
    }
}
