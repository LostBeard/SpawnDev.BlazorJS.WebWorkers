namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Use this attribute to mark a parameter as a transferable list indicating what objects to transfer when posting to a web worker.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class TransferableListAttribute : Attribute
    {

    }
}
