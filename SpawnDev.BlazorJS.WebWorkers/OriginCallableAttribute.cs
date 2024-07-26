namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Used to modify call options for SpawnDev.BlazorJS.WebWorkers.ServiceCallDispatcher
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class OriginCallableAttribute : Attribute
    {
        /// <summary>
        /// Methods with NoReply = true will not send exceptions or results back to the caller<br/>
        /// That makes NoReply calls quicker and useful for broadcast/event messages<br/>
        /// This should only be used on methods that return void or Task (un-typed) and<br/>
        /// there may be issues with serialization of some types
        /// </summary>
        public bool NoReply { get; set; }
    }
}
