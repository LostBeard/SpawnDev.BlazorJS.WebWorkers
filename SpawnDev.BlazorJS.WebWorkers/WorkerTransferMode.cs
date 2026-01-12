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
}
