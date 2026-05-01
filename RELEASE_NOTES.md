# Release Notes

## 0.3.0 - 2026-05-02

- Major architectural shift to a workflow family: `Flow`, `AsyncFlow`, and `TaskFlow`
- Introduced `FsFlow.Net` package for .NET task-oriented workflows and interop
- Added `ColdTask<'value>` for deferred, restartable task factories
- Migrated documentation to a versioned Docusaurus site with generated runnable examples
- Reorganized the docs into a clearer product-manual path across getting started, execution semantics, runtime interop, environment slicing, and architecture
- Added package-oriented API landing pages for `FsFlow` and `FsFlow.Net`
- Trimmed the README into a shorter NuGet-facing entry point
- Added pure validation helpers and effect bridges for `Async` and `Task`
- Expanded benchmark suite with BenchmarkDotNet and new comparison scenarios

## 0.2.0 - 2026-04-28

- Second public preview release of `FsFlow`
- Completed the package and repository identity move to `FsFlow` across project files, examples, tests, docs, and packaging metadata
- Refreshed the docs site presentation and bundled docs assets for the renamed package
- Cleaned up solution and workflow references after the `v0.1.0` release
- Kept the public `Flow` API stable while polishing the package surface before larger follow-up changes

## 0.1.0 - 2026-04-26

- Initial public preview release of `FsFlow`
- Core `Flow<'env, 'error, 'value>` abstraction for explicit environment requirements, typed failures, and cold execution
- Direct `Result`, `Async`, `Task`, and `ColdTask` interop inside one `flow {}` workflow
- Runtime helpers for cancellation, timeout, retry, logging, and scoped cleanup
- User-facing guides for getting started, environment slicing, semantics, task and async interop, and supported architectural styles
- Runnable example applications plus a NativeAOT probe
- NuGet packaging metadata, symbols, SourceLink, and GitHub Pages API docs pipeline

## Release Process

Publish versions as Git tags such as `v0.2.0`.

The GitHub release workflow builds the package artifacts and attaches them to a GitHub Release.
NuGet publishing stays manual.
