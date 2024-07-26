namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Used to modify call options for SpawnDev.BlazorJS.WebWorkers.RemoteDispatcher
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RemoteCallableAttribute : Attribute
    {
        /// <summary>
        /// Methods with NoReply = true will not send exceptions or results back to the caller<br/>
        /// That makes NoReply calls quicker and useful for broadcast/event messages<br/>
        /// This should only be used on methods that return void or Task (un-typed) and<br/>
        /// there may be issues with serialization of some types
        /// </summary>
        public bool NoReply { get; set; }
        /// <summary>
        /// Gets or sets a comma delimited list of roles that are allowed to access the resource.<br/>
        /// RemoteDispatcher.User will be checked for the specified roles.<br/>
        /// It is up to the implementing app to handle role management in RemoteDispatcher.User<br/>
        /// If no roles are set, no role based restrictions will be applied
        /// </summary>
        public string? Roles { get; set; }
    }
}
