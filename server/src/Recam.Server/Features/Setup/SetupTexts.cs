using System.Globalization;
using Microsoft.Net.Http.Headers;

namespace Recam.Server.Features.Setup;

/// <summary>Every sentence of the server page, in one language.</summary>
public sealed record SetupTexts
{
    public static readonly SetupTexts English = new()
    {
        Language = "en",
        PendingTitle = "Pair your first Monitor",
        PendingIntro = "The Monitor is the phone you watch the cameras on. Pair it first; it then adds the cameras.",
        PendingSteps =
        [
            "Take the phone that will watch the cameras and open ReCam.",
            "On the first screen, tap <strong>Watch</strong>.",
            "Point the phone at this QR code. No camera? Tap <strong>Paste code</strong> and paste the code below.",
            "Done: this page turns into the panel of your devices.",
        ],
        CodeLabel = "Pairing code",
        ExpiresAt = time => $"The code expires at {time} and a new one appears by itself.",
        PanelTitle = "Your ReCam",
        CamerasTitle = "Cameras",
        MonitorsTitle = "Monitors",
        NoCameras = "No cameras yet.",
        AddCameraTitle = "Add a camera",
        AddCameraSteps =
        [
            "On a Monitor, tap <strong>+</strong> (Add camera). A QR code appears.",
            "On the phone that will film, open ReCam, tap <strong>Film</strong>, give it a name and scan that QR code.",
            "The camera shows up here and in the Monitor's list.",
        ],
        Online = "Online",
        Offline = "Offline",
        Streaming = "Streaming",
        StandingBy = "Standing by",
        Watching = "Watching now",
        Battery = "Battery",
        Charging = "charging",
        UpdatesEvery = seconds => $"This page updates every {seconds} seconds.",
    };

    public static readonly SetupTexts Portuguese = new()
    {
        Language = "pt-BR",
        PendingTitle = "Pareie o seu primeiro Monitor",
        PendingIntro = "O Monitor é o celular em que você assiste às câmeras. Pareie ele primeiro; depois ele adiciona as câmeras.",
        PendingSteps =
        [
            "Pegue o celular que vai assistir às câmeras e abra o ReCam.",
            "Na primeira tela, toque em <strong>Assistir</strong>.",
            "Aponte o celular para este QR code. Sem câmera? Toque em <strong>Colar código</strong> e cole o código abaixo.",
            "Pronto: esta página vira o painel dos seus aparelhos.",
        ],
        CodeLabel = "Código de pareamento",
        ExpiresAt = time => $"O código expira às {time}, e um novo aparece sozinho.",
        PanelTitle = "Seu ReCam",
        CamerasTitle = "Câmeras",
        MonitorsTitle = "Monitores",
        NoCameras = "Nenhuma câmera ainda.",
        AddCameraTitle = "Adicionar uma câmera",
        AddCameraSteps =
        [
            "Num Monitor, toque no <strong>+</strong> (Adicionar câmera). Aparece um QR code.",
            "No celular que vai filmar, abra o ReCam, toque em <strong>Filmar</strong>, dê um nome e leia esse QR code.",
            "A câmera aparece aqui e na lista do Monitor.",
        ],
        Online = "Online",
        Offline = "Offline",
        Streaming = "Transmitindo",
        StandingBy = "Parada",
        Watching = "Assistindo agora",
        Battery = "Bateria",
        Charging = "carregando",
        UpdatesEvery = seconds => $"Esta página se atualiza a cada {seconds} segundos.",
    };

    public required string Language { get; init; }

    public required string PendingTitle { get; init; }

    public required string PendingIntro { get; init; }

    /// <summary>Trusted HTML: only the page's own markup, never user input.</summary>
    public required IReadOnlyList<string> PendingSteps { get; init; }

    public required string CodeLabel { get; init; }

    public required Func<string, string> ExpiresAt { get; init; }

    public required string PanelTitle { get; init; }

    public required string CamerasTitle { get; init; }

    public required string MonitorsTitle { get; init; }

    public required string NoCameras { get; init; }

    public required string AddCameraTitle { get; init; }

    /// <summary>Trusted HTML: only the page's own markup, never user input.</summary>
    public required IReadOnlyList<string> AddCameraSteps { get; init; }

    public required string Online { get; init; }

    public required string Offline { get; init; }

    public required string Streaming { get; init; }

    public required string StandingBy { get; init; }

    public required string Watching { get; init; }

    public required string Battery { get; init; }

    public required string Charging { get; init; }

    public required Func<int, string> UpdatesEvery { get; init; }

    /// <summary>
    /// Picks Portuguese or English from the browser's Accept-Language, by preference; anything
    /// else, or no header, gets English.
    /// </summary>
    public static SetupTexts For(IEnumerable<StringWithQualityHeaderValue> acceptLanguage) =>
        acceptLanguage
            .OrderByDescending(language => language.Quality ?? 1)
            .Select(language => Match(language.Value.Value))
            .FirstOrDefault(texts => texts is not null)
        ?? English;

    private static SetupTexts? Match(string? tag) => tag switch
    {
        not null when tag.StartsWith("pt", true, CultureInfo.InvariantCulture) => Portuguese,
        not null when tag.StartsWith("en", true, CultureInfo.InvariantCulture) => English,
        _ => null,
    };
}
