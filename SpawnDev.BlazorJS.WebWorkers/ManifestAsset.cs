namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Manifest asset info
    /// </summary>
    public class ManifestAsset
    {
        /// <summary>
        /// File content hash. This should be the base-64-formatted SHA256 value.
        /// </summary>
        public string Hash { get; set; }
        /// <summary>
        /// Asset URL. Normally this will be relative to the application's base href.
        /// </summary>
        public string Url { get; set; }
    }
}
