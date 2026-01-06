namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Use this attribute to mark a IEnumerable&lt;object> parameter as an explicit transferable list indicating what objects to transfer when commincating with workers.<br/>
    /// This attribute should only be used on a single paramter of type IEnumerable&lt;object>.<br/>
    /// If used, the WorkerTransferAttribute should not be used on other paramters and will be ignored.<br/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class TransferableListAttribute : Attribute
    {

    }
}
