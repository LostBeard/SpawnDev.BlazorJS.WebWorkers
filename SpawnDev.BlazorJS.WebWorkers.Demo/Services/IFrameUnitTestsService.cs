using SpawnDev.Blazor.UnitTesting;

namespace SpawnDev.BlazorJS.WebWorkers.Demo.Services
{
    public class IFrameUnitTestsService(BlazorJSRuntime JS, WebWorkerService WebWorkerService, IMathsService MathService, AsyncCallDispatcherTest CallDispatcherBaseTestClass)
    {


        [TestMethod]
        public async Task WebWorkerAppSettingsReadTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            // test key to read from appsettings.json
            var testKey = "Message";
            // get value loaded in this context to compare to worker returned value
            var testValueWindow = await MathService.ReadAppSettingsValue(testKey);
            // get worker and read the value from that context
            var worker = await WebWorkerService.GetIFrameWorker();
            var mathService = worker!.GetService<IMathsService>();
            var testValueWorker = await mathService.ReadAppSettingsValue(testKey);
            // compare
            if (testValueWorker != testValueWindow) throw new Exception("Unexpected result");
            Console.WriteLine($"WebWorkerAppSettingsReadTest value: {testValueWorker}");
        }
    }
}
