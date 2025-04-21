using Microsoft.JSInterop;
using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// An Event that was initially missed while Blazor was loading, but was held using waitUntil() so that Blazor can handle it.<br />
    /// </summary>
    internal class MissedCanMakePaymentEvent : CanMakePaymentEvent
    {
        ///<inheritdoc/>
        public MissedCanMakePaymentEvent(IJSInProcessObjectReference _ref) : base(_ref) { }
        /// <summary>
        /// Resolves the CanMakePaymentEvent.<br />
        /// </summary>
        /// <param name="response"></param>
        public void ResponseResolve(bool response) => JSRef!.CallVoid("responseResolve", response);
        /// <summary>
        /// Rejects the CanMakePaymentEvent.<br />
        /// </summary>
        public void ResponseReject() => JSRef!.CallVoid("responseReject");
        ///<inheritdoc/>
        public bool IsExtended => !JSRef!.IsUndefined("responseResolve");
    }
}

