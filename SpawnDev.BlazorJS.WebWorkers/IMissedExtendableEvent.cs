namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// An ExtendableEvent that was initially missed while Blazor was loading, but was held using waitUntil() so that Blazor can handle it.<br />
    /// Implementaors will be able to use the WaitResolve and WaitReject methods to resolve or reject the event.<br />
    /// </summary>
    public interface IMissedExtendableEvent
    {
        /// <summary>
        /// Resolves the ExtendableEvent.<br />
        /// </summary>
        void WaitResolve();
        /// <summary>
        /// Rejects the ExtendableEvent.<br />
        /// </summary>
        void WaitReject();
    }
}

