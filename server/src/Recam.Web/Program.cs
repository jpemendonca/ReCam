using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Recam.Web;
using Recam.Web.Api;
using Recam.Web.Brighten;
using Recam.Web.Cameras;
using Recam.Web.Devices;
using Recam.Web.Live;
using Recam.Web.Localization;
using Recam.Web.Pairing;
using Recam.Web.Realtime;
using Recam.Web.Recordings;
using Recam.Web.Start;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The browser's language picks the culture, unless the person picked one in Settings (below).
builder.Services.AddLocalization();

builder.Services.AddScoped(_ =>
{
    var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    http.DefaultRequestHeaders.Add(HttpRecamApi.WebHeader, "1");
    return http;
});
builder.Services.AddScoped<IRecamApi, HttpRecamApi>();
builder.Services.AddScoped<IDeviceHub, SignalRDeviceHub>();
builder.Services.AddTransient<ILiveVideo, JsLiveVideo>();
builder.Services.AddScoped<StartController>();
builder.Services.AddTransient<ConnectController>();
builder.Services.AddScoped<CameraListController>();
builder.Services.AddScoped<SoundChoice>();
builder.Services.AddTransient<LiveController>();
builder.Services.AddTransient<AddDeviceController>();
builder.Services.AddTransient<TimelineController>();
builder.Services.AddScoped<IVideoClock, JsVideoClock>();
builder.Services.AddTransient<QuotaController>();
builder.Services.AddTransient<DevicesController>();
builder.Services.AddScoped<IBrightenSurface, JsBrightenSurface>();
builder.Services.AddTransient<BrightenController>();

builder.Services.AddScoped<ILanguageStore, JsLanguageStore>();

var host = builder.Build();

// A language picked in Settings wins over the browser's. Blazor loads that culture's texts when
// the host starts, so it has to be set before.
var saved = await host.Services.GetRequiredService<ILanguageStore>().ReadAsync();
if (LanguageChoice.CultureFor(saved) is { } culture)
{
    CultureInfo.DefaultThreadCurrentCulture = culture;
    CultureInfo.DefaultThreadCurrentUICulture = culture;
}

await host.RunAsync();
