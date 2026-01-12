namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// The WorkerTransferAttribute is used to mark values that should be added to the transferables list when sent to another context.<br/>
    /// Ommitting this attribute will result in the default behavior, which is to transfer only required transferable values.<br/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class WorkerTransferAttribute : Attribute
    {
        /// <summary>
        /// The maximum property depth to traverse for transferable values.<br/>
        /// </summary>
        public int Depth { get; init; } = 3;
        /// <summary>
        /// Transfer mode<br/>
        /// TransferRequired - transfer required transferable values only<br/>
        /// TransferAll - transfer all transferable values, both required and optional<br/>
        /// TransferNone - do not transfer any transferable values<br/>
        /// </summary>
        public WorkerTransferMode Transfer { get; init; } = WorkerTransferMode.TransferAll;
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="transfer">transfer ? TransferAll : TransferNone</param>
        public WorkerTransferAttribute(bool transfer)
        {
            Transfer = transfer ? WorkerTransferMode.TransferAll : WorkerTransferMode.TransferNone;
        }
        /// <summary>
        /// New instance
        /// </summary>
        public WorkerTransferAttribute() { }
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="depth"></param>
        public WorkerTransferAttribute(int depth)
        {
            Depth = depth;
        }
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="transfer"></param>
        public WorkerTransferAttribute(WorkerTransferMode transfer)
        {
            Transfer = transfer;
        }
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="transfer">Worekr transfer mode</param>
        /// <param name="depth">Max property depth</param>
        public WorkerTransferAttribute(WorkerTransferMode transfer, int depth)
        {
            Transfer = transfer;
            Depth = depth;
        }
        /// <summary>
        /// Default transfer mode when not specified.<br/>
        /// WorkerTransferMode.TransferRequired, Depth = 3
        /// </summary>
        public static WorkerTransferAttribute TransferRequiredDefault { get; } = new WorkerTransferAttribute(WorkerTransferMode.TransferRequired, 3);
    }
}
