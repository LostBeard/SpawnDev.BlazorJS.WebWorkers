const params = new Proxy(new URLSearchParams(globalThis.location.search), { get: (searchParams, prop) => searchParams.get(prop), });
globalThis.autoStart ??= params.autoStart !== '0';

// importShim is handles the pre-patched Blazor framework scripts that have had their `import` and `export` calls changed to `importShim` and `exportShim`
//import * as _importShim from "./spawndev.blazorjs.webworkers.import-shim.js"
// load the event holder that holds events while Blazor loads
import * as _eventHolder from "./spawndev.blazorjs.webworkers.event-holder.js"
// Faux-Env creates a minimal window scope like environment (if needed) for Blazor to run in a Web Worker, Shared Worker, or Service Worker.
import * as _fauxEnv from "./spawndev.blazorjs.webworkers.faux-env.js"
// if the document is a fake document (faux-env) init it now
if (document.initDocument) document.initDocument();
// Use static imports to load Blazor pre-patched scripts
import * as _blazor from "./_framework/blazor.webassembly.js"
import * as _dotnet from "./_framework/dotnet.js"
import * as _dotnetNative from "./_framework/dotnet.native.js"
import * as _dotnetRuntime from "./_framework/dotnet.runtime.js"
// SpawnDev.BlazorJS Javascript library is required
import * as _bjs from "./SpawnDev.BlazorJS.lib.module.js"
globalThis.exportShimValues['SpawnDev.BlazorJS.lib.module.js'] = _bjs;

// if library modules are needed, a custom loader should be created that loads those modules and then loads this file, optionally using a custom Blazor.start()

// start Blazor if autoStart hasn't been disabled
if (globalThis.autoStart) {
    Blazor._startTask = Blazor.start();
}
