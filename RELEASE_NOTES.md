# Release Notes

## 0.1.0 - 2026-04-26

- Initial public preview release of `FsFlow`
- Core `Flow<'env, 'error, 'value>` abstraction for explicit environment requirements, typed failures, and cold execution
- Direct `Result`, `Async`, `Task`, `ColdTask`, and `ColdTaskResult` interop inside one `flow {}` workflow
- Runtime helpers for cancellation, timeout, retry, logging, and scoped cleanup
- User-facing guides for getting started, environment slicing, semantics, task and async interop, and supported architectural styles
- Runnable example applications plus a NativeAOT probe
- NuGet packaging metadata, symbols, SourceLink, and GitHub Pages API docs pipeline

## Release Process

Publish versions as Git tags such as `v0.1.0`.

The GitHub release workflow builds the package artifacts and attaches them to a GitHub Release.
NuGet publishing stays manual.
