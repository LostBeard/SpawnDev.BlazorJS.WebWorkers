// A custom service worker or web worker startup script can be used to static import Javascript libraries.
// This was specifically tested to allow Blazor WASM to run in a Chrome Browser extension background ServiceWorker running in 'module' mode so it can run Transformers.js

import * as _globals from "./app.worker.module.globals.js"
import * as _importShim from "./spawndev.blazorjs.webworkers.module.js"

// Start Blazor
Blazor._startTask ??= Blazor.start();

