using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Recam.Web;
using Recam.Web.Api;
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
builder.Services.AddTransient<StartController>();

await builder.Build().RunAsync();
