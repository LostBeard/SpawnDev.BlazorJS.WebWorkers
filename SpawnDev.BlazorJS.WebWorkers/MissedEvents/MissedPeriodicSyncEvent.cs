using Microsoft.JSInterop;
using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.BlazorJS.WebWorkers
{
    internal class MissedPeriodicSyncEvent : PeriodicSyncEvent, IMissedEvent
    {
        ///<inheritdoc/>
        public MissedPeriodicSyncEvent(IJSInProcessObjectReference _ref) : base(_ref) { }
        ///<inheritdoc/>
        public void WaitResolve() => JSRef!.CallVoid("waitResolve");
        ///<inheritdoc/>
        public void WaitReject() => JSRef!.CallVoid("waitReject");
        ///<inheritdoc/>
        public bool IsExtended => !JSRef!.IsUndefined("waitResolve");
    }
}

