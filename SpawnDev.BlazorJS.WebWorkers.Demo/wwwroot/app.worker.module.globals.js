// anything that needs to be set or run before other stuff must be in the first statically imported script because
// static imports in the main file are loaded before any code, no matter the placement of the code or imports.

// this tells spawndev.blazorjs.webworkers.module.js not to start Blazor. We will start it with Blazor.start() and optionally with any startup changes we need.
globalThis.autoStart = false;
