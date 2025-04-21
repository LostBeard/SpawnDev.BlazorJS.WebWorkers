using Microsoft.JSInterop;
using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// An Event that was initially missed while Blazor was loading, but was held using waitUntil() so that Blazor can handle it.<br />
    /// </summary>
    internal class MissedFetchEvent : FetchEvent
    {
        ///<inheritdoc/>
        public MissedFetchEvent(IJSInProcessObjectReference _ref) : base(_ref) { }
        /// <summary>
        /// Resolves the FetchEvent.<br />
        /// </summary>
        /// <param name="response"></param>
        public void ResponseResolve(Response response) => JSRef!.CallVoid("responseResolve", response);
        /// <summary>
        /// Rejects the FetchEvent.<br />
        /// </summary>
        public void ResponseReject() => JSRef!.CallVoid("responseReject");
    }
}

