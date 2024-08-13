using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.WebWorkers;
using SpawnDev.BlazorJS.WebWorkers.Demo6;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazorJSRuntime(out var JS);
builder.Services.AddWebWorkerService();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host = await builder.Build().StartBackgroundServices();


var arg = new SharedCancellationTokenSource();
var token = arg.Token;

JS.Set("_token1", token);
var token12 = JS.Get<JSObject>("_token1");
var keys = token12.JSRef?.Keys();

var token1 = JS.Get<SharedCancellationToken>("_token1");

arg.Cancel();
var gg = token1.IsCancellationRequested;

await host.BlazorJSRunAsync();
