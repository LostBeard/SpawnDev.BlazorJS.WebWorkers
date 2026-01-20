// Todd Tanner
// 2024
// this script is loaded before the pre-patched Blazor startup script and handles the calls that have been converted from `import` to `importShim`
// Blazor WASM .csproj must use <WebWorkerPatchFramework>true</WebWorkerPatchFramework> in a PropertyGroup to pre-patch the Blazor Javascript framework files.
// - use importShim instead of import
// - use exportShim instead of export
// this allows the Blazor _framework scripts to load in browser any scope (window, worker, shared worker, service worker)
// requires SpawnDev.BlazorJS.WebWorkers when running in non-window context to provide a faux document and window environment
if (!globalThis.importShim) {
    //console.log('Defining importShim');
    // if loaded into browser extension content mode, document and history will be undefined, but their globalThis versions are set.
    // should be harmless in every other situation
    if (typeof document === 'undefined' && globalThis.document) document = globalThis.document;
    if (typeof history === 'undefined' && globalThis.history) history = globalThis.history;
    // create globalThis.blazorConfig if it does not exist
    globalThis.blazorConfig = globalThis.blazorConfig ?? {};
    // add defaults to globalThis.blazorConfig
    globalThis.blazorConfig = Object.assign(
        {
            documentBaseURI: globalThis.document ? globalThis.document.baseURI : new URL('./', globalThis.location.href).toString(),
            blazorBaseURI: '',
            frameworkFolderName: '_framework',
            contentFolderName: '_content',
        },
        globalThis.blazorConfig
    );
    if (!globalThis.blazorConfig.blazorBaseURI) {
        globalThis.blazorConfig.blazorBaseURI = (function () {
            var uri = new URL(`./`, location.href);
            if (uri.pathname.includes(`${globalThis.blazorConfig.contentFolderName}/`)) {
                var subpath = uri.pathname.substring(0, uri.pathname.indexOf(`${globalThis.blazorConfig.contentFolderName}/`));
                return new URL(subpath, location.href).toString();
            } else if (uri.pathname.includes(`${globalThis.blazorConfig.frameworkFolderName}/`)) {
                var subpath = uri.pathname.substring(0, uri.pathname.indexOf(`${globalThis.blazorConfig.frameworkFolderName}/`));
                return new URL(subpath, location.href).toString();
            }
            return uri.toString();
        })();
    }
    if (typeof globalThis.constructor.name === 'undefined' && globalThis.window) {
        // Running in Firefox extension content mode
        globalThis.constructor.name = 'Window';
    }
    // sets the document.baseURI to Blazor app's base url
    if (globalThis.constructor.name !== 'Window' && globalThis.document) {
        globalThis.document.baseURI = globalThis.blazorConfig.blazorBaseURI;
    }
    // patched _framework scripts will call exportShim with their filename and optionally their exports
    // returns the script's exports object which may be modified to finish the export
    globalThis.exportShimValues = {};
    globalThis.exportShim = (filename, moduleExports) => {
        if (moduleExports) {
            globalThis.exportShimValues[filename] = moduleExports;
        } else if (!globalThis.exportShimValues[filename]) {
            globalThis.exportShimValues[filename] = {};
        }
        return globalThis.exportShimValues[filename];
    };
    function hasImportScripts() {
        if (typeof globalThis.importScripts !== void 0) {
            try {
                importScripts('./spawndev.blazorjs.webworkers.empty.js');
                //console.log('importScripts supported');
                return true;
            } catch { }
        }
        //console.log('importScripts not supported');
        return false;
    }
    globalThis.importScriptsSupported = hasImportScripts();
    // a custom resolver can be used if set
    globalThis.importShimResolver = null;
    // dynamic import shim for patched _framework scripts
    // uses dynamic import in Window scope
    // uses importScripts in non-window scopes running in 'classic' mode (DedicatedWorker, SharedWorker, ServiceWorker)
    // uses static imports in non-window scopes running in 'module' mode (DedicatedWorker, SharedWorker, ServiceWorker)
    globalThis.dynamicImportSupported = globalThis.constructor.name === 'Window';
    globalThis.importShim = function (moduleName) {
        var filename = moduleName.split('?')[0].split('#')[0]
        filename = filename.indexOf('/') === -1 ? filename : filename.substring(filename.lastIndexOf('/') + 1);
        filename = filename.substring(0, filename.lastIndexOf('.js') + 3);
        //console.log(`importShim: ${moduleName}`, filename);
        return new Promise(async function (resolve, reject) {
            // _framework module scipts have been modified to call exportShim instead of using export { Var as ExportName, ... }, export default ..., etc.
            if (globalThis.exportShimValues[filename]) {
                var moduleExports = globalThis.exportShimValues[filename];
                //console.log(`Pre-resolved: ${moduleName}`, filename, moduleExports);
                resolve(moduleExports);
                return;
            }
            if (globalThis.importShimResolver) {
                var tmp = await globalThis.importShimResolver(moduleName, filename);
                if (tmp !== void 0 || globalThis.exportShimValues[filename] !== void 0) {
                    var moduleExports = globalThis.exportShimValues[filename] ?? {};
                    tmp ??= {};
                    globalThis.exportShimValues[filename] = Object.assign(moduleExports, tmp);
                    moduleExports = globalThis.exportShimValues[filename];
                    resolve(moduleExports);
                    return;
                }
            }
            if (globalThis.dynamicImportSupported !== false) {
                try {
                    var tmp = await import(moduleName);
                    globalThis.dynamicImportSupported = true;
                    // if the script called exportShim, it may have already set an exportShimValues[filename], so map the return value to the existing on (if one)
                    var moduleExports = globalThis.exportShimValues[filename] ?? {};
                    globalThis.exportShimValues[filename] = Object.assign(moduleExports, tmp);
                    moduleExports = globalThis.exportShimValues[filename];
                    resolve(moduleExports);
                    return;
                } catch {
                    // dynamic import not supported
                    globalThis.dynamicImportSupported = false;
                }
            }
            if (globalThis.importScriptsSupported) {
                try {
                    importScripts(moduleName);
                    var moduleExports = globalThis.exportShimValues[filename] ?? {};
                    resolve(moduleExports);
                } catch (e) {
                    reject(e);
                }
                return;
            }
            console.warn(`${globalThis.constructor.name} - Unresolved import: ${filename}. To silence this warning set "globalThis.exportShimValues['${filename}']"`);
            // unsupported environment
            resolve({});
        });
    };
    // import.meta has been patched in module scripts to call this method with their script name instead
    // that way we can determine the correct meta.url property
    globalThis.importShim.meta = function (filename) {
        var meta = {
            // only scripts
            url: new URL(`${globalThis.blazorConfig.frameworkFolderName}/${filename}`, globalThis.blazorConfig.blazorBaseURI).toString(),
        };
        return meta;
    };
}