'use strict';

// Todd Tanner
// 2022 - 2023
// SpawnDev.BlazorJS.WebWorkers
// spawndev.blazorjs.webworkers.js
// Update 2024-07-15: this script will now be copied to the app wwwroot path instead of the rcl _content path (wwwroot/_content/SpawnDev.BlazorJS.WebWorkers/spawndev.blazorjs.webworkers.js)
// this script loads a fake window and document environment
// to enable loading the Blazor WASM app in a DedicatedWorkerGlobalScope, SharedWorkerGlobalScope or ServiceWorkerGlobalScope
// This script is loaded when using 'classic' mode DedicatedWorker, SharedWorker, or ServiceWorker.
// When using 'module' mode, the spawndev.blazorjs.webworkers.module.js script is loaded instead.

var globalThisTypeName = self.constructor.name;
var disableHotReload = true;

function getParameterByName(name, url = location.href) {
    name = name.replace(/[\[\]]/g, '\\$&');
    var regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)'),
        results = regex.exec(url);
    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, ' '));
}
var queryParams = new Proxy({}, {
    get: (orig, prop) => getParameterByName(prop),
});

// a query param can be used to set the index.html file url
var indexHtml = queryParams.indexHtml ?? './';
if (typeof indexHtml === 'string' && ['true', '1'].indexOf(indexHtml.toLowerCase()) !== -1) indexHtml = 'index.html';
// below switches the indexHtml path to index.html if running in a browser extension
var browserExtension = (self.browser && self.browser.runtime && self.browser.runtime.id) || (self.chrome && self.chrome.runtime && self.chrome.runtime.id) || location.href.indexOf('chrome-extension') === 0;
if (browserExtension) indexHtml = 'index.html';

var verboseWebWorkers = !!queryParams.verbose;
var debugMode = !!queryParams.debugMode;
var isServiceWorkerScope = globalThisTypeName == 'ServiceWorkerGlobalScope';
var importServiceWorkerAssets = queryParams.importServiceWorkerAssets;

var consoleLog = function () {
    if (!verboseWebWorkers) return;
    console.log(...arguments);
};

consoleLog('spawndev.blazorjs.webworkers: started');
consoleLog('location.href', location.href);

if (importServiceWorkerAssets) {
    if (importServiceWorkerAssets.indexOf('.js') !== -1) {
        consoleLog('importing asset manifest', importServiceWorkerAssets);
        importScripts(importServiceWorkerAssets);
    } else {
        consoleLog('importing asset manifest', 'service-worker-assets.js');
        importScripts('service-worker-assets.js');
    }
}

// location.href is this script
// - location.href == 'https://localhost:7191/_content/SpawnDev.BlazorJS.WebWorkers/spawndev.blazorjs.webworkers.js?verbose=false'
// or a service worker script
// - location.href == 'https://localhost:7191/service-worker.js'
// if documentBaseURIIsModified == true, 
// - fetch will be replaced with one that uses the modified documentBaseURI as its base path for relative path fetches as is expected in Blazor WASM apps
var documentBaseURIIsModified = false;
var documentBaseURI = (function () {
    var uri = new URL(`./`, location.href);
    if (uri.pathname.includes('_content/')) {
        documentBaseURIIsModified = true;
        var subpath = uri.pathname.substring(0, uri.pathname.indexOf('_content/'));
        return new URL(subpath, location.href).toString();
    }
    return uri.toString();
})();
consoleLog('documentBaseURI', documentBaseURI);

function getAppURL(relativePath) {
    var ret = new URL(relativePath, documentBaseURI).toString();
    if (self.indexImportMaps) {
        for (var mapSet of self.indexImportMaps) {
            if (mapSet.imports[ret]) {
                return mapSet.imports[ret];
            }
        }
    }
    return ret;
}
consoleLog('spawndev.blazorjs.webworkers: loading fake window environment');
// event holder for async blazor startup
importScripts(getAppURL('spawndev.blazorjs.webworkers.event-holder.js'));
// faux DOM and document environment
importScripts(getAppURL('spawndev.blazorjs.webworkers.faux-env.js'));
// faux dom and window environment has been created (currently empty)
// set document.baseURI to the apps basePath (which is relative to this scripts path)
document.baseURI = documentBaseURI;
if (disableHotReload) {
    self._dotnet_watch_ws_injected = true;
}

// dynamic import support tested using an empty script
async function hasDynamicImport() {
    // import() is disallowed on ServiceWorkerGlobalScope by the HTML specification.
    // See https://github.com/w3c/ServiceWorker/issues/1356.
    if (globalThisTypeName == 'ServiceWorkerGlobalScope') {
        return false;
    }
    try {
        await import(getAppURL('spawndev.blazorjs.webworkers.empty.js'));
        return true;
    } catch (e) {
        return false;
    }
}

