namespace SpawnDev.BlazorJS.WebWorkers
{
    /// <summary>
    /// Asset manifest
    /// </summary>
    public class AssetManifest
    {
        /// <summary>
        /// Assets
        /// </summary>
        public List<ManifestAsset> Assets { get; set; } = default!;
        /// <summary>
        /// Version
        /// </summary>
        public string Version { get; set; } = default!;
    }
}
