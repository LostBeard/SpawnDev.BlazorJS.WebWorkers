using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Provides a dispatcher for service calls using a shared web worker, enabling concurrent processing across
    /// multiple browser contexts.
    /// </summary>
    /// <remarks>Shared web workers are only supported in environments where the 'SharedWorker' API is
    /// available. Use the static 'Supported' property to determine if shared web workers can be utilized. The worker's
    /// name uniquely identifies it within the application context. This class manages the lifecycle of the shared
    /// worker and ensures proper resource cleanup by implementing IDisposable.</remarks>
    public class SharedWebWorker : ServiceCallDispatcher, IDisposable
    {
        /// <summary>
        /// Returns true if the SharedWorker API is supported in the current environment, allowing for the use of shared web workers. This property is initialized in the static constructor by checking if the 'SharedWorker' object is defined in the JavaScript environment. If it is undefined, it indicates that shared web workers are not supported, and the property will be set to false. This allows developers to conditionally use shared web workers based on the capabilities of the user's browser or environment.
        /// </summary>
        public static bool Supported;
        static SharedWebWorker()
        {
            Supported = !JS.IsUndefined("SharedWorker");
        }
        /// <summary>
        /// The underlying SharedWorker instance that this SharedWebWorker manages. This property provides access to the shared worker, allowing for communication and control over its lifecycle. The SharedWorker is responsible for handling concurrent processing across multiple browser contexts, and this property allows developers to interact with it directly if needed. Proper management of the shared worker is crucial for ensuring efficient resource usage and preventing memory leaks, especially when disposing of the SharedWebWorker instance.
        /// </summary>
        SharedWorker _sharedWorker { get; set; }
        /// <summary>
        /// The name of the shared worker, which is used to identify it across different browser contexts. This name is
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Creates a new instance of the SharedWebWorker class, initializing it with the specified name, shared worker, and background service manager. The constructor sets up the communication channel with the shared worker and starts the message port if it is available. The shared worker is responsible for handling concurrent processing across multiple browser contexts, while the background service manager manages the services that can be called from the worker. Proper disposal of resources is handled to ensure efficient cleanup when the worker is no longer needed.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sharedWorker"></param>
        /// <param name="webAssemblyServices"></param>
        public SharedWebWorker(string name, SharedWorker sharedWorker, IBackgroundServiceManager webAssemblyServices) : base(webAssemblyServices, sharedWorker.Port)
        {
            Name = name;
            _sharedWorker = sharedWorker;
            if (_port is MessagePort port) port.Start();
        }
        /// <summary>
        /// Disposes of the resources used by the SharedWebWorker instance, including terminating the shared worker and cleaning up the message port. This method ensures that all resources are properly released when the worker is no longer needed, preventing memory leaks and ensuring efficient resource management. The disposal process includes invoking any registered disposal events and handling exceptions that may occur during termination. After disposing of the shared worker and message port, it calls the base class's Dispose method to complete the cleanup process.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;
            _sharedWorker?.Dispose();
            _port?.Dispose();
            base.Dispose(disposing);
        }
    }
}
