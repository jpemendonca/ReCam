using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Cameras;
using Recam.Web.Live;
using Recam.Web.Realtime;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Cameras;

public sealed class CameraListTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly FakeDeviceHub _hub = new();
    private readonly FakeLiveVideo _video = new();

    public CameraListTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<IDeviceHub>(_hub);
        Services.AddSingleton<CameraListController>();
        Services.AddSingleton<ThumbnailController>();
        Services.AddSingleton(_video);
        Services.AddTransient<ILiveVideo>(provider => provider.GetRequiredService<FakeLiveVideo>());
        Services.AddSingleton<SoundChoice>();
        Services.AddTransient<LiveController>();
    }

    [Fact(DisplayName = "Each camera shows its state, battery, temperature and a link to its live view")]
    public void Render_Cameras_ShowsStatus()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", publishing: true, recording: true);
        _api.Cameras.Add(camera);

        // act
        var list = Render<CameraList>();

        // assert
        Assert.Equal("Porta", list.Find(".name").TextContent);
        Assert.Equal("Online · transmitindo · Bateria 80% · 32 °C", list.Find(".status").TextContent);
        Assert.Equal("Gravando", list.Find(".badge").TextContent);
        Assert.Contains("live", list.Find(".dot").ClassList);
        Assert.Equal($"cameras/{camera.Id}", list.Find("a.open").GetAttribute("href"));
        Assert.Equal("Conectado", list.Find(".hub").TextContent.Trim());
    }

    [Fact(DisplayName = "A camera without H.264 cannot record, and says why")]
    public void Render_NoH264_ExplainsInsteadOfToggle()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        _api.Cameras.Add(Support.Cameras.Make("Sala", canRecord: false));

        // act
        var list = Render<CameraList>();

        // assert
        Assert.Empty(list.FindAll("input[role=switch]"));
        Assert.Equal("Não grava: este celular não tem H.264", list.Find(".recording .state").TextContent);
    }

    [Fact(DisplayName = "Turning the Record switch on asks the server for that camera")]
    public void Toggle_Record_CallsHub()
    {
        // arrange
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        var list = Render<CameraList>();

        // act
        list.Find("input[role=switch]").Change(true);

        // assert
        Assert.Contains($"SetRecording {camera.Id} True", _hub.Calls);
    }

    [Theory(DisplayName = "Next to the switch, the card says what really happens with the recording")]
    [InlineData(true, CameraRecordingState.Recording, "Gravando", "on")]
    [InlineData(true, CameraRecordingState.Off, "Começando a gravar…", "starting")]
    [InlineData(true, CameraRecordingState.Starting, "Começando a gravar…", "starting")]
    [InlineData(true, CameraRecordingState.Offline, "Não está gravando: a câmera está offline", "problem")]
    [InlineData(true, CameraRecordingState.NoSpace, "Não está gravando: o disco do servidor está cheio", "problem")]
    [InlineData(true, CameraRecordingState.Stalled, "Não está gravando: o vídeo não chega ao servidor", "problem")]
    [InlineData(false, CameraRecordingState.Recording, "Gravação desligada", "off")]
    public void Render_RecordingState_ShowsWhatHappens(bool switchedOn, CameraRecordingState state, string text, string look)
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        _api.Cameras.Add(Support.Cameras.Make("Porta", recording: switchedOn, recordingState: state));

        // act
        var list = Render<CameraList>();

        // assert
        Assert.Equal(text, list.Find(".recording .state").TextContent);
        Assert.Contains(look, list.Find(".recording").ClassList);
        Assert.Equal(switchedOn, list.Find("input[role=switch]").HasAttribute("checked"));
    }

    [Fact(DisplayName = "When the server hears the first file, Starting turns into Recording on the card")]
    public void StatusChanged_FirstFile_TurnsStartingIntoRecording()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", recording: true, recordingState: CameraRecordingState.Starting);
        _api.Cameras.Add(camera);
        var list = Render<CameraList>();
        var before = list.Find(".recording .state").TextContent;

        // act
        _hub.SendStatus(camera with { RecordingState = CameraRecordingState.Recording });

        // assert
        Assert.Equal("Começando a gravar…", before);
        list.WaitForAssertion(() => Assert.Equal("Gravando", list.Find(".recording .state").TextContent));
        Assert.Equal("Gravando", list.Find(".badge").TextContent);
    }

    [Fact(DisplayName = "An empty server says there is no camera yet")]
    public void Render_NoCameras_SaysSo()
    {
        // arrange
        using var _ = Culture.Use("en-US");

        // act
        var list = Render<CameraList>();

        // assert
        Assert.Equal("No cameras yet.", list.Find(".note").TextContent);
    }

    [Fact(DisplayName = "The dot is green while the camera sends a picture and red otherwise")]
    public void Render_Dot_GreenOnlyWhileSending()
    {
        // arrange
        _api.Cameras.Add(Support.Cameras.Make("Porta", publishing: true));
        _api.Cameras.Add(Support.Cameras.Make("Sala"));

        // act
        var list = Render<CameraList>();

        // assert
        Assert.Equal(["dot live", "dot off"], list.FindAll(".dot").Select(dot => dot.ClassName));
    }

    [Fact(DisplayName = "Clicking anywhere on the card opens the live video")]
    public void Click_Card_OpensLiveView()
    {
        // arrange
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        var list = Render<CameraList>();

        // act
        list.Find("li.camera").Click();

        // assert
        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith($"/cameras/{camera.Id}", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Play opens the live video in the card, which closes by itself and stops watching")]
    public void Play_OpensLive_ClosesByItself()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        var list = Render<CameraList>(parameters => parameters.Add(cameras => cameras.PreviewCloseAfter, TimeSpan.FromMilliseconds(200)));
        Assert.Empty(list.FindAll(".preview"));

        // act
        list.Find("button.play").Click();

        // assert
        list.WaitForAssertion(() => Assert.Contains($"WatchCamera {camera.Id}", _hub.Calls));
        Assert.Single(list.FindAll("button.close-live"));
        list.WaitForAssertion(() => Assert.Empty(list.FindAll(".preview")), TimeSpan.FromSeconds(5));
        list.WaitForAssertion(() => Assert.Contains($"UnwatchCamera {camera.Id}", _hub.Calls));
        Assert.Single(list.FindAll("button.play"));
    }

    [Fact(DisplayName = "Each card shows the camera's newest picture and when it was filmed, or says it has none")]
    public void Render_Pictures_ShowsPictureOrNote()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var porta = Support.Cameras.Make("Porta");
        var sala = Support.Cameras.Make("Sala");
        _api.Cameras.AddRange([porta, sala]);
        var takenAt = new DateTimeOffset(2026, 10, 31, 14, 5, 0, TimeSpan.Zero);
        _api.Thumbnails[porta.Id] = new ThumbnailInfo("data:image/jpeg;base64,AAAA", takenAt);

        // act
        var list = Render<CameraList>();

        // assert
        list.WaitForAssertion(() => Assert.Single(list.FindAll("img.picture")));
        Assert.Equal("data:image/jpeg;base64,AAAA", list.Find("img.picture").GetAttribute("src"));
        Assert.Equal($"Imagem das {takenAt.ToLocalTime():HH:mm}", list.Find(".taken").TextContent);
        Assert.Equal("Ainda sem imagem", list.Find(".no-picture").TextContent);
    }

    [Fact(DisplayName = "Pictures are asked again on a timer, and no more once the page closes")]
    public async Task Pictures_Timer_AskAgainUntilLeaving()
    {
        // arrange
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        var list = Render<CameraList>(parameters => parameters.Add(cameras => cameras.PicturesEvery, TimeSpan.FromMilliseconds(100)));

        // act
        list.WaitForAssertion(() => Assert.True(_api.ThumbnailsAsked.GetValueOrDefault(camera.Id) >= 3), TimeSpan.FromSeconds(5));
        var whileOpen = _api.ThumbnailsAsked[camera.Id];
        await DisposeComponentsAsync();

        // The renderer may close the page a moment later, when it is in the middle of a render; a
        // loop that leaked would never go quiet.
        var quiet = false;
        for (var tries = 0; tries < 20 && !quiet; tries++)
        {
            var before = _api.ThumbnailsAsked[camera.Id];
            await Task.Delay(300, Xunit.TestContext.Current.CancellationToken);
            quiet = _api.ThumbnailsAsked[camera.Id] == before;
        }

        // assert
        Assert.True(whileOpen >= 3);
        Assert.True(quiet);
    }

    [Fact(DisplayName = "Watch all opens every online camera live, and Close all closes them")]
    public void WatchAll_OpensAndClosesEveryOnlineCamera()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var porta = Support.Cameras.Make("Porta");
        var sala = Support.Cameras.Make("Sala");
        var quintal = Support.Cameras.Make("Quintal", online: false);
        _api.Cameras.AddRange([porta, sala, quintal]);
        var list = Render<CameraList>();

        // act
        list.Find("button.watch-all").Click();
        var opened = list.FindAll(".preview").Count;
        var label = list.Find("button.watch-all").TextContent.Trim();
        list.Find("button.watch-all").Click();

        // assert
        Assert.Equal(2, opened);
        Assert.Equal("Fechar todas", label);
        Assert.Empty(list.FindAll(".preview"));
        Assert.Equal("Assistir todas", list.Find("button.watch-all").TextContent.Trim());
        list.WaitForAssertion(() => Assert.Contains($"UnwatchCamera {sala.Id}", _hub.Calls));
        Assert.DoesNotContain($"WatchCamera {quintal.Id}", _hub.Calls);
    }

    [Fact(DisplayName = "A card shows the flashlight while it is on, and follows the camera's reports")]
    public void Render_TorchOn_ShowsIconUntilOff()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", publishing: true, torchOn: true);
        _api.Cameras.Add(camera);
        var list = Render<CameraList>();
        var shown = list.Find(".torch").GetAttribute("title");

        // act
        _hub.SendTorch(camera.Id, false);

        // assert
        Assert.Equal("Lanterna ligada", shown);
        list.WaitForAssertion(() => Assert.Empty(list.FindAll(".torch")));
    }
}
