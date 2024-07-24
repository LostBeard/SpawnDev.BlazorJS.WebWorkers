namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Basic instance information
    /// </summary>
    public class ServiceCallDispatcherInfo
    {
        /// <summary>
        /// From the instance's BlazorJSRuntime.InstanceId property
        /// </summary>
        public string InstanceId { get; init; } = "";
        /// <summary>
        /// The Javascript globalThis class name<br/>
        /// - Window<br/>
        /// - DedicatedWorkerGlobalScope<br/>
        /// - SharedWorkerGlobalScope<br/>
        /// - ServiceWorkerGlobalScope
        /// </summary>
        public string GlobalThisTypeName { get; init; } = "";
    }
}
