using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.WebWorkers;
using SpawnDev.BlazorJS.WebWorkers.Demo;
using SpawnDev.BlazorJS.WebWorkers.Demo.Services;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;


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

builder.Services.AddSingleton<TestClass2>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

var host = await builder.Build().StartBackgroundServices();

var webWorkerService = host.Services.GetRequiredService<WebWorkerService>();

JS.Set("_test", async (bool useModule) =>
{
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
JS.Set("_run", async () =>
{
    try
    {
        await RunIt();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
});

static void Log(string msg)
{
    Console.WriteLine(msg);
}

async Task RunIt()
{
    var bytes = new byte[] { 1, 2, 3, 4, 5 };
    var bytes2 = new byte[] { 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7 };

    if (true)
    {
        Log($"MyClass with WorkerTransfer...");

        // process the ArrayBuffer in a worker with using WorkerTransfer
        // here for comparison purposes
        var sw = Stopwatch.StartNew();

        // get bytes as a Uint8Array
        using var uint8Array = new Uint8Array(bytes);
        using var offscreen1 = new OffscreenCanvas(64, 64);

        var myClass = new MyClass
        {
            MySubClass = new MySubClass
            {
                Uint8Array = uint8Array,
                OffscreenCanvasArray = new[] { offscreen1 },
            }
        };

        //  get the underlying ArrayBuffer
        using var arrayBufferOrig = uint8Array.Buffer;

        var arrayBufferReturned1 = await webWorkerService.TaskPool.Run<TestClass2, MyClass>((tc) => tc.ProcessFrameMyClass(myClass));

        // arrayBufferOrig is detached (indicates it was transferred to the worker)
        Log($"ArrayBuffer is detached: {arrayBufferOrig.Detached}");

        // arrayBufferOrig is detached (indicates it was transferred to the worker)
        var detached = offscreen1.Width == 0 && offscreen1.Height == 0;
        Log($"OffscreenCanvas is detached: {detached}");

        // pull back into .Net so it more fairly compares to the byte[] method
        var bytesReadBack = arrayBufferReturned1.MySubClass.Uint8Array.ReadBytes();

        sw.Stop();
        Log($"Processed with WorkerTransfer {bytesReadBack.Length} bytes in {sw.ElapsedMilliseconds} ms\n");
    }
    return;

    if (true)
    {
        Log($"ArrayBuffer with WorkerTransfer...");

        // process the ArrayBuffer in a worker with using WorkerTransfer
        // here for comparison purposes
        var sw = Stopwatch.StartNew();

        // get bytes as a Uint8Array
        using var uint8Array = new Uint8Array(bytes);

        //  get the underlying ArrayBuffer
        using var arrayBufferOrig = uint8Array.Buffer;

        using var arrayBufferReturned1 = await webWorkerService.TaskPool.Run<TestClass2, ArrayBuffer>((tc) => tc.ProcessFrame(uint8Array));

        // arrayBufferOrig is detached (indicates it was transferred to the worker)
        Log($"ArrayBuffer is detached: {arrayBufferOrig.Detached}");

        // pull back into .Net so it more fairly compares to the byte[] method
        var bytesReadBack = arrayBufferReturned1.ReadBytes();

        sw.Stop();
        Log($"Processed with WorkerTransfer {arrayBufferReturned1.ByteLength} bytes in {sw.ElapsedMilliseconds} ms\n");
    }

    {
        Log($"Dictionary<string, Uint8Array> without WorkerTransfer...");

        // process the ArrayBuffer in a worker without using WorkerTransfer
        // here for comparison purposes
        var sw = Stopwatch.StartNew();

        // get bytes as a Uint8Array
        using var uint8Array = new Uint8Array(bytes);


        using var uint8Array2 = new Uint8Array(bytes);

        //  get the underlying ArrayBuffer
        using var arrayBufferOrig = uint8Array.Buffer;

        //  get the underlying ArrayBuffer
        using var arrayBufferOrig2 = uint8Array2.Buffer;

        var args1 = new List<Uint8Array>
        {
            uint8Array,
            uint8Array2
        };

        var arrayBufferReturned1 = await webWorkerService.TaskPool.Run<TestClass2, List<Uint8Array>>((tc) => tc.ProcessFrameList(args1));

        // arrayBufferOrig is detached (indicates it was transferred to the worker)
        Log($"ArrayBuffer is detached: {arrayBufferOrig.Detached}");

        // pull back into .Net so it more fairly compares to the byte[] method
        var bytesReadBack = arrayBufferReturned1.First().ReadBytes();

        sw.Stop();
        Log($"Processed with WorkerTransfer {bytesReadBack.Length} bytes in {sw.ElapsedMilliseconds} ms\n");
    }

    {
        Log($"Dictionary<string, Uint8Array> without WorkerTransfer...");

        // process the ArrayBuffer in a worker without using WorkerTransfer
        // here for comparison purposes
        var sw = Stopwatch.StartNew();

        // get bytes as a Uint8Array
        using var offscreen1 = new OffscreenCanvas(64, 64);

        using var offscreen2 = new OffscreenCanvas(128, 128);

        var args1 = new List<OffscreenCanvas>
        {
            offscreen1,
            offscreen2
        };

        var arrayBufferReturned1 = await webWorkerService.TaskPool.Run<TestClass2, List<OffscreenCanvas>>((tc) => tc.ProcessFrameList(args1));

        // arrayBufferOrig is detached (indicates it was transferred to the worker)
        var detached = offscreen1.Width == 0 && offscreen1.Height == 0;
        Log($"OffscreenCanvas is detached: {detached}");

        // pull back into .Net so it more fairly compares to the byte[] method
        var bytesReadBack = arrayBufferReturned1.First();

        sw.Stop();
        Log($"Processed with WorkerTransfer {bytesReadBack.Width}x{bytesReadBack.Height} WxH in {sw.ElapsedMilliseconds} ms\n");
    }

}

//var arg = new SharedCancellationTokenSource();
//var token = arg.Token;

//JS.Set("_token1", token);
//var token12 = JS.Get<JSObject>("_token1");
//var keys = token12.JSRef?.Keys();
//var token1 = JS.Get<SharedCancellationToken>("_token1");

await host.BlazorJSRunAsync();

public class MyClass
{
    public MySubClass MySubClass { get; set; }

}
public class MySubClass
{
    /// <summary>
    /// If WorkerTransfer.TransferAll is used, this Uint8Array's underlying ArrayBuffer will be transferred to the worker since the transferable ArrayBuffer is at a property depth of 3 (0 MyClass -> 1 MySubClass -> 2 Uint8Array -> 3 ArrayBuffer)
    /// </summary>
    public Uint8Array Uint8Array { get; set; }
    /// <summary>
    /// The default WorkerTrasnfer attribute will be able to handle the OffscreenCanvas array since the transferable value is at a property depth of 3 (0 MyClass -> 1 MySubClass -> 2 Offscreencanvas[] -> 3 OffscreenCanvas)
    /// </summary>
    public OffscreenCanvas[] OffscreenCanvasArray { get; set; }
}

public class TestClass2
{
    /// <summary>
    /// Tests processing MyClass with the default fallback WorkerTransfer attribute which transfers all required transferables at a property depth of 3
    /// </summary>
    /// <param name="myClass"></param>
    /// <returns></returns>
    public async Task<MyClass> ProcessFrameMyClass([WorkerTransfer(WorkerTransferMode.TransferAll)] MyClass myClass)
    {
        // read in the data and write back out so it compares more equally with the other methods
        byte[] data = myClass.MySubClass.Uint8Array.ReadBytes();
        ArrayBuffer retunrnedArrayBuffer = new Uint8Array(data).Buffer;
        Console.WriteLine($"Processing MyClass with WorkerTransfer {data.Length} bytes in worker");

        return myClass;
    }
    [return: WorkerTransfer]
    public async Task<ArrayBuffer> ProcessFrame([WorkerTransfer] Uint8Array arrayBuffer)
    {
        // read in the data and write back out so it compares more equally with the other methods
        byte[] data = arrayBuffer.ReadBytes();
        ArrayBuffer retunrnedArrayBuffer = new Uint8Array(data).Buffer;
        Console.WriteLine($"Processing ArrayBuffer with WorkerTransfer {data.Length} bytes in worker");

        return retunrnedArrayBuffer;
    }
    [return: WorkerTransfer]
    public async Task<List<Uint8Array>> ProcessFrameList([WorkerTransfer] List<Uint8Array> frame)
    {
        // read in the data and write back out so it compares more equally with the other methods
        byte[] data = frame.First().ReadBytes();
        ArrayBuffer retunrnedArrayBuffer = new Uint8Array(data).Buffer;
        Console.WriteLine($"Processing List with WorkerTransfer {data.Length} bytes in worker");

        return frame;
    }
    public async Task<List<OffscreenCanvas>> ProcessFrameList(List<OffscreenCanvas> frame)
    {
        // read in the data and write back out so it compares more equally with the other methods

        Console.WriteLine($"Processing OffscreenCanvas {frame.First().Width}x{frame.First().Height} WxH in worker");

        return frame;
    }
    public async Task<OffscreenCanvas> ProcessFrameList(OffscreenCanvas frame)
    {
        // read in the data and write back out so it compares more equally with the other methods

        Console.WriteLine($"Processing OffscreenCanvas {frame.Width}x{frame.Height} WxH in worker");

        return frame;
    }
}

class Circlular3
{
    public Circlular2 Parent { get; set; }
    public Circlular1 Circlular1 { get; set; }
    public Uint8Array Uint8Array1 { get; set; }
}
class Circlular2
{
    public Circlular2 Parent { get; set; }
    public Circlular1 Circlular1 { get; set; }
    public Uint8Array Uint8Array1 { get; set; }
}
class Circlular1
{
    public Circlular3 Circlular3 { get; set; }
    public Circlular2 Circlular2 { get; set; }
    public OffscreenCanvas OffscreenCanvas1 { get; set; }
}