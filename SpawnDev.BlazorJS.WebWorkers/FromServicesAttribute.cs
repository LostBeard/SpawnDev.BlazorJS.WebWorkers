namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Method parameters marked with the FromServices attribute will be resolved from the fulfilling side's service provider
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromServicesAttribute : Attribute { }

    /// <summary>
    /// Method parameters marked with the FromLocal attribute will be resolved from the fulfilling side
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromLocalAttribute : Attribute { }
}
