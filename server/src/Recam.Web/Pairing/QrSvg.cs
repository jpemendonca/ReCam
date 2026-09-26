using System.Globalization;
using System.Text;
using QRCoder;

namespace Recam.Web.Pairing;

/// <summary>
/// Draws a QR code as one SVG path, with attributes only: the content security policy blocks
/// inline styles, so nothing here may use a style attribute.
/// </summary>
public static class QrSvg
{
    public static string Render(string text)
    {
        using var data = QRCodeGenerator.GenerateQrCode(text, QRCodeGenerator.ECCLevel.L);
        var modules = data.ModuleMatrix;
        var size = modules.Count;
        var path = new StringBuilder();
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                if (modules[y][x])
                {
                    path.Append(CultureInfo.InvariantCulture, $"M{x} {y}h1v1h-1z");
                }
            }
        }

        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {size} {size}\" shape-rendering=\"crispEdges\" " +
            $"role=\"img\"><rect width=\"{size}\" height=\"{size}\" fill=\"#fff\"/><path fill=\"#000\" d=\"{path}\"/></svg>";
    }
}
