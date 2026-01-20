using SpawnDev.Blazor.UnitTesting;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.JSObjects.WebRTC;
using System.Text;

namespace SpawnDev.BlazorJS.WebWorkers.Demo.Services
{
    public class UnitTestsService(BlazorJSRuntime JS, WebWorkerService WebWorkerService, IMathsService MathService, AsyncCallDispatcherTest CallDispatcherBaseTestClass)
    {
        #region WebWorker

        [TestMethod]
        public async Task WebWorkerKeyedServiceTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var worker = await WebWorkerService.GetWebWorker();

            var apples1 = worker!.GetKeyedService<ITestService2>("apples");
            var bananas1 = worker.GetKeyedService<ITestService2>("bananas");

            var apples1Id = await apples1.GetId();
            if (apples1Id != "apples")
            {
                throw new Exception("Test failed");
            }
            var bananas1Id = await bananas1.GetId();
            if (bananas1Id != "bananas")
            {
                throw new Exception("Test failed");
            }
        }

        [TestMethod]
        public async Task WebWorkerCreateInstance()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var worker = await WebWorkerService.GetWebWorker();

            await worker.AddService<ITestService, TestService>();
            var serviceSingleton1 = worker.GetService<ITestService>();
            var serviceSingleton1Id = await serviceSingleton1.GetId();
            Console.WriteLine($"serviceSingleton1Id: {serviceSingleton1Id}");

            await worker.AddKeyedService<ITestService, TestService>("nmt");
            var serviceScoped1 = worker.GetKeyedService<ITestService>("nmt");
            var serviceScoped1Id = await serviceScoped1.GetId();
            Console.WriteLine($"serviceScoped1Id: {serviceScoped1Id}");
            //var serviceTransient1 = await worker.AddService<TestService>();

            //var serviceSingleton2 = worker.GetService<ITestService>();
            // var serviceScoped2 = worker.GetKeyedService<ITestService>("nmt");
            // var serviceScoped2Id = await serviceScoped2.GetId();
            // Console.WriteLine($"serviceScoped2Id: {serviceScoped2Id} ^^^^^^^^^^^^^");
            //var serviceTransient2 = worker.GetService<ITestServiceTransient>();

