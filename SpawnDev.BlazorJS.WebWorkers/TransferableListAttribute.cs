namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Use the TransferableList attribute to mark a single method parameter as an explicit transferable list, indicating what objects to transfer.<br/>
    /// This attribute should only be used on a single parameter that has a type that implements `IEnumerable&lt;object>`.<br/>
    /// If TransferableList is used, the WorkerTransferAttribute should not be used on other parameters and will be ignored.<br/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class TransferableListAttribute : Attribute
    {

    }
}
