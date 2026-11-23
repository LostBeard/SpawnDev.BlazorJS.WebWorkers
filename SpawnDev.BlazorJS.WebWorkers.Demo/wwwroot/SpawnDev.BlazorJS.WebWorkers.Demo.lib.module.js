// The app's own Blazor JS initializer. Its top-level code calls its own EXPORTED function, as library initializers do (e.g.
// SpawnDev.Blazor.BootRecovery 1.0.0's installBootRecovery()). In a worker the module is loaded through the WebWorkers module patcher,
// which used to turn `export function X` into the function EXPRESSION `_exportsOverride.X = function X`, so this call threw
// "countInitializerRun is not defined" and the worker's .NET boot died (2026-11-22). Test: WebWorkerAppInitializerCallingItsOwnExportRuns.
export function countInitializerRun() {
    globalThis.__webWorkersDemoInitializerRuns = (globalThis.__webWorkersDemoInitializerRuns || 0) + 1;
}

countInitializerRun();

export function beforeWebAssemblyStart() { }