            // var serviceTransient1Id = await serviceSingleton1.GetId();
            // var serviceSingleton2Id = await serviceSingleton1.GetId();
            // var serviceScoped2Id = await serviceSingleton1.GetId();
            // var serviceTransient2Id = await serviceSingleton1.GetId();
            var nmt = true;
        }

        class NewExpressionTestClass
        {
            BlazorJSRuntime JS => BlazorJSRuntime.JS;
            public string Name { get; private set; }
            public NewExpressionTestClass(string name)
            {
                Name = name;
                Console.WriteLine($"Name: {Name} Scope: {JS.GlobalScope.ToString()}");
            }
        }

        [TestMethod]
        public async Task WebWorkerNewExpression()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var worker = await WebWorkerService.GetWebWorker();
            var nameToSet = "new test";
            var serviceKey = 1;
            await worker!.New(serviceKey, () => new NewExpressionTestClass(nameToSet));
            var name = await worker.Run<NewExpressionTestClass, string>(serviceKey, s => s.Name);
            if (name != nameToSet)
            {
                throw new Exception("Test failed");
            }
            var nmt = true;
        }

        [TestMethod]
        public async Task WebWorkerRemoteClassConstructor()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            //var worker = WebWorkerService.Local;
            using var worker = await WebWorkerService.GetWebWorker();
            await worker.New<ITestService>("myoranges", () => new TestService("myoranges"));
            var tsKeyed = await worker.Run<ITestService, string>("myoranges", s => s.GetId());
            var tsKeyedKey = await worker.Run<ITestService, string>("myoranges", s => s.Key);
            Console.WriteLine($"tsKeyedKey: {tsKeyedKey}");
            if (tsKeyedKey != "myoranges")
            {
                throw new Exception("Run failed");
            }
            var exists1 = await worker.KeyedServiceExists<ITestService>("myoranges");
            if (!exists1)
            {
                throw new Exception("KeyedServiceExists failed");
            }
            var removed = await worker.RemoveKeyedService<ITestService>("myoranges");
            if (!removed)
            {
                throw new Exception("RemoveKeyedService failed");
            }
            var exists2 = await worker.KeyedServiceExists<ITestService>("myoranges");
            if (exists2)
            {
                throw new Exception("KeyedServiceExists failed");
            }
        }
        [TestMethod]
        public async Task WebWorkerRemoteClassConstructorLocal()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            var worker = WebWorkerService.Local;
            //using var worker = await WebWorkerService.GetWebWorker();
            await worker.New<ITestService>("myoranges", () => new TestService("myoranges"));
            var tsKeyed = await worker.Run<ITestService, string>("myoranges", s => s.GetId());
            var tsKeyedKey = await worker.Run<ITestService, string>("myoranges", s => s.Key);
            Console.WriteLine($"tsKeyedKey: {tsKeyedKey}");
            if (tsKeyedKey != "myoranges")
            {
                throw new Exception("Run failed");
            }
            var exists1 = await worker.KeyedServiceExists<ITestService>("myoranges");
            if (!exists1)
            {
                throw new Exception("KeyedServiceExists failed");
            }
            var removed = await worker.RemoveKeyedService<ITestService>("myoranges");
            if (!removed)
            {
                throw new Exception("RemoveKeyedService failed");
            }
            var exists2 = await worker.KeyedServiceExists<ITestService>("myoranges");
            if (exists2)
            {
                throw new Exception("KeyedServiceExists failed");
            }
        }


        [TestMethod]
        public async Task TaskPoolFullTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            await CallDispatcherBaseTestClass.Test(WebWorkerService.TaskPool);
        }

        [TestMethod]
        public async Task TaskPoolExpressionTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            // static methods of any class can be called
            // private static methods must be called from inside the class
            // public static methods can be called from outside the class
            var remoteInstanceId = await WebWorkerService.TaskPool.Run(() => ThisMethodWillGetCalledInAWorker(null!, JS.GlobalThisTypeName, JS.InstanceId));
            Console.WriteLine($"{BlazorJSRuntime.JS.GlobalThisTypeName} {BlazorJSRuntime.JS.InstanceId} {remoteInstanceId}");
        }

        // The below region is used to test Func<> callbacks in WebWorker calls.
        // Currently Func<> is not supported, only Actions are supported
        // When support has been added for Func the below methods can be uncommented to enable testing
        #region Test WebWorker Func callback parameter
        // // *********** Func<> ***********
        // [TestMethod]
        // public async Task TaskPoolExpressionFuncTest()
        // {
        //     if (!WebWorkerService.WebWorkerSupported)
        //     {
        //         throw new UnsupportedTestException("Worker not supported by browser.");
        //     }
        //     var dataString = Guid.NewGuid().ToString();
        //     var callbackFunc = () => dataString;
        //     var dataStringRet = await WebWorkerService.TaskPool.Run(() => ThisMethodWillGetCalledInAWorker(callbackFunc));
        //     if (dataString != dataStringRet)
        //     {
        //         throw new Exception("Func<string> call failed");
        //     }
        // }
        // private static string ThisMethodWillGetCalledInAWorker(Func<string> funcCallback)
        // {
        //     return funcCallback();
        // }
        #endregion

        #region TestWindowTask access of Window scope static property
        [TestMethod]
        public async Task TaskPoolExpressionStaticPropertyReadToCancelTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            CancelWorkers = false;
            // CancelWorkers will be set to true on the window scope after 2000 seconds
            // the WebWorker task will read this property every so often to check if it should continue running
            using var cts = new CancellationTokenSource(2000);
            cts.Token.Register(() => CancelWorkers = true);
            var cancelled = await WebWorkerService.TaskPool.Run(() => ThisMethodWillGetCalledInAWorker(null!, 10000));
            if (!cancelled)
            {
                throw new Exception("Test failed");
            }
        }
        // This property will be set on the Window scope and read by the WebWorker scope
        static bool CancelWorkers { get; set; } = false;

        // Method returns true if it was cancelled, false if it ran until completion
        private static async Task<bool> ThisMethodWillGetCalledInAWorker([FromServices] WebWorkerService webWorkerService, double maxRunTimeMS)
        {
            var startTime = DateTime.Now;
            var maxRunTime = TimeSpan.FromMilliseconds(maxRunTimeMS);
            while (DateTime.Now - startTime < maxRunTime)
            {
                Console.WriteLine("Waiting");
                await Task.Delay(50);
                var cancelled = await webWorkerService.WindowTask!.Run(() => CancelWorkers);
                if (cancelled) return true;
            }
            return false;
        }
        #endregion

        #region Test WebWorker CancellationToken parameter
        // *********** CancellationToken ***********
        [TestMethod]
        public async Task TaskPoolExpressionWithCancellationTokenTest2()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var cts = new CancellationTokenSource(2000);
            var cancelled = await WebWorkerService.TaskPool.Run(() => CancellableMethod(10000, cts.Token));
            if (!cancelled) throw new Exception("Task Cancellation failed");
        }
        // Returns true if cancelled
        private static async Task<bool> CancellableMethod(double maxRunTimeMS, CancellationToken token)
        {
            var startTime = DateTime.Now;
            var maxRunTime = TimeSpan.FromMilliseconds(maxRunTimeMS);
            while (DateTime.Now - startTime < maxRunTime)
            {
                // do some work ...
                // check if cancelled using IsCancellationRequestedAsync extension method
                // internally calls Task.Delay(1) to allow event handlers time to receive a cancel message
                if (await token.IsCancellationRequestedAsync()) return true;
            }
            return false;
        }

        [TestMethod]
        public async Task TaskPoolExpressionWithCancellationTokenTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var cts = new CancellationTokenSource(4000);
            try
            {
                await WebWorkerService.TaskPool.Run(() => ThisMethodWillGetCalledInAWorker(10000, cts.Token));
                Console.WriteLine($"Task Cancellation failed");
            }
            catch
            {
                Console.WriteLine($"Success. The task was cancelled");
                return;
            }
            throw new Exception("Task Cancellation failed");
        }
        [TestMethod]
        public async Task TaskPoolExpressionWithCancellationTokenPreCancelledTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            var cancelledToken = new CancellationToken(true);
            try
            {
                await WebWorkerService.TaskPool.Run(() => ThisMethodWillGetCalledInAWorker(5000, cancelledToken));
                Console.WriteLine($"Task Cancellation failed");
            }
            catch
            {
                Console.WriteLine($"Success. The task was cancelled");
                return;
            }
            throw new Exception("Task Cancellation failed");
        }
        [TestMethod]
        public async Task TaskPoolExpressionWithCancellationTokenNoneTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            await WebWorkerService.TaskPool.Run(() => ThisMethodWillGetCalledInAWorker(2000, CancellationToken.None));
            Console.WriteLine($"Success. The task ran to completion.");
        }
        private static async Task ThisMethodWillGetCalledInAWorker(double maxRunTimeMS, CancellationToken token)
        {
            Console.WriteLine($"maxRunTimeMS: {maxRunTimeMS} token.CanBeCanceled: {token.CanBeCanceled} token.IsCancellationRequested: {token.IsCancellationRequested}");
            var startTime = DateTime.Now;
            var maxRunTime = TimeSpan.FromMilliseconds(maxRunTimeMS);
            while (DateTime.Now - startTime < maxRunTime)
            {
                Console.WriteLine("Waiting");
                // do some fake work
                await Task.Delay(50);
                // check if cancellation message received
                await token.ThrowIfCancellationRequestedAsync();
            }
        }
        [TestMethod]
        public async Task WebWorkerCancellationTokenAlreadyCancelledTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            var methodInfo = typeof(MathsService).GetMethod("CancellationTokenTest");
            using var worker = await WebWorkerService.GetWebWorker();
            var cancelledToken = new CancellationToken(true);
            try
            {
                await worker!.Call(typeof(MathsService), methodInfo!, new object[] { 5000d, cancelledToken });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Success. The task was cancelled: {ex.Message}");
                return;
            }
            throw new Exception("Task Cancellation failed");
        }
        #endregion

        private static string ThisMethodWillGetCalledInAWorker([FromServices] BlazorJSRuntime JS, string callingScope, string instanceId)
        {
            Console.WriteLine($"{JS.GlobalThisTypeName} {JS.InstanceId} {callingScope} {instanceId}");
            return JS.InstanceId;
        }

        /// <summary>
        /// This method tests Exception handling of web worker calls.<br/>
        /// As of version 2.15.0, simple Exception serialization is used to pass exceptions back to the caller in another context.</br>
        /// Some exception types may not be serializable and will be deserialized to a basic Exception.<br/>
        /// Regardless of serialization support, the created exception.Message will contain the Exception.ToString() return value.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [TestMethod]
        public async Task WebWorkerExceptionTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var worker = await WebWorkerService.GetWebWorker();
            try
            {
                // call method that throws a DivideByZeroException exception in the web worker context
                // the exception will be caught by the worker and rethrown to the caller
                var willNotGetResult = await worker!.Run(() => DivideByZero());
            }
            catch (DivideByZeroException ex)
            {
                Console.WriteLine($"WebWorkerExceptionTest expected exception message: {ex.ToString()}");
            }
            catch (Exception ex)
            {
                throw new Exception($"The DivideByZeroException exception should have been received. {ex.GetType().FullName}");
            }
        }

        /// <summary>
        /// This method will throw an exception in the web worker context, which gets passed back to the calling window context as.<br/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DivideByZeroException"></exception>
        static async Task<double> DivideByZero()
        {
            throw new DivideByZeroException();  // this will be caught by the worker and rethrown to the caller
        }

        /// <summary>
        /// This method tests the FileSystemSyncAccessHandle class in a worker because it is not available in a window
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [TestMethod]
        public async Task WebWorkerFileSystemSyncAccessHandleTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var worker = await WebWorkerService.GetWebWorker();
            // call method that throws a DivideByZeroException exception in the web worker context
            // the exception will be caught by the worker and rethrown to the caller
            await worker!.Run(() => _WebWorkerFileSystemSyncAccessHandleTest(null!));
        }
        static async Task _WebWorkerFileSystemSyncAccessHandleTest([FromServices] BlazorJSRuntime JS)
        {
            var testFileName = "test.txt";
            using var navigator = JS.Get<Navigator>("navigator");
            using var dir = await navigator.Storage.GetDirectory();
            try
            {
                await dir.RemoveEntry(testFileName);
            }
            catch { }
            using var file = await dir.GetFileHandle(testFileName, true);
            using var fileS = await file.CreateSyncAccessHandle(new FileSystemSyncAccessOptions { Mode = "readwrite-unsafe" });
            try
            {
                var txt = "Hello world!";
                var txtBytes = Encoding.UTF8.GetBytes(txt);
                var bytesWritten = fileS.Write(txtBytes);
                if (txtBytes.Length != bytesWritten)
                {
                    throw new Exception($"Incorrect bytesWritten value. {bytesWritten} != {txtBytes.Length}");
                }
                fileS.Flush();
                var fileSize = fileS.GetSize();
                if (txtBytes.Length != fileSize)
                {
                    throw new Exception($"Incorrect file size. {fileSize} != {txtBytes.Length}");
                }
                var rbDest = new byte[64];
                var bytesRead = fileS.Read(rbDest, new FileSystemSyncReadWriteOptions { At = 0 });
                JS.Log($"bytesRead: {bytesRead} {txtBytes.Length}");
                var readBackTxt = Encoding.UTF8.GetString(rbDest, 0, (int)bytesRead);
                if (readBackTxt != txt)
                {
                    throw new Exception($"Readback failed: {bytesRead} bytes '{readBackTxt}' != '{txt}'");
                }
            }
            finally
            {
                try
                {
                    fileS.Close();
                    await dir.RemoveEntry(testFileName);
                }
                catch { }
            }
        }

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
            using var worker = await WebWorkerService.GetWebWorker();
            var mathService = worker!.GetService<IMathsService>();
            var testValueWorker = await mathService.ReadAppSettingsValue(testKey);
            // compare
            if (testValueWorker != testValueWindow) throw new Exception("Unexpected result");
            Console.WriteLine($"WebWorkerAppSettingsReadTest value: {testValueWorker}");
        }

        [TestMethod]
        public async Task WebWorkerTestMultiSigMethods()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var worker = await WebWorkerService.GetWebWorker();
            var mathService = worker!.GetService<IMathsService>();
            var strValueOrig = "apples";
            var dblValueOrig = Math.PI;

            // test call IMathsService.TestMultiSigMethod which has 3 signatures total.

            // this call is intended for the only method with this name and 2 arguments.
            var twoArgReadBack = await mathService.TestMultiSigMethod(strValueOrig, dblValueOrig);
            var twoArgReadBackExpected = strValueOrig + " " + dblValueOrig;
            if (twoArgReadBack != twoArgReadBackExpected) throw new Exception("Call to worker TestMultiSigMethod with unique number of arguments failed");

            // test call IMathsService.TestMultiSigMethod which has two signatures with the same number of arguments

            // this call intended for the one with a string argument.
            var strValueReadBack = await mathService.TestMultiSigMethod(strValueOrig);
            if (strValueOrig != strValueReadBack) throw new Exception("Call to worker TestMultiSigMethod with 1 argument failed with string");

            // this call intended for the one with a double argument.
            var dblValueReadBack = await mathService.TestMultiSigMethod(dblValueOrig);
            if (dblValueOrig != dblValueReadBack) throw new Exception("Call to worker TestMultiSigMethod with 1 argument failed with double");
        }

        [TestMethod]
        public async Task WebWorkerGenericsTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var worker = await WebWorkerService.GetWebWorker();
            var mathService = worker!.GetService<IMathsService>();
            var value1 = "apples";
            var value2 = 42;
            var value2ReadBack = await mathService.TestGenerics<string, int>(value1, value2);
            if (value2ReadBack != value2) throw new Exception("Unexpected result");
        }

        [TestMethod]
        public async Task WebWorkerTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var worker = await WebWorkerService.GetWebWorker();
            var mathService = worker!.GetService<IMathsService>();
            var randValue = Guid.NewGuid().ToString();
            await mathService.SetValueTest(randValue);
            var readBack = await mathService.GetValueTest();
            if (readBack != randValue) throw new Exception("Unexpected result");
        }

        [TestMethod]
        public async Task WebWorkersTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            using var workerA = await WebWorkerService.GetWebWorker();
            using var workerB = await WebWorkerService.GetWebWorker();
            var mathServiceA = workerA!.GetService<IMathsService>();
            var mathServiceB = workerB!.GetService<IMathsService>();
            var randValue = Guid.NewGuid().ToString();
            await mathServiceA.SetValueTest(randValue);
            var readBack = await mathServiceB.GetValueTest();
            if (readBack == randValue) throw new Exception("Unexpected result");
        }

        [TestMethod]
        public async Task SharedWebWorkerTest()
        {
            if (!WebWorkerService.SharedWebWorkerSupported)
            {
                throw new UnsupportedTestException("SharedWorker not supported by browser.");
            }

            var thisInstanceId = JS.InstanceId;

            using var worker = await WebWorkerService.GetSharedWebWorker();

            var workerInstanceId = await worker!.Run(() => JS.InstanceId);
            Console.WriteLine($"thisInstanceId: {thisInstanceId}");
            Console.WriteLine($"workerInstanceId: {workerInstanceId}");


            var mathService = worker.GetService<IMathsService>();
            var randValue = Guid.NewGuid().ToString();
            await mathService.SetValueTest(randValue);
            var readBack = await mathService.GetValueTest();
            Console.WriteLine($"SharedWebWorkerTest: {readBack}");
            if (readBack != randValue) throw new Exception("Unexpected result");
        }

        [TestMethod]
        public async Task SharedWebWorkersByName()
        {
            if (!WebWorkerService.SharedWebWorkerSupported)
            {
                throw new UnsupportedTestException("SharedWorker not supported by browser.");
            }
            // workerA1 and workerA2 will refer to the same shared worker
            // workerB is a separate worker instance
            using var workerA1 = await WebWorkerService.GetSharedWebWorker("workerA");
            using var workerA2 = await WebWorkerService.GetSharedWebWorker("workerA");
            using var workerB = await WebWorkerService.GetSharedWebWorker("workerB");
            var mathServiceA1 = workerA1!.GetService<IMathsService>();
            var mathServiceA2 = workerA2!.GetService<IMathsService>();
            var mathServiceB = workerB!.GetService<IMathsService>();
            var valueSetWorkerA1 = Guid.NewGuid().ToString();
            await mathServiceA1.SetValueTest(valueSetWorkerA1);
            var valueGetWorkerB = await mathServiceB.GetValueTest();
            var valueGetWorkerA1 = await mathServiceA1.GetValueTest();
            var valueGetWorkerA2 = await mathServiceA2.GetValueTest();
            if (valueGetWorkerA1 != valueSetWorkerA1) throw new Exception("Unexpected result");
            if (valueGetWorkerA2 != valueSetWorkerA1) throw new Exception("SharedWorker appears not shared");
            if (valueGetWorkerB == valueSetWorkerA1) throw new Exception("SharedWorker with different name unexpectedly same as first SharedWorker");
        }
        #endregion

        #region WorkerPool Worker Disposal Test

        [TestMethod]
        public async Task WebWorkerTaskPoolWorkerDisposalTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            // Cancel the task after 2 seconds
            var worker = await WebWorkerService.TaskPool.GetWorkerAsync();
            Console.WriteLine($"WebWorkerService.TaskPool count/max before disposal: {WebWorkerService.TaskPool.WorkersRunning} / {WebWorkerService.TaskPool.MaxPoolSize}");
            worker.Dispose();
            Console.WriteLine($"WebWorkerService.TaskPool count immediately after disposal: {WebWorkerService.TaskPool.WorkersRunning}");
            var worker2 = await WebWorkerService.TaskPool.GetWorkerAsync();
            //await Task.Delay(2000);
            Console.WriteLine($"WebWorkerService.TaskPool count after disposal and await : {WebWorkerService.TaskPool.WorkersRunning}");
            worker2.ReleaseLock();

            await Task.Delay(2000);
            Console.WriteLine($"WebWorkerService.TaskPool count after disposal and await (b) : {WebWorkerService.TaskPool.WorkersRunning}");
        }
        #endregion

        #region SharedCancellationTokenSource and Workers

        [TestMethod]
        public async Task WebWorkerSharedCancellationTokenSourceTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            if (JS.CrossOriginIsolated == false)
            {
                throw new UnsupportedTestException("Not CrossOriginIsolated.");
            }
            if (!SharedArrayBuffer.Supported)
            {
                throw new UnsupportedTestException("SharedArrayBuffer not supported by browser.");
            }
            var retd = -2;
            var loops = 0;
            using (var cts = new SharedCancellationTokenSource())
            {
                var token = cts.Token;
                var runningTask = WebWorkerService.TaskPool.Run(() => AssembleSyncXXX(token));
                while (true)
                {
                    ++loops;
                    try
                    {
                        retd = await runningTask.WaitAsync(TimeSpan.FromMilliseconds(10));
                        break;
                    }
                    catch (TimeoutException) { }
                    cts.Cancel();
                }
            }
            Console.WriteLine($"main retd: {retd}, loops: {loops}");
        }
        static int AssembleSyncXXX(SharedCancellationToken token)
        {
            for (var i = 0; i < 100000000; i++)
            {
                if (token.IsCancellationRequested) return i;
            }
            return -42;
        }

        [TestMethod]
        public async Task WebWorkerSharedCancellationTokenTest()
        {
            if (!WebWorkerService.WebWorkerSupported)
            {
                throw new UnsupportedTestException("Worker not supported by browser.");
            }
            if (JS.CrossOriginIsolated == false)
            {
                throw new UnsupportedTestException("Not CrossOriginIsolated.");
            }
            if (!SharedArrayBuffer.Supported)
            {
                throw new UnsupportedTestException("SharedArrayBuffer not supported by browser.");
            }
            // Cancel the task after 2 seconds
            using var cts = new SharedCancellationTokenSource(2000);
            var i = await WebWorkerService.TaskPool.Run(() => CancellableMethod(10000, cts.Token));
            if (i == -1) throw new Exception("Task Cancellation failed");
        }

        // Returns -1 if not cancelled
        // This method will run for 10 seconds if not cancelled
        private static long CancellableMethod(double maxRunTimeMS, SharedCancellationToken token)
        {
            Console.WriteLine(">> CancellableMethod");
            var startTime = DateTime.Now;
            var maxRunTime = TimeSpan.FromMilliseconds(maxRunTimeMS);
            long i = 0;
            while (DateTime.Now - startTime < maxRunTime)
            {
                // do some work ...
                i += 1;
                // check if cancelled message received
                Console.WriteLine("?");
                if (token.IsCancellationRequested) return i;
            }
            Console.WriteLine("<< CancellableMethod");
            return -1;
        }
        #endregion

        #region Atomics and Workers
        [TestMethod]
        public async Task AtomicsTest()
        {
            if (JS.CrossOriginIsolated == false)
            {
                throw new UnsupportedTestException("Not CrossOriginIsolated.");
            }
            if (!SharedArrayBuffer.Supported)
            {
                throw new UnsupportedTestException("SharedArrayBuffer not supported by browser.");
            }
            var values = new int[] { 0, 1, 2, 3, 55, 5, 6, 7 };
            JS.Set("values", values);
            using var sharedArrayBuffer = new SharedArrayBuffer(Int32Array.BYTES_PER_ELEMENT * values.Length);
            using var typedArray = new Int32Array(sharedArrayBuffer);
            typedArray.Set(new byte[] { 1, 2, 3, 55, 5, 6, 7 }, 1);
            JS.Set("typedArray", typedArray);
            JS.Log("typedArray", typedArray);
            var oldValue = Atomics.Add(typedArray, 1, 42);
            Console.WriteLine($"oldValue: {oldValue}");
            if (oldValue != values[1]) throw new Exception("Match Failed 1");
            var curValue = Atomics.Load(typedArray, 1);
            Console.WriteLine($"curValue: {curValue}");
            if (curValue != 43) throw new Exception("Match Failed 2");
            var ret = await WebWorkerService.TaskPool.Run(() => WebWorkerAtomicsTest(values, sharedArrayBuffer, 6));
            if (ret != typedArray[6]) throw new Exception("Match Failed 3");
        }
        static int WebWorkerAtomicsTest(int[] values, SharedArrayBuffer sharedArrayBuffer, long index)
        {
            using var typedArray = new Int32Array(sharedArrayBuffer);
            Console.WriteLine($"worker exchanging [{index}] with 1");
            var oldValue = Atomics.Exchange(typedArray, index, 1);
            Console.WriteLine($"worker oldValue 6a: {oldValue}");
            oldValue = Atomics.Add(typedArray, index, 42);
            Console.WriteLine($"worker oldValue a2: {oldValue}");
            if (oldValue != values[1]) throw new Exception("Match Failed 1");
            var curValue = Atomics.Load(typedArray, index);
            Console.WriteLine($"curValue: {curValue}");
            if (curValue != 43) throw new Exception("Match Failed 2");
            return curValue;
        }
        [TestMethod]
        public async Task AtomicsSharedArrayBufferBigInt64ArrayTest()
        {
            if (!SharedArrayBuffer.Supported)
            {
                throw new UnsupportedTestException("SharedArrayBuffer not supported by browser.");
            }
            if (!BigInt64Array.Supported)
            {
                throw new UnsupportedTestException("BigInt64Array not supported by browser.");
            }
            var bigValue = long.MaxValue - 100;
            var values = new long[] { 0, 1, 2, 3, 55, 5, 6, 7 };
            JS.Set("values", values);
            using var sharedArrayBuffer = new SharedArrayBuffer(BigInt64Array.BYTES_PER_ELEMENT * values.Length);
            using var typedArray = new BigInt64Array(sharedArrayBuffer);
            typedArray.Set(values);
            JS.Set("typedArray", typedArray);
            JS.Log("typedArray", typedArray);
            Atomics.Exchange(typedArray, 1, bigValue);
            if (typedArray[1] != bigValue) throw new Exception("Match Failed 1");
            Atomics.Exchange(typedArray, 1, values[1]);
            var oldValue = Atomics.Add(typedArray, 1, 42);
            Console.WriteLine($"oldValue: {oldValue}");
            if (oldValue != values[1]) throw new Exception("Match Failed 1a");
            var curValue = Atomics.Load(typedArray, 1);
            Console.WriteLine($"curValue: {curValue}");
            if (curValue != 43) throw new Exception("Match Failed 2");
            var ret = await WebWorkerService.TaskPool.Run(() => WebWorkerAtomicsSharedArrayBufferBigInt64ArrayTest(values, sharedArrayBuffer, 6));
            if (ret != typedArray[6]) throw new Exception("Match Failed 3");
        }
        static long WebWorkerAtomicsSharedArrayBufferBigInt64ArrayTest(long[] values, SharedArrayBuffer sharedArrayBuffer, long index)
        {
            using var typedArray = new BigInt64Array(sharedArrayBuffer);
            Console.WriteLine($"worker exchanging [{index}] with 1");
            var oldValue = Atomics.Exchange(typedArray, index, 1);
            Console.WriteLine($"worker oldValue 6a: {oldValue}");
            oldValue = Atomics.Add(typedArray, index, 42);
            Console.WriteLine($"worker oldValue a2: {oldValue}");
            if (oldValue != values[1]) throw new Exception("Match Failed 1");
            var curValue = Atomics.Load(typedArray, index);
            Console.WriteLine($"curValue: {curValue}");
            if (curValue != 43) throw new Exception("Match Failed 2");
            return curValue;
        }
        #endregion

        #region AbortSignal and Workers
        [TestMethod]
        public async Task AbortSignalTest()
        {
            if (JS.IsUndefined("AbortSignal"))
            {
                throw new UnsupportedTestException("AbortSignal not supported by browser.");
            }
            if (JS.IsUndefined("AbortSignal.timeout"))
            {
                throw new UnsupportedTestException("AbortSignal.timeout not supported by browser.");
            }
            //var worker = await WebWorkerService.TaskPool.GetWorkerAsync();
            var signal = AbortSignal.Timeout(1000);
            var task = WindowAbortSignalTest(signal);
            await Task.Delay(1000);
            //abortController.Abort("taking too long");
            var ret = await task;
            Console.WriteLine($"ret: {ret}");
            //worker.ReleaseLock();
        }
        private static async Task<long> WindowAbortSignalTest(AbortSignal abortSignal)
        {
            var startTime = DateTime.Now;
            var maxRunTime = TimeSpan.FromMilliseconds(5000);
            long i = 0;
            while (DateTime.Now - startTime < maxRunTime)
            {
                // do some work ...
                i += 1;
                await Task.Delay(5);
                // check if cancelled message received
                if (abortSignal.Aborted)
                {
                    var reason = abortSignal.GetReason<DOMException>();
                    Console.WriteLine($"reason: {reason.Name}");
                    return i;
                }
            }
            return -1;
        }
        private static long WebWorkerAbortSignalTest(AbortSignal abortSignal)
        {
            var startTime = DateTime.Now;
            var maxRunTime = TimeSpan.FromMilliseconds(5000);
            long i = 0;
            while (DateTime.Now - startTime < maxRunTime)
            {
                // do some work ...
                i += 1;
                // check if cancelled message received
                if (abortSignal.Aborted) return i;
            }
            return -1;
        }
        #endregion

        #region Locks and Workers
        [TestMethod]
        public async Task LockManagerTest()
        {
            if (WebWorkerService.Locks == null)
            {
                throw new UnsupportedTestException("Locks not supported by browser.");
            }
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
        [TestMethod]
        public async Task LockManagerRequestHandleTest()
        {
            if (WebWorkerService.Locks == null)
            {
                throw new Exception("Locks not supported by browser.");
            }
            JS.Log("requesting lock");
            TaskCompletionSource tcs = await WebWorkerService.Locks.RequestHandle("my_lock");
            // because this is an exclusive lock,
            // the code in this callback will never run in more than 1 thread at a time.
            JS.Log("have lock");
            // simulating async work like synchronizing a browser db to the server
            await Task.Delay(1000);
            // the lock is not released until this async method exits
            JS.Log("releasing lock");
            tcs.SetResult();
            JS.Log("released lock");
        }
        [TestMethod]
        public async Task LockManagerReturnValueTest()
        {
            if (WebWorkerService.Locks == null)
            {
                throw new UnsupportedTestException("Locks not supported by browser.");
            }
            using var navigator = JS.Get<Navigator>("navigator");
            using var locks = navigator.Locks;
            var randId = Guid.NewGuid().ToString();
            Console.WriteLine("requesting lock");
            var clientId2 = await locks.Request(randId, (l) =>
            {
                return "apples!!!!~";
            });
            Console.WriteLine($"released lock: {clientId2}");
        }

        [TestMethod]
        public async Task LockManagerExclusiveLockTimeoutTest()
        {
            if (WebWorkerService.Locks == null)
            {
                throw new UnsupportedTestException("Locks not supported by browser.");
            }
            using var navigator = JS.Get<Navigator>("navigator");
            using var locks = navigator.Locks;
            var lockName = Guid.NewGuid().ToString();
            // Request the lock and get a TaskCompletionSource
            TaskCompletionSource lockHandle1 = await locks.RequestHandle(lockName);
            // the below lock will fail after 1000 milliseconds because we are already holding the lock lockName
            try
            {
                TaskCompletionSource lockHandle2 = await locks.RequestHandle(lockName, 1000);
            }
            catch
            {
                Console.WriteLine($"Success. Lock acquisition timed out as expected");
                // release lockHandle1
                lockHandle1.SetResult();
                return;
            }
            lockHandle1.SetResult();
            throw new Exception("Failed");
        }

        [TestMethod]
        public async Task LockManagerLockRejectTest()
        {
            if (WebWorkerService.Locks == null)
            {
                throw new UnsupportedTestException("Locks not supported by browser.");
            }
            using var navigator = JS.Get<Navigator>("navigator");
            using var locks = navigator.Locks;
            var lockName = Guid.NewGuid().ToString();
            // Request the lock and get a TaskCompletionSource
            TaskCompletionSource lockHandle1 = await locks.RequestHandle(lockName);
            JS.Log($"Acquired lock: {lockName}");
            await Task.Delay(10);
            JS.Log("locks", await locks.Query());
            JS.Log($"Releasing lock: {lockName}");
            lockHandle1.SetException(new Exception("test"));
            await Task.Delay(10);
            JS.Log("locks", await locks.Query());
        }

        private static long WebWorkerLockManagerTest(AbortSignal abortSignal)
        {
            var startTime = DateTime.Now;
            var maxRunTime = TimeSpan.FromMilliseconds(5000);
            long i = 0;
            while (DateTime.Now - startTime < maxRunTime)
            {
                // do some work ...
                i += 1;
                // check if cancelled message received
                if (abortSignal.Aborted) return i;
            }
            return -1;
        }

        #endregion

        #region Type serialization and Workers
        [TestMethod]
        public async Task TypeSerializationTest()
        {
            var type = typeof(WebWorkerService);
            var typeRB = await WebWorkerService.TaskPool.Run(() => WebWorkerTypeSerializationTest(type));
            if (type != typeRB) throw new Exception("Failed");
        }

        private static Type WebWorkerTypeSerializationTest(Type type)
        {
            Console.WriteLine($"Type recvd: {type.FullName}");
            return type;
        }
        #endregion

        #region Transferable and Workers - tests what Transferable objects require transfer to be used in workers (Note: detached == neutered)
        /// <summary>
        /// Uses the `structuredClone()` method to determine if a transferable object is `neutered`, as in the ownership of the object has been transferred.<br/>
        /// If not already `neutered` this method will cause it to become `neutered`.
        /// </summary>
        /// <param name="transferableObject"></param>
        /// <returns></returns>
        bool IsNeuteredCheck(JSObject transferableObject)
        {
            if (JS.GlobalThis is WorkerGlobalScope workerGlobalScope)
            {
                try
                {
                    using var ret = workerGlobalScope.StructuredClone<JSObject>(transferableObject, new StructuredCloneOptions { Transfer = new[] { transferableObject } });
                    return false;
                }
                catch
                {
                    // transfer failed
                    return true;
                }
            }
            else if (JS.GlobalThis is Window window)
            {
                try
                {
                    using var ret = window.StructuredClone<JSObject>(transferableObject, new StructuredCloneOptions { Transfer = new[] { transferableObject } });
                    return false;
                }
                catch
                {
                    // transfer failed
                    return true;
                }
            }
            throw new Exception($"Unsupported global scope: {JS.GlobalThisTypeName}");
        }
        /// <summary>
        /// This method check if an object can be cloned using structuredClone to determine if it must be added to the Transfer list.<br/>
        /// The structuredClone call will fail if transfer is required
        /// </summary>
        /// <param name="transferableObject"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        bool StructuredIsTransferRequiredCheck<T>(T transferableObject) where T : JSObject
        {
            // the clone will fail if transfer is required
            if (JS.GlobalThis is WorkerGlobalScope workerGlobalScope)
            {
                try
                {
                    transferableObject = workerGlobalScope.StructuredClone<T>(transferableObject);
                    return false;
                }
                catch (Exception ex)
                {
                    // clone failed, transfer is required
                    Console.WriteLine($"Clone failed {typeof(T).Name}: {ex.Message}");
                    return true;
                }
            }
            else if (JS.GlobalThis is Window window)
            {
                try
                {
                    transferableObject = window.StructuredClone<T>(transferableObject);
                    return false;
                }
                catch (Exception ex)
                {
                    // clone failed, transfer is required
                    Console.WriteLine($"Clone failed {typeof(T).Name}: {ex.Message}");
                    return true;
                }
            }
            throw new Exception($"Unsupported global scope: {JS.GlobalThisTypeName}");
        }
        /// <summary>
        /// Uses structuredClone to test if an object is transferable.<br/>
        /// This test will cause the object to be `neutered` if successful
        /// </summary>
        /// <param name="transferableObject"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        bool StructuredIsTransferableCheck<T>(T transferableObject) where T : JSObject
        {
            // the call will be successful is the object is transferable
            if (JS.GlobalThis is WorkerGlobalScope workerGlobalScope)
            {
                try
                {
                    transferableObject = workerGlobalScope.StructuredClone<T>(transferableObject, new StructuredCloneOptions { Transfer = new object[] { transferableObject } });
                    return true;
                }
                catch (Exception ex)
                {
                    // transfer failed
                    Console.WriteLine($"Transfer failed {typeof(T).Name}: {ex.Message}");
                    return false;
                }
            }
            else if (JS.GlobalThis is Window window)
            {
                try
                {
                    transferableObject = window.StructuredClone<T>(transferableObject, new StructuredCloneOptions { Transfer = new object[] { transferableObject } });
                    return true;
                }
                catch (Exception ex)
                {
                    // transfer failed
                    Console.WriteLine($"Transfer failed {typeof(T).Name}: {ex.Message}");
                    return false;
                }
            }
            throw new Exception($"Unsupported global scope: {JS.GlobalThisTypeName}");
        }
        /// <summary>
        /// Transfer tests for RTCDataChannel<br/>
        /// Different results on Firefox and Chromium
        /// </summary>
        //[TestMethod]
        public async Task TransferTestRTCDataChannel()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<RTCDataChannel>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(RTCDataChannel)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");
            // Tests
            {
                // get instance to test
                using var conn = new RTCPeerConnection();
                var rtcDataChannel = conn.CreateDataChannel("my channel");
                // 1. test if transfer is required using structuredClone
                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(rtcDataChannel);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception($"{nameof(RTCDataChannel)} error: TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception($"{nameof(RTCDataChannel)} error: TransferRequired should be false");
                    }
                }
            }
            {
                // get instance to test
                using var conn = new RTCPeerConnection();
                var rtcDataChannel = conn.CreateDataChannel("my channel");
                // 2. test if transfer is allowed using structuredClone
                var structuredCloneIsTransferable = StructuredIsTransferableCheck(rtcDataChannel);
                if (isTransferable != structuredCloneIsTransferable)
                {
                    if (structuredCloneIsTransferable)
                    {
                        throw new Exception($"{nameof(RTCDataChannel)} error: Transferable should be true");
                    }
                    else
                    {
                        throw new Exception($"{nameof(RTCDataChannel)} error: Transferable should be false");
                    }
                }
            }
            JS.Log($"Type verified: {nameof(RTCDataChannel)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");
        }
        /// <summary>
        /// Transfer tests for TransformStream</br>
        /// </summary>
        [TestMethod]
        public async Task TransferTestTransformStream()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<TransformStream>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(TransformStream)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");
            // Tests
            {
                // get instance to test
                var transformStream = new TransformStream();
                // 1. test if transfer is required using structuredClone
                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(transformStream);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception($"{nameof(TransformStream)} error: TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception($"{nameof(TransformStream)} error: TransferRequired should be false");
                    }
                }
            }
            {
                // get instance to test
                var transformStream = new TransformStream();
                // 2. test if transfer is allowed using structuredClone
                var structuredCloneIsTransferable = StructuredIsTransferableCheck(transformStream);
                if (isTransferable != structuredCloneIsTransferable)
                {
                    if (structuredCloneIsTransferable)
                    {
                        throw new Exception($"{nameof(TransformStream)} error: Transferable should be true");
                    }
                    else
                    {
                        throw new Exception($"{nameof(TransformStream)} error: Transferable should be false");
                    }
                }
            }
            JS.Log($"Type verified: {nameof(TransformStream)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");
        }
        /// <summary>
        /// Transfer tests for VideoFrame
        /// </summary>
        [TestMethod]
        public async Task TransferTestVideoFrame()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<VideoFrame>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(VideoFrame)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");
            // Tests
            {
                // get instance to test
                using var image = await HTMLImageElement.CreateFromImageAsync("./icon-192.png");
                var videoFrame = new VideoFrame(image, new VideoFrameOptions { Timestamp = 0 });
                // 1. test if transfer is required using structuredClone
                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(videoFrame);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception($"{nameof(VideoFrame)} error: TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception($"{nameof(VideoFrame)} error: TransferRequired should be false");
                    }
                }
            }
            {
                // get instance to test
                using var image = await HTMLImageElement.CreateFromImageAsync("./icon-192.png");
                var videoFrame = new VideoFrame(image, new VideoFrameOptions { Timestamp = 0 });
                // 2. test if transfer is allowed using structuredClone
                var structuredCloneIsTransferable = StructuredIsTransferableCheck(videoFrame);
                if (isTransferable != structuredCloneIsTransferable)
                {
                    if (structuredCloneIsTransferable)
                    {
                        throw new Exception($"{nameof(VideoFrame)} error: Transferable should be true");
                    }
                    else
                    {
                        throw new Exception($"{nameof(VideoFrame)} error: Transferable should be false");
                    }
                }
            }
            JS.Log($"Type verified: {nameof(VideoFrame)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");
        }
        /// <summary>
        /// Transfer tests for MIDIAccess</br>
        /// NOTE: If this test requires user interaction to get an isntance it may be disabled.
        /// </summary>
        //[TestMethod]
        public async Task TransferTestMIDIAccess()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<MIDIAccess>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(MIDIAccess)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");
            // Tests
            {
                // get instance to test
                using var navigator = JS.Get<Navigator>("navigator");
                var midiAccess = await navigator.RequestMIDIAccess();
                // 1. test if transfer is required using structuredClone
                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(midiAccess);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception($"{nameof(MIDIAccess)} error: TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception($"{nameof(MIDIAccess)} error: TransferRequired should be false");
                    }
                }
            }
            {
                // get instance to test
                using var navigator = JS.Get<Navigator>("navigator");
                var midiAccess = await navigator.RequestMIDIAccess();
                if (midiAccess == null) throw new Exception($"{nameof(MIDIAccess)} error: Could not acquire new instance");
                // 2. test if transfer is allowed using structuredClone
                var structuredCloneIsTransferable = transferRequired || StructuredIsTransferableCheck(midiAccess);
                if (isTransferable != structuredCloneIsTransferable)
                {
                    if (structuredCloneIsTransferable)
                    {
                        throw new Exception($"{nameof(MIDIAccess)} error: Transferable should be true");
                    }
                    else
                    {
                        throw new Exception($"{nameof(MIDIAccess)} error: Transferable should be false");
                    }
                }
            }
            JS.Log($"Type verified: {nameof(MIDIAccess)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");
        }
        /// <summary>
        /// Transfer tests for MediaStreamTrack</br>
        /// NOTE: This test requires user interaction to get a MediaStreamTrack... may be disabled.
        /// </summary>
        //[TestMethod]
        public async Task TransferTestMediaStreamTrack()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<MediaStreamTrack>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(MediaStreamTrack)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");
            // Tests
            {
                // get instance to test
                using var navigator = JS.Get<Navigator>("navigator");
                using var stream = await navigator.MediaDevices.GetUserMedia(new MediaStreamConstraints { Audio = true, Video = true });
                var mediaStreamTrack = stream!.GetTracks().First();
                // 1. test if transfer is required using structuredClone
                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(mediaStreamTrack);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception($"{nameof(MediaStreamTrack)} error: TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception($"{nameof(MediaStreamTrack)} error: TransferRequired should be false");
                    }
                }
            }
            {
                // get instance to test
                using var navigator = JS.Get<Navigator>("navigator");
                using var stream = await navigator.MediaDevices.GetUserMedia(new MediaStreamConstraints { Audio = true, Video = true });
                var mediaStreamTrack = stream!.GetTracks().First();
                // 2. test if transfer is allowed using structuredClone
                var structuredCloneIsTransferable = transferRequired || StructuredIsTransferableCheck(mediaStreamTrack);
                if (isTransferable != structuredCloneIsTransferable)
                {
                    if (structuredCloneIsTransferable)
                    {
                        throw new Exception($"{nameof(MediaStreamTrack)} error: Transferable should be true");
                    }
                    else
                    {
                        throw new Exception($"{nameof(MediaStreamTrack)} error: Transferable should be false");
                    }
                }
            }
            JS.Log($"Type verified: {nameof(MediaStreamTrack)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");
        }
        /// <summary>
        /// Transfer tests for MediaSourceHandle</br>
        /// Unsupported test on Firefox (diff results than Chromium)
        /// </summary>
        [TestMethod]
        public async Task TransferTestMediaSourceHandle()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<MediaSourceHandle>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type test: {nameof(MediaSourceHandle)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");

            // this test uses a runtime check of structuredClone to determine if the TransferableAttribute on
            // MediaSourceHandle has the correct TransferRequired value
            {
                var mediaSourceHandle = await WebWorkerService.TaskPool.Run(() => CreateMediaSourceHandle(default!));

                // As of 2026-01-20 In a dedicated worker in Firefox, MediaSource is undefined
                if (mediaSourceHandle == null) throw new UnsupportedTestException("MediaSource not supported in worker");

                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(mediaSourceHandle);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception("TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception("TransferRequired should be false");
                    }
                }
            }
            JS.Log($"Type verified: {nameof(MediaSourceHandle)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");

            // This test verifies that the object is transferred only if its TransferableAttribute.TransferRequired property == true
            {
                var mediaSourceHandle = await WebWorkerService.TaskPool.Run(() => CreateMediaSourceHandle(default!));

                // send the MediaSourceHandle to a WebWorker without WorkerTransfer. The object will only be transferred if TransferRequired == true
                await WebWorkerService.TaskPool.Run(() => TransferRequiredTestMediaSourceHandleMethod(mediaSourceHandle));

                // check if detached. should only be detached if the transfer was required
                var isDetached = IsNeuteredCheck(mediaSourceHandle);
                if (transferRequired)
                {
                    // the MediaSourceHandle should be detached
                    if (!isDetached) throw new Exception("Unexpectdly not detached");
                }
                else
                {
                    // the MediaSourceHandle should not be detached
                    if (isDetached) throw new Exception("Unexpectdly detached");
                }
            }

            // This test verifies that the object is transferred if it is a transferable object,
            // caused by the WorkerTransfer attribute on the method parameter
            {
                var mediaSourceHandle = await WebWorkerService.TaskPool.Run(() => CreateMediaSourceHandle(default!));

                // send the MediaSourceHandle to a WebWorker with WorkerTransfer. The object should be transferred, and therefore detached
                await WebWorkerService.TaskPool.Run(() => TransferAllTestMediaSourceHandleMethod(mediaSourceHandle));

                var isDetached = IsNeuteredCheck(mediaSourceHandle);
                if (isTransferable && !isDetached) throw new Exception("Unexpectdly not detached");
            }
        }
        /// <summary>
        /// MediaSource only has a handle (of type MediaSourceHandle) when created inside of a DedicatedWorker, so this will be called in a dedicated worker to allow access t oa MediaSourceHandle
        /// </summary>
        [return: WorkerTransfer]
        private static async Task<MediaSourceHandle?> CreateMediaSourceHandle([FromServices] BlazorJSRuntime JS)
        {
            var unsupported = JS.IsUndefined("MediaSource");
            if (unsupported) return null;
            using var mediaSource = new MediaSource();
            var mediaSourceHandle = mediaSource.Handle!;
            return mediaSourceHandle;
        }
        private static async Task TransferRequiredTestMediaSourceHandleMethod(MediaSourceHandle offscreenCanvas)
        {
            if (offscreenCanvas == null) throw new Exception("Data error");
        }
        private static async Task TransferAllTestMediaSourceHandleMethod([WorkerTransfer] MediaSourceHandle offscreenCanvas)
        {
            if (offscreenCanvas == null) throw new Exception("Data error");
        }
        /// <summary>
        /// Transfer tests for WritableStream
        /// </summary>
        [TestMethod]
        public async Task TransferTestWritableStream()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<WritableStream>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(WritableStream)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");

            // this test uses a runtime check of structuredClone to determine if the TransferableAttribute on
            // WritableStream has the correct TransferRequired value
            {
                var writableStream = new WritableStream();

                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(writableStream);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception("TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception("TransferRequired should be false");
                    }
                }
            }

            // This test verifies that the object is transferred only if its TransferableAttribute.TransferRequired property == true
            {
                using var writableStream = new WritableStream();

                // send the WritableStream to a WebWorker without WorkerTransfer. The object will only be transferred if TransferRequired == true
                await WebWorkerService.TaskPool.Run(() => TransferRequiredTestWritableStreamMethod(writableStream));

                // check if detached. should only be detached if the transfer was required
                var isDetached = IsNeuteredCheck(writableStream);
                if (transferRequired)
                {
                    // the WritableStream should be detached
                    if (!isDetached) throw new Exception("Unexpectdly not detached");
                }
                else
                {
                    // the WritableStream should not be detached
                    if (isDetached) throw new Exception("Unexpectdly detached");
                }
            }

            // This test verifies that the object is transferred if it is a transferable object,
            // caused by the WorkerTransfer attribute on the method parameter
            {
                using var writableStream = new WritableStream();

                // send the WritableStream to a WebWorker with WorkerTransfer. The object should be transferred, and therefore detached
                await WebWorkerService.TaskPool.Run(() => TransferAllTestWritableStreamMethod(writableStream));

                var isDetached = IsNeuteredCheck(writableStream);
                if (isTransferable && !isDetached) throw new Exception("Unexpectdly not detached");
            }
        }
        private static async Task TransferRequiredTestWritableStreamMethod(WritableStream writableStream)
        {
            if (writableStream == null) throw new Exception("Data error");
        }
        private static async Task TransferAllTestWritableStreamMethod([WorkerTransfer] WritableStream writableStream)
        {
            if (writableStream == null) throw new Exception("Data error");
        }
        /// <summary>
        /// Transfer tests for ReadableStream
        /// </summary>
        [TestMethod]
        public async Task TransferTestReadableStream()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<ReadableStream>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(ReadableStream)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");

            // this test uses a runtime check of structuredClone to determine if the TransferableAttribute on
            // ReadableStream has the correct TransferRequired value
            {
                var readableStream = new ReadableStream();

                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(readableStream);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception("TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception("TransferRequired should be false");
                    }
                }
            }

            // This test verifies that the object is transferred only if its TransferableAttribute.TransferRequired property == true
            {
                using var readableStream = new ReadableStream();

                // send the ReadableStream to a WebWorker without WorkerTransfer. The object will only be transferred if TransferRequired == true
                await WebWorkerService.TaskPool.Run(() => TransferRequiredTestReadableStreamMethod(readableStream));

                // check if detached. should only be detached if the transfer was required
                var isDetached = IsNeuteredCheck(readableStream);
                if (transferRequired)
                {
                    // the ReadableStream should be detached
                    if (!isDetached) throw new Exception("Unexpectdly not detached");
                }
                else
                {
                    // the ReadableStream should not be detached
                    if (isDetached) throw new Exception("Unexpectdly detached");
                }
            }

            // This test verifies that the object is transferred if it is a transferable object,
            // caused by the WorkerTransfer attribute on the method parameter
            {
                using var readableStream = new ReadableStream();

                // send the ReadableStream to a WebWorker with WorkerTransfer. The object should be transferred, and therefore detached
                await WebWorkerService.TaskPool.Run(() => TransferAllTestReadableStreamMethod(readableStream));

                var isDetached = IsNeuteredCheck(readableStream);
                if (isTransferable && !isDetached) throw new Exception("Unexpectdly not detached");
            }
        }
        private static async Task TransferRequiredTestReadableStreamMethod(ReadableStream readableStream)
        {
            if (readableStream == null) throw new Exception("Data error");
        }
        private static async Task TransferAllTestReadableStreamMethod([WorkerTransfer] ReadableStream readableStream)
        {
            if (readableStream == null) throw new Exception("Data error");
        }
        /// <summary>
        /// Transfer tests for OffscreenCanvas
        /// </summary>
        [TestMethod]
        public async Task TransferTestOffscreenCanvas()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<OffscreenCanvas>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(OffscreenCanvas)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");

            // this test uses a runtime check of structuredClone to determine if the TransferableAttribute on
            // OffscreenCanvas has the correct TransferRequired value
            {
                var offscreenCanvas = new OffscreenCanvas(64, 64);

                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(offscreenCanvas);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception("TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception("TransferRequired should be false");
                    }
                }
            }

            // This test verifies that the object is transferred only if its TransferableAttribute.TransferRequired property == true
            {
                using var offscreenCanvas = new OffscreenCanvas(64, 64);

                // send the OffscreenCanvas to a WebWorker without WorkerTransfer. The object will only be transferred if TransferRequired == true
                await WebWorkerService.TaskPool.Run(() => TransferRequiredTestOffscreenCanvasMethod(offscreenCanvas));

                // check if detached. should only be detached if the transfer was required
                var isDetached = IsNeuteredCheck(offscreenCanvas);
                if (transferRequired)
                {
                    // the OffscreenCanvas should be detached
                    if (!isDetached) throw new Exception("Unexpectdly not detached");
                }
                else
                {
                    // the OffscreenCanvas should not be detached
                    if (isDetached) throw new Exception("Unexpectdly detached");
                }
            }

            // This test verifies that the object is transferred if it is a transferable object,
            // caused by the WorkerTransfer attribute on the method parameter
            {
                using var offscreenCanvas = new OffscreenCanvas(64, 64);

                // send the OffscreenCanvas to a WebWorker with WorkerTransfer. The object should be transferred, and therefore detached
                await WebWorkerService.TaskPool.Run(() => TransferAllTestOffscreenCanvasMethod(offscreenCanvas));

                var isDetached = IsNeuteredCheck(offscreenCanvas);
                if (isTransferable && !isDetached) throw new Exception("Unexpectdly not detached");
            }
        }
        private static async Task TransferRequiredTestOffscreenCanvasMethod(OffscreenCanvas offscreenCanvas)
        {
            if (offscreenCanvas == null) throw new Exception("Data error");
        }
        private static async Task TransferAllTestOffscreenCanvasMethod([WorkerTransfer] OffscreenCanvas offscreenCanvas)
        {
            if (offscreenCanvas == null) throw new Exception("Data error");
        }
        /// <summary>
        /// Transfer tests for MessagePort
        /// </summary>
        [TestMethod]
        public async Task TransferTestMessagePort()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<MessagePort>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(MessagePort)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");

            // this test uses a runtime check of structuredClone to determine if the TransferableAttribute on
            // MessagePort has the correct TransferRequired value
            {
                using var channel = new MessageChannel();
                var messagePort = channel.Port2;
                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(messagePort);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception("TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception("TransferRequired should be false");
                    }
                }
            }

            // This test verifies that the object is transferred only if its TransferableAttribute.TransferRequired property == true
            {
                using var channel = new MessageChannel();
                using var messagePort = channel.Port2;

                // send the MessagePort to a WebWorker without WorkerTransfer. The object will only be transferred if TransferRequired == true
                await WebWorkerService.TaskPool.Run(() => TransferRequiredTestMessagePortMethod(messagePort));

                // check if detached. should only be detached if the transfer was required
                var isDetached = IsNeuteredCheck(messagePort);
                if (transferRequired)
                {
                    // the MessagePort should be detached
                    if (!isDetached) throw new Exception("Unexpectdly not detached");
                }
                else
                {
                    // the MessagePort should not be detached
                    if (isDetached) throw new Exception("Unexpectdly detached");
                }
            }

            // This test verifies that the object is transferred if it is a transferable object,
            // caused by the WorkerTransfer attribute on the method parameter
            {
                using var channel = new MessageChannel();
                using var messagePort = channel.Port2;

                // send the MessagePort to a WebWorker with WorkerTransfer. The object should be transferred, and therefore detached
                await WebWorkerService.TaskPool.Run(() => TransferAllTestMessagePortMethod(messagePort));

                var isDetached = IsNeuteredCheck(messagePort);
                if (isTransferable && !isDetached) throw new Exception("Unexpectdly not detached");
            }
        }
        private static async Task TransferRequiredTestMessagePortMethod(MessagePort messagePort)
        {
            if (messagePort == null) throw new Exception("Data error");
        }
        private static async Task TransferAllTestMessagePortMethod([WorkerTransfer] MessagePort messagePort)
        {
            if (messagePort == null) throw new Exception("Data error");
        }
        /// <summary>
        /// Transfer tests for ImageBitmap
        /// </summary>
        [TestMethod]
        public async Task TransferTestImageBitmap()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<ImageBitmap>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(ImageBitmap)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");

            // this test uses a runtime check of structuredClone to determine if the TransferableAttribute on
            // ImageBitmap has the correct TransferRequired value
            {
                using var window = JS.Get<Window>("window");
                using var offscreenCanvas = new OffscreenCanvas(64, 64);
                using var ctx = offscreenCanvas.Get2DContext();
                var imageBitmap = offscreenCanvas.TransferToImageBitmap();

                var structuredCloneRequiresTransfer = StructuredIsTransferRequiredCheck(imageBitmap);
                if (transferRequired != structuredCloneRequiresTransfer)
                {
                    if (structuredCloneRequiresTransfer)
                    {
                        throw new Exception("TransferRequired should be true");
                    }
                    else
                    {
                        throw new Exception("TransferRequired should be false");
                    }
                }
            }

            // This test verifies that the object is transferred only if its TransferableAttribute.TransferRequired property == true
            {
                using var window = JS.Get<Window>("window");
                using var offscreenCanvas = new OffscreenCanvas(64, 64);
                using var ctx = offscreenCanvas.Get2DContext();
                using var imageBitmap = offscreenCanvas.TransferToImageBitmap();

                // send the ImageBitmap to a WebWorker without WorkerTransfer. The object will only be transferred if TransferRequired == true
                await WebWorkerService.TaskPool.Run(() => TransferRequiredTestImageBitmapMethod(imageBitmap));

                // check if detached. should only be detached if the transfer was required
                var isDetached = IsNeuteredCheck(imageBitmap);
                if (transferRequired)
                {
                    // the ImageBitmap should be detached
                    if (!isDetached) throw new Exception("Unexpectdly not detached");
                }
                else
                {
                    // the ImageBitmap should not be detached
                    if (isDetached) throw new Exception("Unexpectdly detached");
                }
            }

            // This test verifies that the object is transferred if it is a transferable object,
            // caused by the WorkerTransfer attribute on the method parameter
            {
                using var window = JS.Get<Window>("window");
                using var offscreenCanvas = new OffscreenCanvas(64, 64);
                using var ctx = offscreenCanvas.Get2DContext();
                using var imageBitmap = offscreenCanvas.TransferToImageBitmap();

                // send the ImageBitmap to a WebWorker with WorkerTransfer. The object should be transferred, and therefore detached
                await WebWorkerService.TaskPool.Run(() => TransferAllTestImageBitmapMethod(imageBitmap));

                var isDetached = IsNeuteredCheck(imageBitmap);
                if (isTransferable && !isDetached) throw new Exception("Unexpectdly not detached");
            }
        }
        private static async Task TransferRequiredTestImageBitmapMethod(ImageBitmap imageBitmap)
        {
            if (imageBitmap == null) throw new Exception("Data error");
        }
        private static async Task TransferAllTestImageBitmapMethod([WorkerTransfer] ImageBitmap imageBitmap)
        {
            if (imageBitmap == null) throw new Exception("Data error");
        }
        /// <summary>
        /// Transfer tests for AudioData
        /// </summary>
        [TestMethod]
        public async Task TransferTestAudioData()
        {
            var transferableAttr = TransferableAttribute.GetTransferable<AudioData>();
            // gets the TransferableAttribute (if one)
            var isTransferable = transferableAttr != null;
            // gets the TransferRequired property from the TransferableAttribute (if one)
            var transferRequired = transferableAttr?.TransferRequired == true;
            JS.Log($"Type: {nameof(AudioData)} IsTransferable: {isTransferable} TransferRequired: {transferRequired}");

            // Create AudioData to run the test with
            var sampleRate = 48000; // Common sample rate in Hz
            var numberOfFrames = sampleRate * 1; // 1 second of audio data
            var numberOfChannels = 1; // Mono audio
            var rawData = new float[numberOfFrames];
            for (var i = 0; i < numberOfFrames; i++)
            {
                // Generate a simple sine wave sample data between -1.0 and 1.0
                rawData[i] = (float)Math.Sin(2 * Math.PI * i * 440 / sampleRate); // 440 Hz tone
            }
            {
                using var audioData = new AudioData(new AudioDataOptions
                {
                    Format = "f32-planar", // 32-bit floating point, planar format (one array per channel)
                    SampleRate = sampleRate,
                    NumberOfFrames = numberOfFrames,
                    NumberOfChannels = numberOfChannels,
                    Timestamp = 0, // Timestamp in microseconds
                    Data = (Float32Array)rawData // Pass the raw data array
                });

                // send the AudioData to a WebWorker without WorkerTransfer. The object will only be transferred if TransferRequired == true
                using var audioDataReturned = await WebWorkerService.TaskPool.Run(() => TransferRequiredTestAudioDataMethod(audioData));

                // check if detached. should only be detached if the transfer was required
                var isDetached = audioData.SampleRate == 0;
                if (transferRequired)
                {
                    // the AudioData should be detached
                    if (!isDetached) throw new Exception("Unexpectdly not detached");
                }
                else
                {
                    // the AudioData should not be detached
                    if (isDetached) throw new Exception("Unexpectdly detached");
                }
            }

            {
                using var audioData = new AudioData(new AudioDataOptions
                {
                    Format = "f32-planar", // 32-bit floating point, planar format (one array per channel)
                    SampleRate = sampleRate,
                    NumberOfFrames = numberOfFrames,
                    NumberOfChannels = numberOfChannels,
                    Timestamp = 0, // Timestamp in microseconds
                    Data = (Float32Array)rawData // Pass the raw data array
                });

                // send the AudioData to a WebWorker with WorkerTransfer. The object should be transferred, and therefore detached
                using var audioDataReturned = await WebWorkerService.TaskPool.Run(() => TransferAllTestAudioDataMethod(audioData));
                var isDetached = audioData.SampleRate == 0;
                if (isTransferable && !isDetached) throw new Exception("Unexpectdly not detached");
            }
        }
        private static async Task<AudioData> TransferRequiredTestAudioDataMethod(AudioData audioData)
        {
            if (audioData?.SampleRate != 48000) throw new Exception("SampleRate mismatch");
            return audioData;
        }
        private static async Task<AudioData> TransferAllTestAudioDataMethod([WorkerTransfer] AudioData audioData)
        {
            if (audioData?.SampleRate != 48000) throw new Exception("SampleRate mismatch");
            return audioData;
        }
        #endregion

        #region WorkerTransfer and Workers
        [TestMethod]
        public async Task WorkerTransferTest1()
        {

            var bytes = new byte[] { 1, 3, 5, 7, 9 };
            // get bytes as a Uint8Array
            using var uint8Array = new Uint8Array(bytes);

            //  get the underlying ArrayBuffer
            using var arrayBufferOrig = uint8Array.Buffer;
            using var arrayBufferReturned1 = await WebWorkerService.TaskPool.Run(() => WorkerWithTransferTestWorkerMethod(arrayBufferOrig));

            // arrayBufferOrig is now detached and cannot be used (indicates it was transferred to the worker)
            if (!arrayBufferOrig.Detached) throw new Exception("ArrayBuffer not detached");

            // pull back into .Net so it more fairly compares to the byte[] method
            var bytesReadBack = arrayBufferReturned1.ReadBytes();
            if (!bytesReadBack.SequenceEqual(bytes)) throw new Exception("Data mismatch after transfer");
        }

        [return: WorkerTransfer]
        private static async Task<ArrayBuffer> WorkerWithTransferTestWorkerMethod([WorkerTransfer] ArrayBuffer arrayBuffer)
        {
            byte[] data = arrayBuffer.ReadBytes();
            ArrayBuffer retunrnedArrayBuffer = new Uint8Array(data).Buffer;
            Console.WriteLine($"Processing ArrayBuffer with WorkerTransfer {data.Length} bytes in worker");
            return retunrnedArrayBuffer;
        }

        [TestMethod]
        public async Task WorkerTransferArrayBufferWithoutTransferTest()
        {

            var bytes = new byte[] { 1, 3, 5, 7, 9 };
            // get bytes as a Uint8Array
            using var uint8Array = new Uint8Array(bytes);

            //  get the underlying ArrayBuffer
            using var arrayBufferOrig = uint8Array.Buffer;
            using var arrayBufferReturned1 = await WebWorkerService.TaskPool.Run(() => WorkerWithoutTransferTestWorkerMethod(arrayBufferOrig));

            // arrayBufferOrig is not detached and can still be used (indicates it was transferred to the worker)
            if (arrayBufferOrig.Detached) throw new Exception("ArrayBuffer detached");

            // pull back into .Net so it more fairly compares to the byte[] method
            var bytesReadBack = arrayBufferReturned1.ReadBytes();
            if (!bytesReadBack.SequenceEqual(bytes)) throw new Exception("Data mismatch after transfer");
        }

        private static async Task<ArrayBuffer> WorkerWithoutTransferTestWorkerMethod(ArrayBuffer arrayBuffer)
        {
            byte[] data = arrayBuffer.ReadBytes();
            ArrayBuffer retunrnedArrayBuffer = new Uint8Array(data).Buffer;
            Console.WriteLine($"Processing ArrayBuffer without WorkerTransfer {data.Length} bytes in worker");
            return retunrnedArrayBuffer;
        }

        [TestMethod]
        public async Task WorkerOffscreenCanvasWorkerTransferListTest()
        {
            using var offscreenCanvas = new OffscreenCanvas(64, 64);

            using var offscreenCanvasReturned = await WebWorkerService.TaskPool.Run(() => WorkerOffscreenCanvasWorkerTransferListTestMethod(offscreenCanvas, new[] { offscreenCanvas }));

            // offscreenCanvas will be detached if transferred (required)
            var detached = offscreenCanvas.Width == 0 || offscreenCanvas.Height == 0;
            if (!detached) throw new Exception("OffscreenCanvas not detached");

            // pull back into .Net so it more fairly compares to the byte[] method
            var sizeMatch = offscreenCanvasReturned.Width == 64 && offscreenCanvasReturned.Height == 64;
            if (!sizeMatch) throw new Exception("Data mismatch after transfer");
        }

        /// <summary>
        /// The TransferableList attribute indicates that the objects that should  be  transferred will be  explicitly set using the marked object[] parameter
        /// </summary>
        /// <param name="offscreenCanvas"></param>
        /// <param name="transferLsit"></param>
        /// <returns></returns>
        private static async Task<OffscreenCanvas> WorkerOffscreenCanvasWorkerTransferListTestMethod(OffscreenCanvas offscreenCanvas, [TransferableList] object[] transferList)
        {
            Console.WriteLine($"Processing OffscreenCanvas with explicit TransferableList {offscreenCanvas.Width}x{offscreenCanvas.Height} bytes in worker");
            return offscreenCanvas;
        }

        [TestMethod]
        public async Task WorkerOffscreenCanvasWithoutTransferTest()
        {
            using var offscreenCanvas = new OffscreenCanvas(64, 64);

            using var offscreenCanvasReturned = await WebWorkerService.TaskPool.Run(() => WorkerOffscreenCanvasWithoutTransferTestMethod(offscreenCanvas));

            // offscreenCanvas will be detached if transferred (required)
            var detached = offscreenCanvas.Width == 0 || offscreenCanvas.Height == 0;
            if (!detached) throw new Exception("OffscreenCanvas not detached");

            // pull back into .Net so it more fairly compares to the byte[] method
            var sizeMatch = offscreenCanvasReturned.Width == 64 && offscreenCanvasReturned.Height == 64;
            if (!sizeMatch) throw new Exception("Data mismatch after transfer");
        }

        private static async Task<OffscreenCanvas> WorkerOffscreenCanvasWithoutTransferTestMethod(OffscreenCanvas offscreenCanvas)
        {
            Console.WriteLine($"Processing OffscreenCanvas with implicit WorkerTransfer {offscreenCanvas.Width}x{offscreenCanvas.Height} bytes in worker");
            return offscreenCanvas;
        }

        [TestMethod]
        public async Task WorkerOffscreenCanvasListWithoutTransferTest()
        {
            using var offscreenCanvas = new OffscreenCanvas(64, 64);

            var offscreenCanvaslist = new List<OffscreenCanvas> { offscreenCanvas };

            var offscreenCanvasesReturned = await WebWorkerService.TaskPool.Run(() => WorkerOffscreenCanvasListWithoutTransferTestMethod(offscreenCanvaslist));
            var offscreenCanvasReturned = offscreenCanvasesReturned[0];

            // offscreenCanvas will be detached if transferred (required)
            var detached = offscreenCanvas.Width == 0 || offscreenCanvas.Height == 0;
            if (!detached) throw new Exception("OffscreenCanvas not detached");

            // pull back into .Net so it more fairly compares to the byte[] method
            var sizeMatch = offscreenCanvasReturned.Width == 64 && offscreenCanvasReturned.Height == 64;
            if (!sizeMatch) throw new Exception("Data mismatch after transfer");
        }

        private static async Task<List<OffscreenCanvas>> WorkerOffscreenCanvasListWithoutTransferTestMethod(List<OffscreenCanvas> offscreenCanvases)
        {
            var offscreenCanvas = offscreenCanvases[0];
            Console.WriteLine($"Processing OffscreenCanvas list with implicit WorkerTransfer {offscreenCanvas.Width}x{offscreenCanvas.Height} bytes in worker");
            return offscreenCanvases;
        }

        [TestMethod]
        public async Task WorkerMyClassDepth3WorkerTransferTest()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            // Testes WorkerTransfer not specified on the method parameter which sets transferred mode to TransferRequired at property depth 3
            {
                // get bytes as a Uint8Array
                using var uint8Array = new Uint8Array(bytes);

                // get underlying ArrayBuffer so we can check it
                using var arrayBuffer = uint8Array.Buffer;

                // get an OffscreenCanvas, which is required to be transferred when passed to postMessage or an exception is thrown is the all fails
                using var offscreen1 = new OffscreenCanvas(64, 64);

                var myClass = new MyClass           // depth 0
                {
                    MySubClass = new MySubClass     // depth 1
                    {
                        Uint8Array = uint8Array,    // depth 2 (ArrayBuffer at depth 3)
                        OffscreenCanvasArray = new[]// depth 2
                        {
                    offscreen1  // depth 3
                },
                    }
                };

                // WorkerMyClassDepth3WorkerTransferTestMethod
                var offscreenCanvasesReturned = await WebWorkerService.TaskPool.Run(() => WorkerMyClassDepth3WorkerTransferTestMethod(myClass));
                var offscreenCanvasReturned = offscreenCanvasesReturned.MySubClass.OffscreenCanvasArray[0];

                // the ArrayBuffer will not be detached because it onyl required transferables are sent by default at depth 3
                var arrayBufferDetached = arrayBuffer.Detached;
                if (arrayBufferDetached) throw new Exception("arrayBuffer original detached");

                // offscreenCanvas will be detached if transferred (required)
                var detached = offscreen1.Width == 0 || offscreen1.Height == 0;
                if (!detached) throw new Exception("OffscreenCanvas original not detached");

                // pull back into .Net so it more fairly compares to the byte[] method
                var sizeMatch = offscreenCanvasReturned.Width == 64 && offscreenCanvasReturned.Height == 64;
                if (!sizeMatch) throw new Exception("Data mismatch after transfer");
            }

            // Testes WorkerTransfer specified on the method parameter which sets transferred mode to TransferAll at property depth 3
            {

                // get bytes as a Uint8Array
                using var uint8Array = (Uint8Array)bytes;

                // get underlying ArrayBuffer so we can check it
                using var arrayBuffer = uint8Array.Buffer;

                // get an OffscreenCanvas, which is required to be transferred when passed to postMessage or an exception is thrown is the all fails
                using var offscreen1 = new OffscreenCanvas(64, 64);

                var myClass = new MyClass           // depth 0
                {
                    MySubClass = new MySubClass     // depth 1
                    {
                        Uint8Array = uint8Array,    // depth 2 (ArrayBuffer at depth 3, default max depth)
                        OffscreenCanvasArray = new[]// depth 2
                        {
                        offscreen1              // depth 3, default max depth
                    },
                    }
                };

                // WorkerMyClassDepth3WorkerTransferTestAllMethod
                var offscreenCanvasesReturned = await WebWorkerService.TaskPool.Run(() => WorkerMyClassDepth3WorkerTransferTestAllMethod(myClass));
                var offscreenCanvasReturned = offscreenCanvasesReturned.MySubClass.OffscreenCanvasArray[0];

                // the ArrayBuffer will be detached because it optional transferables are sent at depth 3 because WorekrTransfer was specified on the parameter
                var arrayBufferDetached = arrayBuffer.Detached;
                if (!arrayBufferDetached) throw new Exception("arrayBuffer original not detached");

                // offscreenCanvas will be detached if transferred (required)
                var detached = offscreen1.Width == 0 || offscreen1.Height == 0;
                if (!detached) throw new Exception("OffscreenCanvas original not detached");

                // pull back into .Net so it more fairly compares to the byte[] method
                var sizeMatch = offscreenCanvasReturned.Width == 64 && offscreenCanvasReturned.Height == 64;
                if (!sizeMatch) throw new Exception("Data mismatch after transfer");
            }
        }

        /// <summary>
        /// The MyClass myClass parameter is not marked with WorkerTransfer so it will use the default: new WorkerTransfer(WorkerTransferMode.TransferRequired, depth: 3)<br/>
        /// </summary>
        /// <param name="myClass"></param>
        /// <returns></returns>
        private static async Task<MyClass> WorkerMyClassDepth3WorkerTransferTestMethod(MyClass myClass)
        {
            byte[] data = myClass.MySubClass.Uint8Array.ReadBytes();
            ArrayBuffer retunrnedArrayBuffer = new Uint8Array(data).Buffer;
            Console.WriteLine($"Processing MyClass with implicit WorkerTransfer {data.Length} bytes in worker");
            return myClass;
        }
        /// <summary>
        /// The MyClass myClass parameter is marked with the default WorkerTransfer so it will use: new WorkerTransfer(WorkerTransferMode.TransferAll, depth: 3)<br/>
        /// </summary>
        /// <param name="myClass"></param>
        /// <returns></returns>
        private static async Task<MyClass> WorkerMyClassDepth3WorkerTransferTestAllMethod([WorkerTransfer] MyClass myClass)
        {
            byte[] data = myClass.MySubClass.Uint8Array.ReadBytes();
            ArrayBuffer retunrnedArrayBuffer = new Uint8Array(data).Buffer;
            Console.WriteLine($"Processing MyClass with explicit WorkerTransfer {data.Length} bytes in worker");
            return myClass;
        }

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
        #endregion

    }
}
