# Changelog

## [2.5.11] - 2024-10-31

### Fixed
- Fixed WebWorkerService startup issue when running on the server. Allows registering the WebWorkerService on the server so that server side rendering and pre-rendering of components that use the WebWorkerService do not throw an error. The service will only be functional when using WASM rendering.

