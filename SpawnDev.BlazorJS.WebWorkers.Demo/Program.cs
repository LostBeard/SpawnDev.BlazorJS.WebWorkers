using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.WebWorkers;
using SpawnDev.BlazorJS.WebWorkers.Demo;
using SpawnDev.BlazorJS.WebWorkers.Demo.Services;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddBlazorJSRuntime(out var JS);
Console.WriteLine($">>> BlazorJS Running: {JS.GlobalScope.ToString()}");

builder.Services.AddWebWorkerService();

builder.Services.RegisterServiceWorker<AppServiceWorker>(new ServiceWorkerConfig
{
    Options = new ServiceWorkerRegistrationOptions
    {
        //Type = "module",
    },
});

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

var host = await builder.Build().StartBackgroundServices();


JS.Set("_test", async (bool useModule) => {
    var webWorkerService = host.Services.GetRequiredService<WebWorkerService>();
    if (useModule)
    {
        var worker = await webWorkerService.GetWebWorker(new WebWorkerOptions { WorkerOptions = new WorkerOptions { Type = "module" } });
        await worker!.Run(() => Console.WriteLine("module worker"));
    }
    else
    {
        var worker = await webWorkerService.GetWebWorker();
        await worker!.Run(() => Console.WriteLine("classic worker"));
    }
});

//var arg = new SharedCancellationTokenSource();
//var token = arg.Token;

//JS.Set("_token1", token);
//var token12 = JS.Get<JSObject>("_token1");
//var keys = token12.JSRef?.Keys();
//var token1 = JS.Get<SharedCancellationToken>("_token1");

await host.BlazorJSRunAsync();
