namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// The WorkerTransferAttribute is used to mark values that should be added to the transferred list when send to another context
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class WorkerTransferAttribute : Attribute
    {
        /// <summary>
        /// If true, the 
        /// </summary>
        public bool Transfer { get; private set; } = true;
        /// <summary>
        /// New instance
        /// </summary>
        /// <param name="transfer"></param>
        public WorkerTransferAttribute(bool transfer = true) => Transfer = transfer;
    }
}
