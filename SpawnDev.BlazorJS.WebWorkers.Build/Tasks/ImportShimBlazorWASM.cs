using Microsoft.Build.Framework;
using System;
using System.IO;

namespace SpawnDev.BlazorJS.WebWorkers.Build.Tasks
{
    public class ImportShimBlazorWASM : Microsoft.Build.Utilities.Task
    {

        public string ServiceWorkerAssetsManifest { get; set; }

        [Required]
        public ITaskItem[] StaticWebAsset { get; set; }

        [Required]
        public string ProjectDir { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public string BasePath { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        [Required]
        public bool PatchFramework { get; set; }

        [Required]
        public string PackageContentDir { get; set; }

        [Required]
        public bool DebugSpawnDevWebWorkersBuildTasks { get; set; }

        [Required]
        public bool PublishMode { get; set; }

        public string OutputWwwroot { get; set; }

        public override bool Execute()
        {
            Console.WriteLine($">> ServiceWorkerAssetsManifest: {ServiceWorkerAssetsManifest}");
            if (DebugSpawnDevWebWorkersBuildTasks)
            {
                System.Diagnostics.Debugger.Launch();
            }
            if (!PatchFramework)
            {
                return true;
            }
            OutputWwwroot = Path.GetFullPath(Path.Combine(OutputPath, "wwwroot"));
            PackageContentDir = Path.GetFullPath(PackageContentDir);
            var patcher = new BlazorWASMFrameworkTool(OutputWwwroot, PackageContentDir, ServiceWorkerAssetsManifest);
            // patch Blazor _framework files to allow running in non-window scopes
            patcher.ImportPatch();
            // if the app has an `service-worker-assets.js` file, some hashes may need to be updated due to file patching
            if (PublishMode)
            {
                patcher.VerifyAssetsManifest();
            }
            return true;
        }
    }
}
