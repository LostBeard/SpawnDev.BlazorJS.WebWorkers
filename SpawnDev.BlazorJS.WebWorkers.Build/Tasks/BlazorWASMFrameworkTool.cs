using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpawnDev.BlazorJS.WebWorkers.Build.Tasks
{
    internal class BlazorWASMFrameworkTool
    {
        public string TargetDir { get; private set; }
        public string WwwrootDir { get; private set; }
        public string FrameworkDir { get; private set; }
        public bool IsPublishBuild { get; private set; }
        public string PatchDir { get; private set; }
        public string PatchJSPath { get; private set; }
        public bool UseMinifiedPatch { get; set; } = false;
        public string AssetManifestFileName = "";
        public string AssetManifestFilePath = "";
        bool PatchManifest = false;

        public BlazorWASMFrameworkTool(string wwwrootPath, string contentPath, string assetManifestFile)
        {
            AssetManifestFileName = assetManifestFile;
            PatchDir = Path.GetFullPath(contentPath);
            if (!Directory.Exists(PatchDir))
            {
                throw new Exception("build content directory not found");
            }
            PatchJSPath = Path.Combine(PatchDir, UseMinifiedPatch ? "patch.min.js" : "patch.js");
            WwwrootDir = Path.GetFullPath(wwwrootPath);
            if (!Directory.Exists(WwwrootDir))
            {
                throw new Exception("wwwroot directory not found in the targetDir");
            }
            FrameworkDir = Path.Combine(WwwrootDir, "_framework");
            if (!Directory.Exists(FrameworkDir))
            {
                throw new Exception("_framework directory not found");
            }
            //Console.WriteLine($"frameworkPath: [{FrameworkDir}]");
            var dotnetJSPublishPath = Path.Combine(FrameworkDir, "dotnet.js");
            IsPublishBuild = File.Exists(dotnetJSPublishPath);
            if (!IsPublishBuild && !File.Exists(dotnetJSPublishPath))
            {
                throw new Exception("dotnet.js not found");
            }
            //Console.WriteLine($"IsPublishBuild: {IsPublishBuild}");
            if (!File.Exists(PatchJSPath))
            {
                throw new Exception("patch.js not found");
            }
            if (!string.IsNullOrEmpty(AssetManifestFileName))
            {
                var assetManifestFilePath = Path.Combine(WwwrootDir, AssetManifestFileName);
                if (File.Exists(assetManifestFilePath))
                {
                    AssetManifestFilePath = Path.GetFullPath(assetManifestFilePath);
                }
            }
            PatchManifest = !string.IsNullOrEmpty(AssetManifestFilePath) && File.Exists(AssetManifestFilePath);
        }
        // https://www.digitalocean.com/community/tools/minify
        // if there is a minified version of the script that is newer than the non-minified use it
        string GetPatchFilePath(string name)
        {
            var minifiedJSPath = Path.Combine(PatchDir, $"{name}.min.js");
            var minifiedJSFInfo = new FileInfo(minifiedJSPath);
            var normalJSPath = Path.Combine(PatchDir, $"{name}.js");
            var normalJSInfo = new FileInfo(normalJSPath);
            if (!minifiedJSFInfo.Exists) return normalJSPath;
            if (!normalJSInfo.Exists) return minifiedJSPath;
            return minifiedJSFInfo.LastWriteTimeUtc > normalJSInfo.LastWriteTimeUtc ? minifiedJSPath : normalJSPath;
        }
        static string patchedTag = "// FRAMEWORK-PATCHED";
        string ReadPatchFile(string name)
        {
            return File.ReadAllText(GetPatchFilePath(name));
        }
        // publish builds put all the Blazor js files in $(TargetDir)wwwroot\_framework
        // non-publish builds put the Blazor js files in $(TargetDir)wwwroot\_framework AND $(TargetDir)
        /// <summary>
        /// This method patches the Blazor WebAssembly Javascript files so that Blazor can start in environments that do not support dynamic imports.<br/>
        /// </summary>
        public void ImportPatch()
        {
            var frameworkDirJSFiles = Directory.GetFiles(FrameworkDir, "*.js");
            var patchedTag = "// FRAMEWORK-PATCHED";
            foreach (var jsFile in frameworkDirJSFiles)
            {
                //Console.WriteLine(jsFile);
                var filename = Path.GetFileName(jsFile);
                var js = File.ReadAllText(jsFile);
                var orig = js;
                if (js.Contains(patchedTag))
                {
                    // already patched. skip.
                    continue;
                }
                var modified = false;
                var newLine = js.Contains("\r\n") ? "\r\n" : (js.Contains("\n") ? "\n" : "\r\n");
                // getBaseURI:()=>document.baseURI
                // getBaseURI:()=>globalThis.blazorConfig.documentBaseURI
                js = Regex.Replace(js, @"\bgetBaseURI:\(\)=>(globalThis\.)?document\.baseURI\b", @"getBaseURI:()=>globalThis.blazorConfig.documentBaseURI");
                // ***************************************************************************************
                // ***************************************************************************************
                // patch document.basURI which Blazor uses to determine where Blazor assets are located.
                // in workers this will be determined based on location.href
                // in browser extensions running in content mode, this will be (globalThis.chrome || globalThis.browser).runtime.getURL('./')
                js = Regex.Replace(js, @"\bglobalThis\.document\.baseURI\b", @"globalThis.blazorConfig.blazorBaseURI");
                js = Regex.Replace(js, @"\bdocument\.baseURI\b", @"globalThis.blazorConfig.blazorBaseURI");
                // ***************************************************************************************
                // ***************************************************************************************
                // Patch import so that it uses importShim from the patched code instead of dynamic imports
                var filenameJson = JsonSerializer.Serialize(filename);
                // the filename will be hardcoded into the script when imported via importShim, the filename will used to 
                // patch import( -> importShim(
                js = Regex.Replace(js, @"\bimport\(", @"importShim(");
                // patch import.meta -> importShim.meta('FILENAME')
                js = Regex.Replace(js, @"\bimport\.meta\b", $@"importShim.meta({filenameJson})");
                // ***************************************************************************************
                // ***************************************************************************************
                // Module scripts will be wrapped in a function and have `exports` captured so they can be returned from importShim
                // patch export
                // ** dotnet 7.0 ((
                // dotnet.7.0.0.amub20uvka.js
                // export default createDotnetRuntime
                // ** dotnet 8.0 **
                // dotnet.js
                // export{Be as default,Fe as dotnet,We as exit};
                // dotnet.runtime.8.0.1.rswtxkdyko.js
                // export{Ll as configureEmscriptenStartup,Rl as configureRuntimeStartup,Bl as configureWorkerStartup,Ol as initializeExports,Uo as initializeReplacements,b as passEmscriptenInternals,g as setRuntimeGlobals};
                // dotnet.native.8.0.1.sz7bf40gus.js
                // export default createDotnetRuntime;
                var exportPatched = false;
                if (RegexReplaceLast(ref js, @"\bexport[ \t]*(\{[^}]+\})", (m) =>
                {
                    var newExportStr = Regex.Replace(m.Groups[1].Value, @"([^\s{},]+)\s+as\s+([^\s,{}]+)", "$2:$1");
                    return $"globalThis.exportShim({filenameJson},{newExportStr})";
                }))
                {
                    exportPatched = true;
                }
                else if (RegexReplaceLast(ref js, @"\bexport[ \t]+function[ \t]+([^ \t(]+)", @"globalThis.exportShim(" + filenameJson + @").$1=function $1"))
                {
                    exportPatched = true;
                }
                else if (RegexReplaceLast(ref js, @"\bexport[ \t]+async[ \t]+function[ \t]+([^ \t(]+)", @"globalThis.exportShim(" + filenameJson + @").$1=async function $1"))
                {
                    exportPatched = true;
                }
                else if (RegexReplaceLast(ref js, @"\bexport[ \t]+default[ \t]+([^ \t;]+)", @"globalThis.exportShim(" + filenameJson + @").default=$1"))
                {
                    exportPatched = true;
                }
                if (exportPatched)
                {
                    modified = true;
                    // exports were patched. Wrap in a called function.
                    js = "(()=>{" + newLine + js + newLine + "})();" + newLine;
                }
                // if the blazor entry point, add patch support code
                if (filename == "blazor.webassembly.js")
                {
                    // this is blazor entry point.
                    // add patch support code to this file
                    modified = true;
                    //var workerDom = ReadPatchFile("worker-dom");
                    var prePatch = ReadPatchFile("patch");
                    var postPatch = ReadPatchFile("patch-post");
                    js = $"{prePatch} {newLine} {js} {newLine} {postPatch}";
                }
                var changed = orig != js;
                if (changed)
                {
                    Console.WriteLine($"Patched: {filename}");
                    // add patched marker
                    js = patchedTag + newLine + js;
                    // save patched version
                    File.WriteAllText(jsFile, js);
                }
            }
        }
        public void VerifyAssetsManifest()
        {
            if (string.IsNullOrEmpty(AssetManifestFilePath) || !File.Exists(AssetManifestFilePath))
            {
                return;
            }
            var manifestUpdated = false;
            var assetsManifestFile = ReadServiceWorkerAssetsManifest(AssetManifestFilePath);
            var entryCount = assetsManifestFile?.Assets?.Count ?? 0;
            if (entryCount == 0)
            {
                return;
            }
            var version = assetsManifestFile.Version;
            Console.WriteLine($"Asset manifest loaded: {entryCount} {version}");
            foreach (var entry in assetsManifestFile.Assets)
            {
                var relativePath = entry.Url.Replace('/', Path.DirectorySeparatorChar);
                var filePath = Path.Combine(WwwrootDir, relativePath);
                if (!File.Exists(filePath))
                {
                    // skip
                    continue;
                }
                var fileHash = $"sha256-{FileHasher.GetFileHashBase64(filePath)}";
                if (fileHash != entry.Hash)
                {
                    entry.Hash = fileHash;
                    manifestUpdated = true;
                    Console.WriteLine($"Asset manifest entry updated: {entry.Url} {fileHash}");
                }
            }
            if (manifestUpdated)
            {
                WriteAssetManifest(AssetManifestFilePath, assetsManifestFile);
                Console.WriteLine($"Asset manifest was updated");
            }
            else
            {
                Console.WriteLine($"Asset manifest was not modified");
            }
        }
        private static AssetManifest ReadServiceWorkerAssetsManifest(string assetsManifestPath)
        {
            var jsContents = File.ReadAllText(assetsManifestPath);
            var jsonStart = jsContents.IndexOf('{');
            var jsonLength = jsContents.LastIndexOf('}') - jsonStart + 1;
            var json = jsContents.Substring(jsonStart, jsonLength);
            return JsonSerializer.Deserialize<AssetManifest>(json, DefaultJsonSerializerOptions);
        }
        static JsonSerializerOptions DefaultJsonSerializerOptions { get; set; } = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        static void WriteAssetManifest(string assetsManifestPath, AssetManifest assetManifest)
        {
            var json = JsonSerializer.Serialize(assetManifest, DefaultJsonSerializerOptions);
            var js = $"self.assetsManifest = {json};";
            File.WriteAllText(assetsManifestPath, js);
        }
        static bool RegexReplaceLast(ref string subject, string pattern, Func<Match, string> ifFound)
        {
            MatchCollection matches;
            if ((matches = Regex.Matches(subject, pattern)).Count > 0)
            {
                var m = matches.Last();
                var replaceWith = ifFound(m);
                var preStr = subject.Substring(0, m.Index);
                var postStr = subject.Substring(m.Index + m.Length);
                subject = preStr + replaceWith + postStr;
                return true;
            }
            return false;
        }
        static bool RegexReplaceLast(ref string subject, string pattern, string replacement)
        {
            MatchCollection matches;
            if ((matches = Regex.Matches(subject, pattern)).Count > 0)
            {
                var m = matches.Last();
                var replaceWith = Regex.Replace(m.Value, pattern, replacement);
                var preStr = subject.Substring(0, m.Index);
                var postStr = subject.Substring(m.Index + m.Length);
                subject = preStr + replaceWith + postStr;
                return true;
            }
            return false;
        }
    }
    public static class MatchCollectionExtensions
    {
        public static Match Last(this MatchCollection _this) => _this[_this.Count - 1];
    }
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
    /// <summary>
    /// Asset manifest
    /// </summary>
    public class AssetManifest
    {
        /// <summary>
        /// Assets
        /// </summary>
        public List<ManifestAsset> Assets { get; set; }
        /// <summary>
        /// Version
        /// </summary>
        public string Version { get; set; }
    }
}
