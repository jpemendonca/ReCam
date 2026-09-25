using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using QRCoder;
using Recam.Server.Domain;

namespace Recam.Server.Features.Setup;

/// <summary>
/// Server-rendered HTML in one language, with no script and no actions. Both pages reload
/// themselves: the panel to follow presence, the QR page to turn into the panel once the first
/// Monitor pairs (slower, so the code can still be copied).
/// </summary>
public static class SetupPage
{
    public const int PanelRefreshSeconds = 5;
    public const int PendingRefreshSeconds = 30;

    // Escapes markup but keeps accented letters readable in the page source.
    private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

    public static string RenderPending(OwnerSetupStatus.Pending pending, SetupTexts texts)
    {
        using var qrData = QRCodeGenerator.GenerateQrCode(pending.PairingUri, QRCodeGenerator.ECCLevel.M);
        using var svg = new SvgQRCode(qrData);
        var expiresAt = pending.ExpiresAt.ToString("HH:mm 'UTC'", CultureInfo.InvariantCulture);
        return Layout(texts, PendingRefreshSeconds, $"""
            <header><p class="brand">ReCam</p><h1>{Encode(texts.PendingTitle)}</h1><p class="lead">{Encode(texts.PendingIntro)}</p></header>
            <section class="pair">
            <ol class="steps">
            {Steps(texts.PendingSteps)}</ol>
            <div class="qr card">{svg.GetGraphic(8)}</div>
            </section>
            <section class="card">
            <label for="code">{Encode(texts.CodeLabel)}</label>
            <textarea id="code" class="code" readonly rows="3">{Encode(pending.PairingUri)}</textarea>
            <p class="muted">{Encode(texts.ExpiresAt(expiresAt))}</p>
            </section>
            """);
    }

    public static string RenderPanel(IReadOnlyList<PanelDevice> devices, RecordingUsage recordings, SetupTexts texts)
    {
        var cameras = devices.Where(device => device.Role == DeviceRole.Camera).ToList();
        var monitors = devices.Where(device => device.Role != DeviceRole.Camera).ToList();
        var cameraCards = cameras.Count == 0
            ? $"<p class=\"muted\">{Encode(texts.NoCameras)}</p>"
            : string.Concat(cameras.Select(camera => CameraCard(camera, texts)));
        return Layout(texts, PanelRefreshSeconds, $"""
            <header><p class="brand">ReCam</p><h1>{Encode(texts.PanelTitle)}</h1></header>
            <section>
            <h2>{Encode(texts.CamerasTitle)} <span class="count">{cameras.Count}</span></h2>
            <div class="grid">
            {cameraCards}
            </div>
            </section>
            <section>
            <h2>{Encode(texts.MonitorsTitle)} <span class="count">{monitors.Count}</span></h2>
            <div class="grid">
            {string.Concat(monitors.Select(monitor => MonitorCard(monitor, texts)))}
            </div>
            </section>
            <section class="card">
            <h2>{Encode(texts.RecordingsTitle)}</h2>
            <p>{Encode(texts.RecordingsUsage(Gigabytes(recordings.UsedBytes, texts), Gigabytes(recordings.QuotaBytes, texts)))}</p>
            </section>
            <section class="card">
            <h2>{Encode(texts.AddCameraTitle)}</h2>
            <ol class="steps">
            {Steps(texts.AddCameraSteps)}</ol>
            </section>
            <p class="muted">{Encode(texts.UpdatesEvery(PanelRefreshSeconds))}</p>
            """);
    }

