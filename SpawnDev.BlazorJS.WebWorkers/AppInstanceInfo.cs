using System.Text.Json.Serialization;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Information about an instance of a Blazor app
    /// </summary>
    public class AppInstanceInfo
    {
        /// <summary>
        /// The instance's instanceId, a unique and randomly generated Guid string created during BlazorJSRuntime startup 
        /// </summary>
        public string InstanceId { get; set; } = default!;
        /// <summary>
        /// The InstanceId of the instance that created this instance (if one)
        /// </summary>
        public string? OwnerId { get; set; }
        /// <summary>
        /// The id set by the owner (OwnerId) when they created this instance
        /// </summary>
        public string? ChildId { get; set; }
        /// <summary>
        /// The instance's location at startup
        /// </summary>
        public string Url { get; set; } = default!;
        /// <summary>
        /// The instance's app baseUri
        /// </summary>
        public string BaseUrl { get; set; } = default!;
        /// <summary>
        /// The scope the instance is running in
        /// </summary>
        public GlobalScope Scope { get; set; }
        /// <summary>
        /// The name property of the global scope. In shared workers, this is the shared worker name used when there were created.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }
        /// <summary>
        /// If the scope is a dedicated worker, this is the parent's instance id<br/>
        /// If this is a window scope and running in an iframe, this is the parent window's instanceId
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ParentInstanceId { get; set; }
        /// <summary>
        /// The instance's clientId. null if a undetermined.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ClientId { get; set; }
        /// <summary>
        /// The name of the instance's running indicator lock (if one)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? LockName { get; set; }
        /// <summary>
        /// Returns true if this instance is a TaaskPool worker
        /// </summary>
        public bool TaskPoolWorker { get; set; }
    }
}
