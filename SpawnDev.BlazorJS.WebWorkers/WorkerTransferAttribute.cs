namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Specifies the modes for transferring worker-related data, indicating whether required, all, or no transferable
    /// values should be included in the operation.
    /// </summary>
    /// <remarks>Use this enumeration to control the scope of data transfer when performing worker-related
    /// operations. Selecting the appropriate mode can help optimize performance and ensure that only necessary data is
    /// transferred.</remarks>
    public enum WorkerTransferMode
    {
        /// <summary>
        /// Transfer required transferable values only.<br/>
        /// </summary>
        TransferRequired,
        /// <summary>
        /// Transfer all transferable values, both required and optional.<br/>
        /// </summary>
        TransferAll,
        /// <summary>
        /// Indicates that no data transfer operation is desired.
        /// </summary>
        TransferNone
    }
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
        /// <param name="transfer"></param>
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
        /// <param name="transfer"></param>
        /// <param name="depth"></param>
        public WorkerTransferAttribute(WorkerTransferMode transfer, int depth)
        {
            Transfer = transfer;
            Depth = depth;
        }
        /// <summary>
        /// Default transfer mode when not specified
        /// </summary>
        public static WorkerTransferAttribute TransferRequiredDefault { get; } = new WorkerTransferAttribute(WorkerTransferMode.TransferRequired, 3);
    }
}
