# Decision Logs

This folder captures settled design decisions and supporting rationale that no longer belong in `dev-docs/PLAN.md`.

## Index

- [Flow architecture](flow-architecture.md): 2026-04-27 to 2026-04-28. Workflow family split, package boundary, cold/restartable semantics, `ColdTask`, and builder surface.
- [TaskFlow and ValueTask](taskflow-valuetask.md): 2026-04-28. Why `TaskFlow` stays Task-backed and why `ValueTask` stays a boundary shape.
- [Benchmark history](benchmark-history.md): 2026-04-28. Recorded benchmark runs and the conclusions they supported.
- [Docs source extraction](docs-source-extraction.md): 2026-04-29. Move API pages from hand-authored lifted markdown to a real source-doc extraction pass with source links.
- [Reader-env `yield`](reader-env-yield.md): 2026-04-29. Allow `yield` inside readers to project from the environment while keeping `Flow.read`.
- [Option and ValueOption binding](option-valueoption-binding.md): 2026-05-03. Keep implicit binding only for `unit` error workflows and use explicit conversion helpers for typed errors.
- [Logging ergonomics](logging-ergonomics.md): 2026-05-03. Keep the core logging abstraction generic and treat `ILogger` as an integration adapter.