    private static string CameraCard(PanelDevice camera, SetupTexts texts)
    {
        var streaming = !camera.Online
            ? string.Empty
            : camera.Publishing
                ? $" <span class=\"badge live\">{Encode(texts.Streaming)}</span>"
                : $" <span class=\"badge idle\">{Encode(texts.StandingBy)}</span>";
        var battery = camera.BatteryLevel is { } level
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"{level}%{(camera.IsCharging == true ? $" ({texts.Charging})" : string.Empty)}")
            : "—";
        return $"""
            <article class="device card" data-device="{camera.Id:N}"><h3>{Encode(camera.Name)}</h3><p>{PresenceBadge(camera, texts)}{streaming}</p><dl><dt>{Encode(texts.Watching)}</dt><dd>{camera.Watchers.ToString(CultureInfo.InvariantCulture)}</dd><dt>{Encode(texts.Battery)}</dt><dd>{Encode(battery)}</dd></dl></article>

            """;
    }

    private static string MonitorCard(PanelDevice monitor, SetupTexts texts) => $"""
        <article class="device card" data-device="{monitor.Id:N}"><h3>{Encode(monitor.Name)}</h3><p>{PresenceBadge(monitor, texts)}</p></article>

        """;

    private static string PresenceBadge(PanelDevice device, SetupTexts texts) => device.Online
        ? $"<span class=\"badge on\">{Encode(texts.Online)}</span>"
        : $"<span class=\"badge off\">{Encode(texts.Offline)}</span>";

    private static string Steps(IEnumerable<string> steps) =>
        string.Concat(steps.Select(step => $"<li>{step}</li>\n"));

    private static string Encode(string text) => Html.Encode(text);

    // "1,2" in Portuguese and "1.2" in English; whole numbers without decimals.
    private static string Gigabytes(long bytes, SetupTexts texts) =>
        (bytes / 1024d / 1024d / 1024d).ToString("0.#", CultureInfo.GetCultureInfo(texts.Language));

    private static string Layout(SetupTexts texts, int refreshSeconds, string body) => $$"""
        <!doctype html>
        <html lang="{{texts.Language}}">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <meta name="color-scheme" content="light dark">
        <meta http-equiv="refresh" content="{{refreshSeconds}}">
        <title>ReCam</title>
        <style>
        :root { --bg: #f4f6f5; --card: #ffffff; --text: #17201d; --muted: #5d6b66; --line: #dde3e0;
          --accent: #00796b; --on: #1a7f37; --off: #8a948f; --live: #c62828; --radius: 14px; }
        @media (prefers-color-scheme: dark) {
          :root { --bg: #101614; --card: #1a2320; --text: #e6ece9; --muted: #9aa8a2; --line: #2c3833;
            --accent: #4db6ac; --on: #4cc26b; --off: #7c8783; --live: #ef5350; }
        }
        * { box-sizing: border-box; }
        body { margin: 0; background: var(--bg); color: var(--text); font: 16px/1.5 system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; }
        main { max-width: 60rem; margin: 0 auto; padding: 1.5rem 1rem 3rem; }
        header { margin-bottom: 1.5rem; }
        .brand { margin: 0; color: var(--accent); font-weight: 700; letter-spacing: .04em; }
        h1 { margin: .2rem 0 .4rem; font-size: 1.8rem; line-height: 1.2; }
        h2 { font-size: 1.2rem; margin: 1.5rem 0 .75rem; }
        h3 { margin: 0 0 .4rem; font-size: 1.05rem; overflow-wrap: anywhere; }
        .lead, .muted { color: var(--muted); }
        .muted { font-size: .9rem; }
        .card { background: var(--card); border: 1px solid var(--line); border-radius: var(--radius); padding: 1rem 1.25rem; }
        section.card { margin-top: 1.5rem; }
        .card h2 { margin-top: 0; }
        .pair { display: grid; gap: 1.5rem; align-items: center; }
        @media (min-width: 720px) { .pair { grid-template-columns: 1fr 1fr; } }
        .steps { margin: 0; padding-left: 1.4rem; }
        .steps li { margin: .35rem 0 .7rem; padding-left: .25rem; }
        .steps li::marker { color: var(--accent); font-weight: 700; }
        .qr { background: #ffffff; padding: .75rem; }
        .qr svg { display: block; width: 100%; height: auto; }
        label { display: block; font-weight: 600; margin-bottom: .4rem; }
        .code { width: 100%; font: .8rem/1.4 ui-monospace, Consolas, monospace; color: var(--text); background: var(--bg); border: 1px solid var(--line); border-radius: 8px; padding: .5rem; resize: vertical; }
        .grid { display: grid; gap: .75rem; grid-template-columns: repeat(auto-fill, minmax(14rem, 1fr)); }
        .count { color: var(--muted); font-weight: 400; }
        .device p { margin: 0 0 .5rem; }
        .badge { display: inline-block; padding: .05rem .6rem; border-radius: 999px; font-size: .8rem; font-weight: 600; border: 1px solid currentColor; }
        .badge.on { color: var(--on); }
        .badge.off, .badge.idle { color: var(--off); }
        .badge.live { color: var(--live); }
        dl { display: grid; grid-template-columns: auto 1fr; gap: .15rem .75rem; margin: 0; font-size: .9rem; }
        dt { color: var(--muted); }
        dd { margin: 0; text-align: right; font-variant-numeric: tabular-nums; }
        </style>
        </head>
        <body>
        <main>
        {{body}}
        </main>
        </body>
        </html>
        """;
}
