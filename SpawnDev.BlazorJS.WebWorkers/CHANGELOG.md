# Changelog


## [2.5.22] - 2024-11-25

### Changed
- Updated SpawnDev.BlazorJS dependency to 2.5.22


## [2.5.17] - 2024-11-15

### Changed
- Updated SpawnDev.BlazorJS dependency to 2.5.17

### Fixed
- Fixed JsonConverterAttribute with HybridConverter and HybridConverterFactory not working (Ex: SharedCancellationTokenSource)


## [2.5.16] - 2024-11-15

### Changed
- Updated SpawnDev.BlazorJS dependency to 2.5.16


## [2.5.15] - 2024-11-15

### Changed
- Updated SpawnDev.BlazorJS dependency to 2.5.15


## [2.5.14] - 2024-11-14

### Changed
- Updated SpawnDev.BlazorJS dependency to 2.5.14


## [2.5.13] - 2024-11-12

### Changed
- Updated package icon
- Updated net9.0 Microsoft.AspNetCore.Components.WebAssembly dependency to 9.0.0
- Updated SpawnDev.BlazorJS dependency to 2.5.13


## [2.5.12] - 2024-11-10

### Changed
- Updated SpawnDev.BlazorJS dependency to 2.5.12


## [2.5.11] - 2024-10-31

### Fixed
- Fixed WebWorkerService startup issue when running on the server. Allows registering the WebWorkerService on the server so that server side rendering and pre-rendering of components that use the WebWorkerService do not throw an error. The service will only be functional when using WASM rendering.

