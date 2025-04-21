using Microsoft.JSInterop;
using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// An Event that was initially missed while Blazor was loading, but was held using waitUntil() so that Blazor can handle it.<br />
    /// </summary>
    internal class MissedPaymentRequestEvent : PaymentRequestEvent, IMissedEvent
    {
        ///<inheritdoc/>
        public MissedPaymentRequestEvent(IJSInProcessObjectReference _ref) : base(_ref) { }
        ///<inheritdoc/>
        public void WaitResolve() => JSRef!.CallVoid("waitResolve");
        ///<inheritdoc/>
        public void WaitReject() => JSRef!.CallVoid("waitReject");
        ///<inheritdoc/>
        public bool IsExtended => !JSRef!.IsUndefined("waitResolve");
    }
}

