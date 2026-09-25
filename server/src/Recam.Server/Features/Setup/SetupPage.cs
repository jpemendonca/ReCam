using System.Globalization;
using System.Net;
using System.Text;
using QRCoder;
using Recam.Server.Domain;

namespace Recam.Server.Features.Setup;

/// <summary>
/// Server-rendered HTML, in English and Portuguese, with no script. Both pages reload
/// themselves: the panel to follow presence, the QR page to turn into the panel once the first
/// phone pairs (slower, so the code can still be copied).
/// </summary>
public static class SetupPage
{
    public const int PanelRefreshSeconds = 5;
    public const int PendingRefreshSeconds = 30;

    public static string RenderPending(OwnerSetupStatus.Pending pending)
    {
        using var qrData = QRCodeGenerator.GenerateQrCode(pending.PairingUri, QRCodeGenerator.ECCLevel.M);
        using var svg = new SvgQRCode(qrData);
        var expiresAt = WebUtility.HtmlEncode(pending.ExpiresAt.ToString("HH:mm 'UTC'", CultureInfo.InvariantCulture));
        return Layout($"""
            <h1>Pair your first phone</h1>
            <p>Open the ReCam app on the phone that will watch, choose <strong>For watching</strong> and scan this code.</p>
            <p lang="pt">Abra o app ReCam no celular que vai assistir, escolha <strong>Para assistir</strong> e leia este código.</p>
            <div class="qr">{svg.GetGraphic(8)}</div>
            <p>No camera? In the app, choose <strong>Paste code</strong> and paste this. · Sem câmera? No app, escolha <strong>Colar código</strong> e cole isto.</p>
            <textarea class="code" readonly rows="4" onclick="this.select()">{WebUtility.HtmlEncode(pending.PairingUri)}</textarea>
            <p class="muted">Expires at {expiresAt}; a new code appears by itself. · Expira às {expiresAt}; um código novo aparece sozinho.</p>
            """, PendingRefreshSeconds);
    }

    public static string RenderPanel(IReadOnlyList<PanelDevice> devices)
    {
        var rows = new StringBuilder();
        foreach (var device in devices)
        {
            rows.Append(Row(device));
        }

        return Layout($"""
            <h1>ReCam</h1>
            <p>Devices paired with this server. Add cameras and phones from <strong>Add</strong>, on a phone that watches.</p>
            <p lang="pt">Aparelhos pareados com este servidor. Adicione câmeras e celulares pelo <strong>Adicionar</strong>, num celular que assiste.</p>
            <table>
            <thead><tr><th>Name · Nome</th><th>Type · Tipo</th><th>Online</th><th>Streaming · Transmitindo</th><th>Watching · Assistindo</th><th>Battery · Bateria</th></tr></thead>
            <tbody>
            {rows}</tbody>
            </table>
            <p class="muted">Updates every {PanelRefreshSeconds} seconds. · Atualiza a cada {PanelRefreshSeconds} segundos.</p>
            """, PanelRefreshSeconds);
    }

    private static string Row(PanelDevice device)
    {
        var isCamera = device.Role == DeviceRole.Camera;
        var type = isCamera ? "Camera · Câmera" : "Watches · Assiste";
        var online = device.Online ? "<span class=\"on\">yes · sim</span>" : "<span class=\"off\">no · não</span>";
        var streaming = !isCamera ? "—" : device.Publishing ? "yes · sim" : "no · não";
        var watching = isCamera ? device.Watchers.ToString(CultureInfo.InvariantCulture) : "—";
        var battery = !isCamera || device.BatteryLevel is not { } level
            ? "—"
            : string.Create(CultureInfo.InvariantCulture, $"{level}%{(device.IsCharging == true ? " ⚡" : string.Empty)}");
        return $"""
            <tr data-device="{device.Id:N}"><td>{WebUtility.HtmlEncode(device.Name)}</td><td>{type}</td><td>{online}</td><td>{streaming}</td><td>{watching}</td><td>{battery}</td></tr>

            """;
    }

    private static string Layout(string body, int refreshSeconds) => $$"""
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <meta http-equiv="refresh" content="{{refreshSeconds}}">
        <title>ReCam</title>
        <style>
        body { font-family: system-ui, sans-serif; max-width: 48rem; margin: 2rem auto; padding: 0 1rem; line-height: 1.5; }
        .qr svg { width: 100%; max-width: 32rem; height: auto; background: #fff; }
        .muted { color: #666; font-size: .9rem; }
        .code { width: 100%; font-family: ui-monospace, monospace; font-size: .8rem; }
        table { width: 100%; border-collapse: collapse; }
        th, td { text-align: left; padding: .4rem .5rem; border-bottom: 1px solid #ddd; }
        .on { color: #1a7f37; }
        .off { color: #999; }
        </style>
        </head>
        <body>
        {{body}}
        </body>
        </html>
        """;
}
