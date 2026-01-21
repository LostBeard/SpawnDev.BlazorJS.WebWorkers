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
    Register = ServiceWorkerStartupRegistration.Unregister
});

builder.Services.AddSingleton(builder.Configuration); // used to demo appsettings reading in workers
builder.Services.AddSingleton<IMathsService, MathsService>();

builder.Services.AddKeyedSingleton<ITestService2>("apples", (_, key) => new TestService2((string)key!));
builder.Services.AddKeyedSingleton<ITestService2>("bananas", (_, key) => new TestService2((string)key!));

builder.Services.AddSingleton<AsyncCallDispatcherTest>();

// This service holds unit tests
builder.Services.AddSingleton<UnitTestsService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

await builder.Build().BlazorJSRunAsync();