var initWebWorkerBlazor = async function () {
    // patch self.fetch to use document.baseURI for the relative path base path
    if (documentBaseURIIsModified) {
        let fetchOrig = self.fetch;
        self.fetch = function (resource, options) {
            consoleLog("webWorkersFetch", typeof resource, resource);
            if (typeof resource === 'string') {
                // resource is a string
                let newUrl = getAppURL(resource);
                return fetchOrig(newUrl, options);
            } else {
                // resource is a Request object
                // currently not modified. could cause issues if a relative path was used to create the Request object.
                return fetchOrig(resource, options);
            }
        };
    }
    // fetch getText method
    async function getText(href) {
        var response = await fetch(getAppURL(href));
        return await response.text();
    }
    // TODO - detect pre-patched framework or read a config file that indicates state
    var WebWorkerEnabledAttributeName = 'webworker-enabled';
    // set false here, but may be set to true if import maps are found in the index.html and need to be used
    var importMapsFound = false;
    // detect if we need to use a patched framework
    var dynamicImportSupported = await hasDynamicImport();
    if (!dynamicImportSupported) {
        consoleLog("dynamic import is not supported. The framework scripts will be fetched, patched, and then loaded. A CSP script-src rule blocking 'unsafe-eval' may prevent this");
    } else {
        consoleLog('dynamic import is supported. The framework will be loaded as-is.');
    }
    // Get index.html and parse it for scripts
    function getScriptNodes(indexHtmlSrc) {
        var scriptNodes = [];
        var scriptPatt = /<script\s*(.*?)(?:\s*\/>|\s*>(.*?)<\/script>)/gms;
        var attributesPatt = /([^\s=]+)(?:=(?:"([^"]*)"|'([^"]*)'|([^\s=]+)))?/gm;
        var m = scriptPatt.exec(indexHtmlSrc);
        while (m) {
            let scriptNode = {
                attributes: {},
                text: m[2],
            }
            let scriptTagAttributes = m[1];
            let attrMatch = attributesPatt.exec(scriptTagAttributes);
            while (attrMatch) {
                let attrName = attrMatch[1];
                let attrValue = '';
                if (attrMatch[2]) attrValue = attrMatch[2];
                else if (attrMatch[3]) attrValue = attrMatch[3];
                else if (attrMatch[4]) attrValue = attrMatch[4];
                scriptNode.attributes[attrName] = attrValue;
                attrMatch = attributesPatt.exec(scriptTagAttributes);
            }
            scriptNodes.push(scriptNode);
            m = scriptPatt.exec(indexHtmlSrc);
        }
        return scriptNodes;
    }
    // detect the Blazor runtime type
    // can be:
    // '' - none or unknown
    // 'wasm' - Using Blazor WebAssembly standalone runtime
    // 'united' - Using Blazor United runtime
    // do on the fly worker compatiblility patching if needed
    // add webworker-enabled attribute to the runtime if it is not already there
    function prePatchedCheck(jsStr) {
        return jsStr.indexOf('importShim(') !== -1 || jsStr.indexOf('// FRAMEWORK-PATCHED' || jsStr.indexOf('exportShim(') !== -1) !== -1;
    }
    async function detectBlazorRuntime(scriptNodes, overrideUnitedRuntime) {
        for (var scriptNode of scriptNodes) {
            let src = scriptNode.attributes.src;
            if (!src) continue;
            if (src.includes('_framework/blazor.web.')) {
                if (overrideUnitedRuntime) {
                    // blazor united comes with the wasm runtime also
                    // if overrideUnitedRuntime == true, we will use the wasm runtime instead of the united runtime
                    src = src.replace('_framework/blazor.web.', '_framework/blazor.webassembly.');
                    scriptNode.attributes.src = src;
                } else {
                    // modify the united runtime as needed for compatibility with WebWorkers
                    if (typeof scriptNode.attributes[WebWorkerEnabledAttributeName] === 'undefined') {
                        scriptNode.attributes[WebWorkerEnabledAttributeName] = '';
                    }
                    // load script text so we can do some on-the-fly patching to fix compatibility with WebWorkers
                    let jsStr = await getText(src);
                    if (prePatchedCheck(jsStr)) {
                        // already patched. 
                        return {
                            runtime: 'united',
                            prepatched: true,
                        };
                    }
                    // united runtime doesn't start web assembly when it loads by default
                    // it waits until a webassembly rendered component is loaded but that won't happen in a worker so we patch
                    // the runtime to allow access to the method that actually starts webassembly directly
                    // self.__blazorInternal.startLoadingWebAssemblyIfNotStarted()
                    var placeHolderPatt = /(this\.initialComponents\s*=\s*\[\s*\],\s*)/;
                    var m = placeHolderPatt.exec(jsStr);
                    if (m) {
                        jsStr = jsStr.replace(placeHolderPatt, '$1setTimeout(()=>this.startWebAssemblyIfNotStarted(),0),');
                        if (!dynamicImportSupported || importMapsFound) {
                            // fix dynamic imports
                            jsStr = fixModuleScript(jsStr, src);
                        }
                        scriptNode.text = jsStr;
                        return {
                            runtime: 'united',
                            prepatched: false,
                        };
                    } else {
                        console.warn(`Failed to find Blazor United runtime 'placeHolderPatt' in '${src}' for on the fly patching. Will try Blazor WASM runtime '_framework/blazor.webassembly.js' as fallback.`);
                        src = src.replace('_framework/blazor.web.', '_framework/blazor.webassembly.');
                        scriptNode.attributes.src = src;
                    }
                }
            }
            if (src.includes('_framework/blazor.webassembly.')) {
                // modify the wasm runtime as needed for compatibility with WebWorkers
                if (typeof scriptNode.attributes[WebWorkerEnabledAttributeName] === 'undefined') {
                    scriptNode.attributes[WebWorkerEnabledAttributeName] = '';
                }
                if (!dynamicImportSupported || importMapsFound) {
                    // load script text so we can do some on-the-fly patching to fix compatibility with WebWorkers
                    let jsStr = await getText(src);
                    if (prePatchedCheck(jsStr)) {
                        // already patched. 
                        return {
                            runtime: 'wasm',
                            prepatched: true,
                        };
                    }
                    // fix dynamic imports
                    scriptNode.text = fixModuleScript(jsStr, src);
                }
                return {
                    runtime: 'wasm',
                    prepatched: false,
                };
            }
        }
        return null;
    }
    // if a strict content-security-policy prohibits 'unsafe-eval' the below method will not work... sciprts will need to be pre-processed (during publish/build step)
    // import shim used by code that is patched on the fly
    self.importOverride = async function (callerSrc, src) {
        consoleLog('importOverride', src, callerSrc);
        src = new URL(src, callerSrc).toString();
        var jsStr = await getText(src);
        jsStr = fixModuleScript(jsStr, src);
        let fn = new Function(jsStr);
        var ret = fn.apply(createProxiedObject(self), []);
        if (!ret) ret = createProxiedObject({});
        return ret;
    }
    // this method patches 'dynamic import scripts' to work in an environment that does not support 'dynamic import scripts'
    // it is designed for and tested with the Blazor WASM runtime.
    // it may not work on other modules
    /**
     * Patches a Blazor Javascript framework file at runtime to allow loading in a Web Worker, Shared Worker, or Service Worker.
     * @param {string} jsStr The javascript file source code to patch
     * @param {string} src The source URL of the javascript file
     * @returns
     */
    function fixModuleScript(jsStr, src) {
        // handle things that are automatically handled by import
        src = getAppURL(src);
        //var srcFilename = new URL(src).pathname.split('/').pop();
        var scriptUrl = JSON.stringify(src);
        consoleLog('fixModuleScript.scriptUrl', src, scriptUrl);
        // fix import.meta.url - The full URL to the module
        // import.meta.url -> SCRIPT_URL
        jsStr = jsStr.replace(/\bimport\.meta\.url\b/g, scriptUrl);
        // import.meta -> { url: SCRIPT_URL }
        jsStr = jsStr.replace(/\bimport\.meta\b/g, `{ url: ${scriptUrl} }`);
        // import -> importOverride ... importOverride can decide at runtime if it needs to use 'importScripts' or 'await import'
        jsStr = jsStr.replace(/\bimport\(/g, `importOverride(${scriptUrl},`);
        // export
        // https://www.geeksforgeeks.org/what-is-export-default-in-javascript/
        // handle exports from
        // lib modules
        // Ex(_content/SpawnDev.BlazorJS/SpawnDev.BlazorJS.lib.module.js)
        // export function beforeStart(options, extensions) {
        // export function afterStarted(options, extensions) {
        var exportPatt = /\bexport[ \t]+function[ \t]+([^ \t(]+)/g;
        jsStr = jsStr.replace(exportPatt, '_exportsOverride.$1 = function $1');
        // To match: _framework/blazor-hotreload.
        // export async function receiveHotReloadAsync() {
        exportPatt = /\bexport[ \t]+async[ \t]+function[ \t]+([^ \t(]+)/g;
        jsStr = jsStr.replace(exportPatt, '_exportsOverride.$1 = async function $1');
        // handle exports from
        // dotnet.7.0.0.amub20uvka.js
        // export default createDotnetRuntime
        //
        // dotnet 8.0
        // dotnet.native.8.0.1.sz7bf40gus.js
        // export default createDotnetRuntime;
        // dotnet.js
        // export{Be as default,Fe as dotnet,We as exit};
        // dotnet.runtime.8.0.1.rswtxkdyko.js
        // export{Ll as configureEmscriptenStartup,Rl as configureRuntimeStartup,Bl as configureWorkerStartup,Ol as initializeExports,Uo as initializeReplacements,b as passEmscriptenInternals,g as setRuntimeGlobals};
        exportPatt = /\bexport[ \t]+default[ \t]+([^ \t;]+)/g;
        jsStr = jsStr.replace(exportPatt, '_exportsOverride.default = $1');
        // export{Be as default,Fe as dotnet,We as exit};
        // below changes the above line to the below line changing the 'VAR as KEY' to 'KEY:VAR'
        // export{default:Be,dotnet:Fe,exit:We};
        exportPatt = /([a-zA-Z0-9$_]+)\s+as\s+([a-zA-Z0-9$_]+)/g;
        jsStr = jsStr.replace(exportPatt, '$2:$1');
        // export { dotnet, exit, INTERNAL };
        exportPatt = /\bexport\b[ \t]*(\{[^}]+\})/g;
        jsStr = jsStr.replace(exportPatt, '_exportsOverride = Object.assign(_exportsOverride, $1)');
        var modulize = `let _exportsOverride = {}; ${jsStr}; return _exportsOverride;`;
        return modulize;
    }
    async function initializeBlazor() {
        // get index.html text for parsing
        // .Net 10 adds the need to acquire the import map from the index.html
        var indexHtmlSrc = await getText(indexHtml);
        var scriptNodes = getScriptNodes(indexHtmlSrc);
        // find any import maps
        self.indexImportMaps = [];
        for (var scriptNode of scriptNodes) {
            if (scriptNode.attributes['type'] == 'importmap') {
                try {
                    let importMap = JSON.parse(scriptNode.text);
                    let resolvedMaps = {};
                    for (let k in importMap.imports) {
                        let v = importMap.imports[k];
                        let kUrl = new URL(k, documentBaseURI).toString();
                        var vUrl = new URL(v, documentBaseURI).toString();
                        resolvedMaps[kUrl] = vUrl;
                    }
                    Object.assign(importMap.imports, resolvedMaps);
                    self.indexImportMaps.push(importMap);
                    consoleLog('added importmap', importMap);
                } catch (ex) {
                    consoleLog('error parsing the importmap', ex);
                }
            }
        }
        importMapsFound = self.indexImportMaps && self.indexImportMaps.length;
        if (importMapsFound) {
            // when using importmaps,  importScripts is overriden to fix calls to aliases
            let importScriptsOrig = self.importScripts;
            self.importScripts = (url) => {
                var realUrl = getAppURL(url);
                consoleLog('importScripts', url, realUrl);
                importScriptsOrig(realUrl);
            }
        }
        // detect runtime type and do runtime patching if needed
        var runtimeInfo = await detectBlazorRuntime(scriptNodes);
        consoleLog(`BlazorRuntimeType: '${runtimeInfo.runtime}' Prepatched: ${runtimeInfo.prepatched}`);
        // setup standard document
        var htmlEl = document.appendChild(document.createElement('html'));
        var headEl = htmlEl.appendChild(document.createElement('head'));
        var bodyEl = htmlEl.appendChild(document.createElement('body'));
        // add blazor specific stuff
        // <div id="app">
        var appDiv = bodyEl.appendChild(document.createElement('div'));
        appDiv.setAttribute('id', 'app');
        // <div id="blazor-error-ui">
        var errorDiv = bodyEl.appendChild(document.createElement('div'));
        errorDiv.setAttribute('id', 'blazor-error-ui');
        // load webworker-enabled scripts in order found in the root html file
        for (var scriptNode of scriptNodes) {
            let webWorkerEnabledValue = scriptNode.attributes[WebWorkerEnabledAttributeName];
            let isWebWorkerEnabled = typeof webWorkerEnabledValue !== 'undefined' && webWorkerEnabledValue !== 'false';
            if (!isWebWorkerEnabled) continue;
            let scriptEl = document.createElement('script');
            if (scriptNode.parentNode && scriptNode.parentNode.tagName.toLowerCase() === 'head') {
                headEl.appendChild(scriptEl);
            } else {
                bodyEl.appendChild(scriptEl);
            }
            for (var attr in scriptNode.attributes) {
                let attrValue = scriptNode.attributes[attr];
                scriptEl.setAttribute(attr, attrValue);
            }
            if (scriptNode.text) scriptEl.text = scriptNode.text;
        }
        // init document
        document.initDocument();
    }
    await initializeBlazor();
};
initWebWorkerBlazor();

