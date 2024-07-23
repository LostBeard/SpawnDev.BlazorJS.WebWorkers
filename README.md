
# SpawnDev.BlazorJS.WebWorkers
[![NuGet](https://img.shields.io/nuget/dt/SpawnDev.BlazorJS.WebWorkers.svg?label=SpawnDev.BlazorJS.WebWorkers)](https://www.nuget.org/packages/SpawnDev.BlazorJS.WebWorkers) 

- Easily call Blazor Services in separate threads with WebWorkers and SharedWebWorkers 
- Call services in other Windows
- Works in Blazor WASM .Net 6, 7, and 8.
- SharedArrayBuffer is not required, so no special HTTP headers to configure.
- Supports and uses transferable objects whenever possible
- Run Blazor WASM in a Service Worker

[Live Demo](https://blazorjs.spawndev.com/)  

### Supported .Net Versions
- Blazor WebAssembly .Net 6, 7, and 8 
- - Tested VS Template: Blazor WebAssembly Standalone App
- Blazor United .Net 8 (in WebAssembly project only) 
- - Tested VS Template: Blazor Web App (Interactive WebAssembly mode without prerendering)

Tested working in the following browsers (tested with .Net 8.) Chrome Android does not currently support SharedWorkers. 

| Browser         | WebWorker Status | SharedWebWorker Status |
|-----------------|------------------|------------------------|
| Chrome          | ✔ | ✔ |
| MS Edge         | ✔ | ✔ |
| Firefox         | ✔ | ✔ | 
| Chrome Android  | ✔ | ❌ (SharedWorker not supported by browser) |
| MS Edge Android | ✔ | ❌ (SharedWorker not supported by browser) |
| Firefox Android | ✔ | ✔ | 

Issues can be reported [here](https://github.com/LostBeard/SpawnDev.BlazorJS.WebWorkers/issues) on GitHub.

## WebWorkerService
The WebWorkerService singleton contains many methods for working with multiple instances of your Blazor app running in any scope, whether Window, Worker, SharedWorker, or ServiceWorker. 

### Primary WebWorkerService members:
- **Info** - This property provides basic info about the currently running instance, like instance id and the global scope type.
- [**TaskPool**](#webworkerservicetaskpool) - This WebWorkerPool property gives quick and easy access to any number of Blazor instances running in dedicated worker threads. Access your services in separate threads. TaskPool threads can be started at startup or set to start as needed.
- [**WindowTask**](#webworkerservicewindowtask) - If the current scope is a Window it dispatches on the current scope. If the current scope is a WebWorker and its parent is a Window it will dispatch on the parent Window's scope. Only available in a Window context, or in a WebWorker created by a Window.
- [**Instances**](#webworkerserviceinstances) - This property gives access to every running instance of your Blazor App in the active browser. This includes every scope including other Windows, Workers, SharedWorkers, and ServiceWorkers. Call directly into any running instance from any instance.
- [**Locks**](#webworkerservicelocks) - This property is an instance of LockManager acquired from Navigator.Locks. LockManager provides access to cross-thread locks in all browser scopes. Locks work in a very similar manner to the .Net Mutex.
- [**GetWebWorker**](#webworker) - This async method creates and returns a new instance of WebWorker when it is ready.
- [**GetSharedWebWorker**](#sharedwebworker) - This async method returns an instance of SharedWebWorker with the given name, accessible by all Blazor instances. The worker is created the if it does not already exist. 

### Notes and Common Issues
WebWorkers are separate instances of your Blazor WASM app running in [Workers](https://developer.mozilla.org/en-US/docs/Web/API/Worker). These instances are called into using [postMessage](https://developer.mozilla.org/en-US/docs/Web/API/Worker/postMessage).

#### Developer console shows blazor startup message more than once.
Workers share the window's console. Startup messages and other console messages from them is normal.

#### Missing Javascript dependencies in WebWorkers
See [Javascript dependencies in WebWorkers](#javascript-dependencies-in-webworkers)

#### Serialization and WebWorkers
Communication with WebWorkers is done using [postMessage](https://developer.mozilla.org/en-US/docs/Web/API/Worker/postMessage). Because postMessage is a Javascript method, the data passed to it will be serialized and deserialized using the JSRuntime serializer. Not all .Net types are supported by the JSRuntime serializer so calling methods with unsupported parameter or return types will throw an exception.

### Example WebWorkerService setup and usage. 

Program.cs  
```cs
// ...
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.WebWorkers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
// Add SpawnDev.BlazorJS.BlazorJSRuntime
builder.Services.AddBlazorJSRuntime();
// Add SpawnDev.BlazorJS.WebWorkers.WebWorkerService
builder.Services.AddWebWorkerService(webWorkerService =>
{
    // Optionally configure the WebWorkerService service before it is used
    // Default WebWorkerService.TaskPool settings: PoolSize = 0, MaxPoolSize = 1, AutoGrow = true
    // Below sets TaskPool max size to 2. By default the TaskPool size will grow as needed up to the max pool size.
    // Setting max pool size to -1 will set it to the value of navigator.hardwareConcurrency
    webWorkerService.TaskPool.MaxPoolSize = 2;
    // Below is telling the WebWorkerService TaskPool to set the initial size to 2 if running in a Window scope and 0 otherwise
    // This starts up 2 WebWorkers to handle TaskPool tasks as needed
    webWorkerService.TaskPool.PoolSize = webWorkerService.GlobalScope == GlobalScope.Window ? 2 : 0;
});
// Other misc. services
builder.Services.AddSingleton<IMathService, MathService>();
builder.Services.AddScoped((sp) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
// ...

// build and Init using BlazorJSRunAsync (instead of RunAsync)
await builder.Build().BlazorJSRunAsync();
```

## AsyncCallDispatcher
**AsyncCallDispatcher** is the base class used for accessing other instances of Blazor. **AsyncCallDispatcher** provides a few different calling conventions for instance to instance communication.

Where **AsyncCallDispatcher** is used:  
- The classes **AppInstance**, [**WebWorker**](#webworker), [**SharedWebWorker**](#sharedwebworker), and **WebWorkerPool** all inherit from **AsyncCallDispatcher**.
- [**WebWorkerService.TaskPool**](#webworkerservicetaskpool) - an instance of **WebWorkerPool**, which inherits from **AsyncCallDispatcher**
- **[WebWorkerService.WindowTask](#webworkerservicewindowtask)** - an instance of **AsyncCallDispatcher**
- [**WebWorkerService.Instances**](#webworkerserviceinstances) - a **List&lt;AppInstance&gt;**. **AppInstance** inherits from **AsyncCallDispatcher**  

### Supported Instance To Instance Calling Conventions

**Expressions** - Run(), Set()  
- Supports generics, property get and set, asynchronous and synchronous method calls.
- Supports calling private methods from inside the owning class.

**Delegates** - Invoke()  
- Supports generics, asynchronous and synchronous method calls.  
- Supports calling private methods from inside the owning class.

**Interface proxy** - GetService()
- Supports generics, and asynchronous method calls. (uses DispatchProxy)  
- Does not support static methods, private methods, synchronous calls, or properties.
- Requires services to be registered using an interface.

WebWorkerService.TaskPool Example that demonstrates using **AsyncCallDispatcher's** Expression, Delegate, and Interface proxy invokers to call service methods in a TaskPool WebWorker.

```cs
public interface IMyService
{
    Task<string> WorkerMethodAsync(string input);
}
public class MyService : IMyService
{
    WebWorkerService WebWorkerService;
    public MyService(WebWorkerService webWorkerService)
    {
        WebWorkerService = webWorkerService;
    }
    private string WorkerMethod(string input)
    {
        return $"Hello {input} from {WebWorkerService.InstanceId}";
    }
    public async Task<string> WorkerMethodAsync(string input)
    {
        return $"Hello {input} from {WebWorkerService.InstanceId}";
    }
    public async Task CallWorkerMethod()
    {
        // Call the private method WorkerMethod on this scope (normal)
        Console.WriteLine(WorkerMethod(WebWorkerService.InstanceId));

        // Call a private synchronous method in a WebWorker thread using a Delegate
        Console.WriteLine(await WebWorkerService.TaskPool.Invoke(WorkerMethod, WebWorkerService.InstanceId));

        // Call a private synchronous method in a WebWorker thread using an Expression
        Console.WriteLine(await WebWorkerService.TaskPool.Run(() => WorkerMethod(WebWorkerService.InstanceId)));

        // Call a public async method in a WebWorker thread using an Expression, specifying the registered service to call via a Type parameter (as well as the return type)
        Console.WriteLine(await worker.Run<IMyService, string>(myService => myService.WorkerMethodAsync(WebWorkerService.InstanceId)));

        // Call a public async method in a WebWorker thread using am Interface Proxy
        var service = WebWorkerService.TaskPool.GetService<IMyService>();
        Console.WriteLine(await service.WorkerMethodAsync(WebWorkerService.InstanceId));
    }
}
```

## WebWorkerService.TaskPool
WebWorkerService.TaskPool is ready to call any registered service in a background thread. If WebWorkers are not supported, TaskPool calls will run in the Window scope. The TaskPool settings can be configured when calling AddWebWorkerService(). By default, no worker tasks are started automatically at startup and the max pool size is set to 1. See above example.

## WebWorkerService.Instances
WebWorkerService.Instances is a **List&lt;AppInstance&gt;** where each item represents a running instance. The **AppInstance** class provides some basic information about the running Blazor instance and also allows calling into the instance via its base class [**AsyncCallDispatcher**]($asynccalldispatcher)

The below example iterates all running window instances, reads a service proeprty, and calls a method in that instance.  
```cs
// below gets an instance of AppInstance for each instance running in a Window global scope
var windowInstances = WebWorkerService.Instances.Where(o => o.Info.Scope == GlobalScope.Window).ToList();
var localInstanceId = WebWorkerService.InstanceId;
foreach (var windowInstance in windowInstances)
{
    // below line is an example of how to read a property from another instance
    // here we are reading the BlazorJSRuntime service's InstanceId property from a window instance
    var remoteInstanceId = await windowInstance!.Run(() => JS.InstanceId);
    // below line is an example of how to call a method (here, the static method Console.WriteLine) in another instance
    await windowInstance.Run(() => Console.WriteLine("Hello " + remoteInstanceId + " from " + localInstanceId));
}
```

## WebWorkerService.WindowTask
Sometimes WebWorkers may need to call back into the Window thread that owns them. This can easily be achieved using WebWorkerService.WindowTask. If the current scope is a Window it dispatches on the current scope. If the current scope is a WebWorker and its parent is a Window it will dispatch on the parent Window's scope. Only available in a Window context, or in a WebWorker created by a Window.

```cs
public class MyService
{
    WebWorkerService WebWorkerService;
    public MyService(WebWorkerService webWorkerService)
    {
        WebWorkerService = webWorkerService;
    }
    string CalledOnWindow(string input)
    {
        return $"Hello {input} from {WebWorkerService.InstanceId}";
    }
    public async Task StartedInWorker()
    {   
        // Do some work ...         
        // report back to Window (Expression example)
        // Call the private method CalledOnWindow on the Window thread using an Expression
        Console.WriteLine(await WebWorkerService.WindowTask.Run(() => CalledOnWindow(WebWorkerService.InstanceId)));

        // Do some more work ...         
        // report back to Window again (Delegate example)
        // Call the private method CalledOnWindow on the Window thread using a Delegate
        Console.WriteLine(await WebWorkerService.WindowTask.Invoke(CalledOnWindow, WebWorkerService.InstanceId));
    }
}
```

### Using SharedCancellationToken to cancel a WebWorker task

As of version 2.2.91 ```SharedCancellationToken``` is a supported parameter type and can be used to cancel a running task. SharedCancellationToken works in a similar way to CancellationToken. SharedCancellationTokenSource works in a similar way to CancellationTokenSource.

```cs
public async Task WebWorkerSharedCancellationTokenTest()
{
    if (!WebWorkerService.WebWorkerSupported)
    {
        throw new Exception("Worker not supported by browser. Expected failure.");
    }
    // Cancel the task after 2 seconds
    using var cts = new SharedCancellationTokenSource(2000);
    var i = await WebWorkerService.TaskPool.Run(() => CancellableMethod(10000, cts.Token));
    if (i == -1) throw new Exception("Task Cancellation failed");
}

// Returns -1 if not cancelled
// This method will run for 10 seconds if not cancelled
private static async Task<long> CancellableMethod(double maxRunTimeMS, SharedCancellationToken token)
{
    var startTime = DateTime.Now;
    var maxRunTime = TimeSpan.FromMilliseconds(maxRunTimeMS);
    long i = 0;
    while (DateTime.Now - startTime < maxRunTime)
    {
        // do some work ...
        i += 1;
        // check if cancelled message received
        if (token.IsCancellationRequested) return i;
    }
    return -1;
}
```

#### Limitation: SharedCancellationToken requires cross-origin isolation
```SharedCancellationToken``` and ```SharedCancellationTokenSource``` use a ```SharedArrayBuffer``` for signaling instead of postMessage like ```CancellationToken``` uses. This adds the benefit of working in both synchronous and asynchronous methods. However, they have their own limitation of requiring a cross-origin isolation due to ```SharedArrayBuffer``` restrictions.

### Using CancellationToken to cancel a WebWorker task

As of version 2.2.88 ```CancellationToken``` is a supported parameter type and can be used to cancel a running task.  

```cs
public async Task TaskPoolExpressionWithCancellationTokenTest2()
{
    if (!WebWorkerService.WebWorkerSupported)
    {
        throw new Exception("Worker not supported by browser. Expected failure.");
    }
    // Cancel the task after 2 seconds
    using var cts = new CancellationTokenSource(2000);
    var cancelled = await WebWorkerService.TaskPool.Run(() => CancellableMethod(10000, cts.Token));
    if (!cancelled) throw new Exception("Task Cancellation failed");
}

// Returns true if cancelled  
// This method will run for 10 seconds if not cancelled  
private static async Task<bool> CancellableMethod(double maxRunTimeMS, CancellationToken token)
{
    var startTime = DateTime.Now;
    var maxRunTime = TimeSpan.FromMilliseconds(maxRunTimeMS);
    while (DateTime.Now - startTime < maxRunTime)
    {
        // do some work ...
        await Task.Delay(50);
        // check if cancelled message received
        if (await token.IsCancellationRequestedAsync()) return true;
    }
    return false;
}
```

#### Limitation: CancellationToken requires the receiving method to be async
When a CancellationTokenSource cancels a token that has been passed to a WebWorker a postMessage is sent to the WebWorker(s) to notify them and they call cancel on their instance of a CancellationTokenSource. The problem, is that this requires the method that uses the CancellationToken allows the message event handler time to receive the cancellation message by yielding the thread briefly (```await Task.Delay(1)```) before rechecking if the CancellationToken is cancelled. The extension methods ```CancellationToken.IsCancellationRequestedAsync()``` and ```CancellationToken.ThrowIfCancellationRequestedAsync()``` do this automatically internally. Therefore, CancellationToken will not work in a synchronous method as the message event will never receive the cancellation message. SharedCancellationToken does not have this limitation.

## WebWorkerService.Locks
**WebWorkerService.Locks** is an instance of [LockManager](https://developer.mozilla.org/en-US/docs/Web/API/LockManager) acquired from  ```navigator.locks```. The MDN documentation for [LockManager](https://developer.mozilla.org/en-US/docs/Web/API/LockManager) is explains the interface and has examples.

From MDN [LockManager.request()](https://developer.mozilla.org/en-US/docs/Web/API/LockManager/request)
> The request() method of the LockManager interface requests a Lock object with parameters specifying its name and characteristics. The requested Lock is passed to a callback, while the function itself returns a Promise that resolves (or rejects) with the result of the callback after the lock is released, or rejects if the request is aborted.
>
> The mode property of the options parameter may be either "exclusive" or "shared".
>
> Request an "exclusive" lock when it should only be held by one code instance at a time. This applies to code in both tabs and workers. Use this to represent mutually exclusive access to a resource. When an "exclusive" lock for a given name is held, no other lock with the same name can be held.
>
> Request a "shared" lock when multiple instances of the code can share access to a resource. When a "shared" lock for a given name is held, other "shared" locks for the same name can be granted, but no "exclusive" locks with that name can be held or granted.
>
> This shared/exclusive lock pattern is common in database transaction architecture, for example to allow multiple simultaneous readers (each requests a "shared" lock) but only one writer (a single "exclusive" lock). This is known as the readers-writer pattern. In the IndexedDB API, this is exposed as "readonly" and "readwrite" transactions which have the same semantics.

### WebWorkerService.Locks.Request()

Locks.RequestHandle() example:  
```cs
public async Task SynchronizeDatabase()
{
    JS.Log("requesting lock");
    await WebWorkerService.Locks.Request("my_lock", async (lockInfo) =>
    {
        // because this is an exclusive lock, 
        // the code in this callback will never run in more than 1 thread at a time.
        JS.Log("have lock", lockInfo);
        // simulating async work like synchronizing a browser db to the server
        await Task.Delay(1000);
        // the lock is not released until this async method exits
        JS.Log("releasing lock");
    });
    JS.Log("released lock");
}
```

### WebWorkerService.Locks.RequestHandle()

LockManager.RequestHandle() is an extension method that instead of taking a callback to call when the lock is acquired, it waits for the lock and then returns a TaskCompletionSource that is used to release the lock.

The below example is functionally equivalent to the one above. LockManager.RequestHandle() becomes more useful when a lock needs to be held for an extended period of time.

Locks.RequestHandle() example:  
```cs
public async Task SynchronizeDatabase()
{
    JS.Log("requesting lock");
    TaskCompletionSource tcs = await WebWorkerService.Locks.RequestHandle("my_lock");
    // because this is an exclusive lock,
    // the code between here and 'tcs.SetResult();' will never run in more than 1 thread at a time.
    JS.Log("have lock");
    // simulating async work like synchronizing a browser db to the server
    await Task.Delay(1000);
    // the lock is not released until this async method exits
    JS.Log("releasing lock");
    tcs.SetResult();
    JS.Log("released lock");
}
```

## WebWorker
You can use the properties ```WebWorkerService.SharedWebWorkerSupported``` and ```WebWorkerService.WebWorkerSupported``` to check for support. 

For a simple fallback when not supported:  
- If WebWorkerService.GetWebWorker() returns a WebWorker, use WebWorker.GetService\<T\>().   
- If WebWorkerService.GetWebWorker() returns a null, use IServiceProvider.GetService\<T\>().

Example component code that uses a service (IMyService) in a WebWorker if supported and in the default Window context if not.
```cs
[Inject]
WebWorkerService workerService { get; set; }

[Inject]
IServiceProvider serviceProvider { get; set; }

// MyServiceAuto will be IMyService running in the WebWorker context if available and IMyService running in the Window context if not
IMyService MyService { get; set; }

WebWorker? webWorker { get; set; }

protected override async Task OnInitializedAsync()
{
    // GetWebWorker() will return null if workerService.WebWorkerSupported == false
    webWorker = await workerService.GetWebWorker();
    // get the WebWorker's service instance if available or this Window's service instance if not
    MyService = webWorker != null ? webWorker.GetService<IMyService>() : serviceProvider.GetService<IMyService>();
    await base.OnInitializedAsync();
}
```

Another example with a progress callback.
```cs

// Create a WebWorker

[Inject]
WebWorkerService workerService { get; set; }
 
 // ...

var webWorker = await workerService.GetWebWorker();

// Call GetService<ServiceInterface> on a web worker to get a proxy for the service on the web worker.
// GetService can only be called with Interface types
var workerMathService = webWorker.GetService<IMathService>();

// Call async methods on your worker service 
var result = await workerMathService.CalculatePi(piDecimalPlaces);

// Action types can be passed for progress reporting
var result = await workerMathService.CalculatePiWithActionProgress(piDecimalPlaces, new Action<int>((i) =>
{
    // the worker thread can call this method to report progress if desired
    piProgress = i;
    StateHasChanged();
}));
```

## SharedWebWorker
Calling GetSharedWebWorker in another window with the same sharedWorkerName will return the same SharedWebWorker
```cs
// Create or get SHaredWebWorker with the provided sharedWorkerName
var sharedWebWorker = await workerService.GetSharedWebWorker("workername");

// Just like WebWorker but shared
var workerMathService = sharedWebWorker.GetService<IMathService>();

// Call async methods on your shared worker service
var result = await workerMathService.CalculatePi(piDecimalPlaces);
```

## Send events
```cs
// Optionally listen for event messages
worker.OnMessage += (sender, msg) =>
{
    if (msg.TargetName == "progress")
    {
        PiProgress msgData = msg.GetData<PiProgress>();
        piProgress = msgData.Progress;
        StateHasChanged();
    }
};

// From SharedWebWorker or WebWorker threads, send an event to connected parent(s)
workerService.SendEventToParents("progress", new PiProgress { Progress = piProgress });

// Or on send an event to a connected worker
webWorker.SendEvent("progress", new PiProgress { Progress = piProgress });
```

## Worker Transferable JSObjects

[Faster is better.](https://developer.chrome.com/blog/transferable-objects-lightning-fast/) SpawnDev WebWorkers use [transferable objects](https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API/Transferable_objects) by default for better performance, but it can be disabled with WorkerTransferAttribute. Setting WorkerTransfer to false will cause the property, return value, or parameter to be copied to the receiving thread instead of transferred.


Example
```cs
public class ProcessFrameResult : IDisposable
{
    [WorkerTransfer(false)]
    public ArrayBuffer? ArrayBuffer { get; set; }
    public byte[]? HomographyBytes { get; set; }
    public void Dispose(){
        ArrayBuffer?.Dispose();
    }
}

[return: WorkerTransfer(false)]
public async Task<ProcessFrameResult?> ProcessFrame([WorkerTransfer(false)] ArrayBuffer? frameBuffer, int width, int height, int _canny0, int _canny1, double _needlePatternSize)
{
    var ret = new ProcessFrameResult();
    // ...
    return ret;
}
```

In the above example; the WorkerTransferAttribute on the return type set to false will prevent all properties of the return type from being transferred.

### Transferable JSObject types. Source [MDN](https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API/Transferable_objects#supported_objects)
ArrayBuffer  
MessagePort  
ReadableStream  
WritableStream  
TransformStream  
AudioData  
ImageBitmap  
VideoFrame  
OffscreenCanvas  
RTCDataChannel  


## Javascript dependencies in WebWorkers
When loading a WebWorker, SpawnDev.BlazorJS.WebWorkers ignores all &lt;script&gt; tags in index.html except those marked with the attribute "webworker-enabled". If those scripts will be referenced in workers add the attribute ```webworker-enabled```. This applies to both inline and external scripts.

index.html (partial view)  
```html
    <script webworker-enabled>
        console.log('This script will run in all scopes including window and worker due to the webworker-enabled attribute');
    </script>
    <script>
        console.log('This script will only run in a window scope');
    </script>
```

## ServiceWorker
SpawnDev.BlazorJS.WebWorkers supports running in a ServiceWorker. It is as simple as registering a class to run in the ServiceWorker to handle ServiceWorker events.

#### wwwroot/index.html
Remove the serviceWorker registration from `index.html` (default for PWA Blazor WASM apps). SpawnDev.BlazorJS.WebWorkers will register the service worker on its own when called in the `Program.cs`.
  
Delete below line (if found) in `index.html`:  
`<script>navigator.serviceWorker.register('service-worker.js');</script>`

### Program.cs
A minimal Program.cs
```cs
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// SpawnDev.BlazorJS
builder.Services.AddBlazorJSRuntime();

// SpawnDev.BlazorJS.WebWorkers
builder.Services.AddWebWorkerService();

// Register a ServiceWorker handler (AppServiceWorker here) that inherits from ServiceWorkerEventHandler
builder.Services.RegisterServiceWorker<AppServiceWorker>();

// Or Unregister the ServiceWorker if no longer desired
//builder.Services.UnregisterServiceWorker();

// SpawnDev.BlazorJS startup (replaces RunAsync())
await builder.Build().BlazorJSRunAsync();
```

### AppServiceWorker.cs
A verbose service worker implementation example.
- Handle ServiceWorker events by overriding the ServiceWorkerEventHandler base class virtual methods.
- The ServiceWorker event handlers are only called when running in a ServiceWorkerGlobalScope context.
- The AppServiceWorker singleton may be started in any scope and therefore must be scope aware. (For example, do not try to use localStorage in a Worker scope.)
```cs
public class AppServiceWorker : ServiceWorkerEventHandler
{
    public AppServiceWorker(BlazorJSRuntime js) : base(js)
    {

    }

    // called before any ServiceWorker events are handled
    protected override async Task OnInitializedAsync()
    {
        // This service may start in any scope. This will be called before the app runs.
        // If JS.IsWindow == true be careful not stall here.
        // you can do initialization based on the scope that is running
        Log("GlobalThisTypeName", JS.GlobalThisTypeName);
    }

    protected override async Task ServiceWorker_OnInstallAsync(ExtendableEvent e)
    {
        Log($"ServiceWorker_OnInstallAsync");
        _ = ServiceWorkerThis!.SkipWaiting();   // returned task can be ignored
    }

    protected override async Task ServiceWorker_OnActivateAsync(ExtendableEvent e)
    {
        Log($"ServiceWorker_OnActivateAsync");
        await ServiceWorkerThis!.Clients.Claim();
    }

    protected override async Task<Response> ServiceWorker_OnFetchAsync(FetchEvent e)
    {
        Log($"ServiceWorker_OnFetchAsync", e.Request.Method, e.Request.Url);
        Response ret;
        try
        {
            ret = await JS.Fetch(e.Request);
        }
        catch (Exception ex)
        {
            ret = new Response(ex.Message, new ResponseOptions { Status = 500, StatusText = ex.Message, Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } } });
            Log($"ServiceWorker_OnFetchAsync failed: {ex.Message}");
        }
        return ret;
    }

    protected override async Task ServiceWorker_OnMessageAsync(ExtendableMessageEvent e)
    {
        Log($"ServiceWorker_OnMessageAsync");
    }

    protected override async Task ServiceWorker_OnPushAsync(PushEvent e)
    {
        Log($"ServiceWorker_OnPushAsync");
    }

    protected override void ServiceWorker_OnPushSubscriptionChange(Event e)
    {
        Log($"ServiceWorker_OnPushSubscriptionChange");
    }

    protected override async Task ServiceWorker_OnSyncAsync(SyncEvent e)
    {
        Log($"ServiceWorker_OnSyncAsync");
    }

    protected override async Task ServiceWorker_OnNotificationCloseAsync(NotificationEvent e)
    {
        Log($"ServiceWorker_OnNotificationCloseAsync");
    }

    protected override async Task ServiceWorker_OnNotificationClickAsync(NotificationEvent e)
    {
        Log($"ServiceWorker_OnNotificationClickAsync");
    }
}
```

#### wwwroot/service-worker.js
The files wwwroot/service-worker.js and wwwroot/service-worker.published.js can be ignored as they will not be used. SpawnDev.BlazorJS.WebWorkers will load its own script instead: `spawndev.blazorjs.webworkers.js`

WARNING: Do not delete the service-worker.js or service-worker.published.js files if you are using the `<ServiceWorkerAssetsManifest>` tag in your project's .csproj file to create an assets manifest for a PWA, or an exception will be thrown when the project builds.

# Blazor Web App compatibility
.Net 8 introduced a new hosting model that allows mixing [Blazor server render mode](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0#interactive-server-side-rendering-interactive-ssr) and [Blazor WebAssembly render mode](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0#client-side-rendering-csr). [Prerendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0#prerendering) was also added to improve initial rendering times. "Prerendering is the process of initially rendering page content on the server without enabling event handlers for rendered controls." 

One of the primary goals of SpawnDev.BlazorJS is to give [Web API](https://developer.mozilla.org/en-US/docs/Web/API) access to Blazor WebAssembly that mirrors Javascript's own Web API. This includes calling conventions. For example, a call that is synchronous in Javascript is synchronous in Blazor, an asynchronous call is asynchronous. To provide that, SpawnDev.BlazorJS requires access to Micorosft's IJSInProcessRuntime and IJSInProcessRuntime is only available in Blazor WebAssembly.


## Compatible ```Blazor Web App``` options:  
Prerendering is not compatible with SpawnDev.BlazorJS because it runs on the server. So we need to let Blazor know that SpawnDev.BlazorJS components must be rendered only with WebAssembly. How this is done depends on your project settings.

### ```Interactive render mode``` - ```Auto (Server and WebAssembly)``` or ```WebAssembly```  

### ```Interactivity location``` - ```Per page/component```  

In the Server project ```App.razor```:  
```html
    <Routes />
```

In WebAssembly pages and components that require SpawnDev.BlazorJS:  
```cs
@rendermode @(new InteractiveWebAssemblyRenderMode(prerender: false))
```
  
### ```Interactivity location``` - ```Global```   
In the Server project ```App.razor```:  
```html
    <Routes @rendermode="new InteractiveWebAssemblyRenderMode(prerender: false)"  />
```

# IDisposable 
NOTE: The above code shows quick examples. Some objects implement IDisposable, such as JSObject, Callback, and IJSInProcessObjectReference types. 

JSObject types will dispose of their IJSInProcessObjectReference object when their finalizer is called if not previously disposed. 

Callback types must be disposed unless created with the Callback.CreateOne method, in which case they will dispose themselves after the first callback. Disposing a Callback prevents it from being called.

IJSInProcessObjectReference does not dispose of interop resources with a finalizer and MUST be disposed when no longer needed. Failing to dispose these will cause memory leaks.

IDisposable objects returned from a WebWorker or SharedWorker service are automatically disposed after the data has been sent to the calling thread.

# Support

Consider sponsoring me to give me more time to work on SpawnDev.BlazorJS.WebWorkers and other open source projects.
[Sponsor](https://github.com/sponsors/LostBeard)

[SpawnDev Discord](https://discord.gg/UkGfHtRdSt)

Buy me a coffee 

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=7QTATH4UGGY9U)

Issues can be reported [here](https://github.com/LostBeard/SpawnDev.BlazorJS.WebWorkers/issues) on GitHub.

SpawnDev.BlazorJS.WebWorkers is inspired by Tewr's BlazorWorker implementation. Thank you!  
https://github.com/Tewr/BlazorWorker

# Demos
BlazorJS and WebWorkers Demo  
https://blazorjs.spawndev.com/

Current site under development using Blazor WASM  
https://www.spawndev.com/  
