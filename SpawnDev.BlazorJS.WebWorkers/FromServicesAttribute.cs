namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Method parameters marked with the FromServices attribute will be resolved from the fulfilling side's service provider
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromServicesAttribute : Attribute { }

#if !NET8_0_OR_GREATER
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromKeyedServicesAttribute : Attribute
    {
        /// <summary>
        /// Creates a new <see cref="FromKeyedServicesAttribute"/> instance.
        /// </summary>
        /// <param name="key">The key of the keyed service to bind to.</param>
        public FromKeyedServicesAttribute(object key) => Key = key;

        /// <summary>
        /// The key of the keyed service to bind to.
        /// </summary>
        public object Key { get; }
    }
#endif

    /// <summary>
    /// Method parameters marked with the FromLocal attribute will be resolved from the fulfilling side
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromLocalAttribute : Attribute { }
}
