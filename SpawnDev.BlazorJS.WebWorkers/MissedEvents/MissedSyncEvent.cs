using Microsoft.JSInterop;
using SpawnDev.BlazorJS.JSObjects;

namespace SpawnDev.BlazorJS.WebWorkers
{
    internal class MissedSyncEvent : SyncEvent, IMissedEvent
    {
        ///<inheritdoc/>
        public MissedSyncEvent(IJSInProcessObjectReference _ref) : base(_ref) { }
        ///<inheritdoc/>
        public void WaitResolve() => JSRef!.CallVoid("waitResolve");
        ///<inheritdoc/>
        public void WaitReject() => JSRef!.CallVoid("waitReject");
        ///<inheritdoc/>
        public bool IsExtended => !JSRef!.IsUndefined("waitResolve");
    }
}

