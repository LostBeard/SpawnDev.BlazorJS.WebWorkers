using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.WebWorkers;
using SpawnDev.BlazorJS.WebWorkers.Demo;
using SpawnDev.BlazorJS.WebWorkers.Demo.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// add BlazorJSRuntime (Javascript interop)
builder.Services.AddBlazorJSRuntime(out var JS);

// writes the global scope to console
Console.WriteLine($">>> BlazorJS Running: {JS.GlobalScope.ToString()}");

// added WebWorkerService with defaults
// WebWorkerService.TaskPool defaults: MaxPoolSize == 1, PoolSize == 0 (starts the TaskPool Worker when first requested)
builder.Services.AddWebWorkerService();

// add services used by unit tests
builder.Services.AddSingleton(builder.Configuration); // used to demo appsettings reading in workers
builder.Services.AddSingleton<IMathsService, MathsService>();
builder.Services.AddKeyedSingleton<ITestService2>("apples", (_, key) => new TestService2((string)key!));
builder.Services.AddKeyedSingleton<ITestService2>("bananas", (_, key) => new TestService2((string)key!));
builder.Services.AddSingleton<AsyncCallDispatcherTest>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// add service(s) that holds unit tests
builder.Services.AddSingleton<UnitTestsService>();

// add root elements if running in the window
if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

// start the app using BlazorJSRunAsync
// allows proper startup in non-window scopes
await builder.Build().BlazorJSRunAsync();