# FsFlow Tasks

This file is the active queue for `scripts/ralph-loop-tasks.sh`.
Keep completed work out of this file.
Keep settled design decisions in `dev-docs/decisions/`.
Keep live architecture direction in `dev-docs/PLAN.md`.

The numbered items below are intentionally linear so the ralph loop can move through them one at a time.

## Completed 0.3.0 Release

1. [x] Finish the docs site shape for the current API.
   - Lead with `Flow`, `AsyncFlow`, and `TaskFlow`.
   - Keep separate API homes for `FsFlow` and `FsFlow.Net`.
2. [x] Raise the docs from generated dump to product manual.
   - Clarify getting started, execution semantics, task/async interop, environment slicing, and architectural styles.
3. [x] Improve public doc comments for the release.
   - Cover the first-use combinators, builders, bridges, and edge cases users will hit first.
4. [x] Verify examples and docs build.
   - Run the example generation path and the docs site build.
5. [x] Trim the README into a release entry point.
   - Keep the value proposition, install snippet, smallest useful example, and docs link.
6. [x] Cut the release.
   - Confirm package metadata, docs versioning, and release notes.

## Post-0.3.0 Architecture

7. [x] Define the explicit `Check` type and public names.
   - Choose the predicate carrier shape and the boolean-algebra vocabulary.
8. [x] Implement `Check` composition and error bridging.
   - Add `not`, `and`, `or`, `all`, `any`, and the bridge into domain errors.
9. [x] Define the validation graph carrier and public names.
   - Choose the graph type name and the diagnostic/path vocabulary.
10. [x] Implement diagnostics merge semantics.
   - Add empty, singleton, merge, and flatten helpers plus recursive branch merging.
11. [x] Add the accumulating `Validation` carrier and `validate {}` CE.
   - Make `and!` accumulate siblings and `let!` remain sequential within a branch.
12. [x] Build the `Check` helper surface.
    - Add predicate constructors, boolean algebra, and bridge helpers.
13. [x] Split and tighten the `Result` helper surface.
    - Keep fail-fast helpers on `Result`, add `mapErrorTo`, and keep `result {}`.
14. [x] Normalize `Result` binding across the flow builders.
    - Bind `Result` and `Result<unit, _>` directly in `Flow`, `AsyncFlow`, and `TaskFlow`.
15. [x] Implement the reader-env `yield` ergonomics.
    - Allow `yield _.Field` in reader-style builders and keep `Flow.read`.
16. [x] Design and prototype the runtime and capability model.
    - Separate runtime services from app dependencies and pressure-test the API shape.

## Docs and Automation

17. [x] Rewrite docs around the post-`0.3.0` model once implemented.
    - Lead with `Check -> Result -> Validation -> Flow -> AsyncFlow -> TaskFlow`.
18. [x] Replace lifted API pages with source-doc extraction.
    - Preserve source links and keep hand-written notes limited to cross-cutting commentary.
19. [ ] Expand executable documentation examples into docs-as-tests.
    - Keep examples runnable during docs generation and capture evaluated output.
20. [ ] Tighten docs release automation.
    - Wire docs generation into release and tag workflows, and add link checking where practical.
21. [ ] Expand benchmark coverage where it helps the architecture.
    - Add peer benchmarks for `FsToolkit.ErrorHandling`, `Ply`, and `IcedTasks`.

## Deferred

22. [ ] Decide whether `Option<'value>` and `ValueOption<'value>` should get implicit binding or only explicit conversion helpers.
23. [ ] Decide whether the core logging abstraction should stay generic or lean on `ILogger` adapters for ergonomics.
