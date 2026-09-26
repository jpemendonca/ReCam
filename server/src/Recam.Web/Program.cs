using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Recam.Web;
using Recam.Web.Api;
using Recam.Web.Cameras;
using Recam.Web.Live;
using Recam.Web.Pairing;
using Recam.Web.Realtime;
using Recam.Web.Recordings;
using Recam.Web.Start;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The browser's language picks the culture; Blazor sets it before the app starts.
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
builder.Services.AddTransient<StartController>();
builder.Services.AddScoped<CameraListController>();
builder.Services.AddTransient<LiveController>();
builder.Services.AddTransient<AddDeviceController>();
builder.Services.AddTransient<TimelineController>();
builder.Services.AddTransient<QuotaController>();

await builder.Build().RunAsync();
