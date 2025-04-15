namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// An ExtendableEvent that was initially missed while Blazor was loading, but was held using waitUntil() so that Blazor can handle it.<br />
    /// Implementaors will be able to use the WaitResolve and WaitReject methods to resolve or reject the event.<br />
    /// </summary>
    public interface IMissedEvent
    {
        /// <summary>
        /// Resolves the missed ExtendableEvent.<br />
        /// </summary>
        void WaitResolve();
        /// <summary>
        /// Rejects the missed ExtendableEvent.<br />
        /// </summary>
        void WaitReject();
        /// <summary>
        /// Returns true if the event is an ExtendableEvent.<br />
        /// </summary>
        bool IsExtended { get; }
    }
}

