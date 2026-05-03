# FsFlow Plan

This file tracks live product-shape direction and any remaining open questions.
`dev-docs/TASKS.md` is the executable backlog.
`dev-docs/decisions/README.md` indexes the settled decisions that no longer belong here.

## North Star

FsFlow should be framed as one model for Result-based programs in F#.

Start with validation and plain `Result`, then lift the same logic into `Flow`, `AsyncFlow`, or `TaskFlow` when you need environment, async, task, cancellation, logging, or runtime concerns.

The core progression is:

```text
Check -> Result -> Validation -> Flow -> AsyncFlow -> TaskFlow
```

The same predicate and validation vocabulary should work at every step. The user should not need separate helper worlds for raw checks, fail-fast `Result`, accumulated `Validation`, `Async<Result>`, and `Task<Result>`.

## Settled Decisions

These items are no longer live design questions and are tracked in the decision log:

- [Flow architecture](decisions/flow-architecture.md): workflow family split, namespace continuity, cold/restartable semantics, `ColdTask`, and builder surface
- [TaskFlow and ValueTask](decisions/taskflow-valuetask.md): why `TaskFlow` stays Task-backed and why `ValueTask` stays a boundary shape
- [Validation surface](decisions/validation-surface.md): `Check`, `Diagnostics`, `Validation`, and the applicative `validate {}` split
- [API reference page shape](decisions/reference-page-shape.md): one page per public API surface, side-menu entries, examples, and source links
- [Docs source extraction](decisions/docs-source-extraction.md): source-aware API pages with links back to the implementation
- [Reader-env `yield`](decisions/reader-env-yield.md): `yield _.Field` as shorthand while keeping `Flow.read`
- [Option and ValueOption binding](decisions/option-valueoption-binding.md): keep implicit binding only for `unit` error workflows and use explicit conversion helpers for typed errors
- [Logging ergonomics](decisions/logging-ergonomics.md): keep the core logging abstraction generic and treat `ILogger` as an adapter

## Live Docs Direction

The remaining product-shape work is the documentation surface, not the core architecture:

- every public API gets a dedicated reference page
- the reference pages are lifted from the XML doc comments so the rendered docs match the IDE experience
- each page uses the same FsToolkit-style structure: summary, explanation, example, member map, and source link
- package hubs stay concise and point readers into the member pages
- validation docs lead with `Check`, `Diagnostics`, `Validation`, and applicative `validate {}`
- released docs point at the versioned source snapshot
- next/unreleased docs point at `main`

## Open Questions

- whether any legacy `Validate` mentions should remain outside compatibility notes in the narrative docs
- whether the docs site navigation wants any extra grouping once every public API has its own page

## Done Means

- the docs read like product documentation for the user
- the API reference is useful without opening the source
- every public API is reachable from the side menu
- semantic edge cases are documented and tested
- the project feels like a maintained library, not a design notebook
