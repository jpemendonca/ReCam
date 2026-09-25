using System.Globalization;
using System.Net;
using QRCoder;

namespace Recam.Server.Features.Setup;

public static class SetupPage
{
    public static string Render(OwnerSetupStatus status) => status switch
    {
        OwnerSetupStatus.Pending pending => Layout(PendingBody(pending)),
        OwnerSetupStatus.Configured => Layout(ConfiguredBody),
        _ => throw new InvalidOperationException($"Unknown setup status {status.GetType().Name}."),
    };

    private const string ConfiguredBody = """
        <h1>Recam is already configured</h1>
        <p>An owner phone is already paired. Add cameras from the Watch tab on that phone.</p>
        <h1 lang="pt">O Recam já está configurado</h1>
        <p lang="pt">Um celular dono já está pareado. Adicione câmeras pela aba Assistir desse celular.</p>
        """;

    private static string PendingBody(OwnerSetupStatus.Pending pending)
    {
        using var qrData = QRCodeGenerator.GenerateQrCode(pending.PairingUri, QRCodeGenerator.ECCLevel.M);
        using var svg = new SvgQRCode(qrData);
        var expiresAt = WebUtility.HtmlEncode(pending.ExpiresAt.ToString("HH:mm 'UTC'", CultureInfo.InvariantCulture));
        return $"""
            <h1>Pair your phone</h1>
            <p>Open the Recam app, go to the <strong>Watch</strong> tab and scan this code. This phone becomes the owner.</p>
            <p lang="pt">Abra o app Recam, vá na aba <strong>Assistir</strong> e leia este código. Este celular vira o dono.</p>
            <div class="qr">{svg.GetGraphic(8)}</div>
            <p>No camera? In the app, choose <strong>Paste code</strong> and paste this. · Sem câmera? No app, escolha <strong>Colar código</strong> e cole isto.</p>
            <textarea class="code" readonly rows="4" onclick="this.select()">{WebUtility.HtmlEncode(pending.PairingUri)}</textarea>
            <p class="muted">Expires at {expiresAt}. Reload the page for a new code. · Expira às {expiresAt}. Recarregue a página para um novo código.</p>
            """;
    }

    private static string Layout(string body) => $$"""
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>Recam setup</title>
        <style>
        body { font-family: system-ui, sans-serif; max-width: 32rem; margin: 2rem auto; padding: 0 1rem; line-height: 1.5; }
        .qr svg { width: 100%; height: auto; background: #fff; }
        .muted { color: #666; font-size: .9rem; }
        .code { width: 100%; font-family: ui-monospace, monospace; font-size: .8rem; }
        </style>
        </head>
        <body>
        {{body}}
        </body>
        </html>
        """;
}
