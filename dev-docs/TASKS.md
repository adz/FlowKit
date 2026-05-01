# FsFlow Tasks

This backlog is driven by one question:

How do we make the library feel like a coherent product from pure `Result` to runtime-capable flows, while the docs and API reference stay source-based and polished?

## PREPLAN: 0.3.0 Release

Release `0.3.0` from the current codebase before the larger validation graph and runtime redesign.

1. Finish the docs site shape for the current API.
   - Lead with the existing `Flow`, `AsyncFlow`, and `TaskFlow` split.
   - Keep the docs honest about what exists today versus what is planned next.
   - Preserve separate API homes for `FsFlow` and `FsFlow.Net`.
2. Raise the current docs from generated dump to product manual.
   - Make the getting-started path clear.
   - Make execution semantics, task/async interop, environment slicing, and architectural styles easy to find.
   - Add a forward-looking note that validation graph and runtime/capability work are planned after `0.3.0`.
3. Improve API doc comments enough for the release.
   - Prioritize the public combinators, builders, bridges, and edge cases users will hit first.
   - Keep the source-doc extraction work planned, but do not block `0.3.0` on the full extraction pipeline if the current generated reference is releasable.
4. Verify examples and docs build.
   - Run the docs example generation path.
   - Run the docs site build.
   - Fix broken links, stale examples, or naming mismatches introduced by the docs rewrite.
5. Trim the README into the release entry point.
   - Keep the value proposition, install snippet, smallest useful example, and docs link.
   - Move tutorial-level content into docs pages.
   - Preserve the README as the NuGet-facing landing page.
6. Cut the release.
   - Confirm package metadata and versioning.
   - Confirm docs versioning and release notes.
   - Publish packages and docs for `0.3.0`.

## Post-0.3.0 Architecture Backlog

1. Define the validation graph carrier and public names.
   - Choose the graph type name, likely `Diagnostics<'error>` or `ValidationGraph<'error>`.
   - Define `Diagnostic<'error>`, path segments, local issues, child branches, and flattening behavior.
   - Keep the vocabulary generic: node, branch, scope, path, child, diagnostic.
   - Avoid core names that assume a UI form model such as field or subform.
2. Implement diagnostics merge semantics.
   - Add `empty`, `singleton`, `merge`, `ofList`, `toList`, and path/scoping helpers.
   - Merge local diagnostics by append.
   - Merge children recursively when path segments match.
   - Preserve distinct branches when path segments differ.
3. Add the accumulating `Validation` carrier and `validate {}` CE.
   - Use a value-first shape such as `Validation<'value,'error> = Result<'value, Diagnostics<'error>>`.
   - Make `let!` sequential within a branch.
   - Make `and!` accumulate sibling failures through diagnostics merge.
   - Lift `Result<'value,'error>` into validation by wrapping errors as singleton diagnostics.
   - Add tests for mixed sequential/applicative blocks.
4. Build the `Validate` helper surface.
   - Keep mirrored `Validate.OkIf.*` and `Validate.FailIf.*` predicates where practical.
   - Add `Validate.Error.is` and `Validate.Error.with` for assigning domain errors to failed checks.
   - Add `Validate.collect`, `sequence`, `map2`, `map3`, and `apply` over the validation graph.
   - Add path/scope helpers for attaching diagnostics under a branch.
5. Split and tighten the `Result` helper surface.
   - Keep `Result` focused on fail-fast helpers such as `map`, `bind`, `mapError`, `sequence`, and `traverse`.
   - Add the short-circuiting `result {}` builder for pure domain logic.
   - Document `result {}` as the fail-fast path and `validate {}` as the accumulating path.
6. Normalize `Result` binding across the flow builders.
   - Make `Flow`, `AsyncFlow`, and `TaskFlow` bind `Result` and `Result<unit, _>` directly.
   - Keep direct lifting as the preferred CE pattern.
   - Avoid making users choose bridge modules for the common path.
7. Implement the reader-env `yield` ergonomics.
   - Allow `yield _.Field` as shorthand for environment projection.
   - Mirror the same pattern in `AsyncFlowBuilder` and `TaskFlowBuilder`.
   - Keep `Flow.read` as the canonical explicit API and document the ambiguity around function-valued `yield`.
8. Design and prototype the runtime and capability model.
   - Separate runtime services and policies from app dependencies.
   - Pressure-test `RuntimeContext`, `service<'T>`, record-based service access, `layer`, and block-level policy operations.
   - Decide whether this lives in core, `FsFlow.Net`, or a dedicated runtime layer before implementation hardens.
9. Rewrite docs around the post-`0.3.0` model once implemented.
   - Lead with `Validate -> Result -> Flow -> AsyncFlow -> TaskFlow`.
   - Add dedicated pages for validation graphs, `result {}`, `validate {}`, and the runtime/capability model.
   - Update ecosystem comparison pages with the implemented API rather than speculative names.
10. Replace lifted API pages with source-doc extraction.
   - Move API pages from hand-authored lifted markdown to a real extraction pass.
   - Preserve source links in the rendered docs.
   - Keep hand-written commentary for exceptions and cross-cutting notes only.
11. Expand executable documentation examples into docs-as-tests.
   - Keep example projects runnable during docs generation.
   - Capture evaluated output back into generated docs.
   - Use `Unquote` or equivalent assertions where examples should stay green.
12. Tighten docs release automation.
   - Wire docs generation into release and tag workflows.
   - Validate the current site on pull requests.
   - Add link checking and a docs preview path where practical.
13. Expand benchmark coverage where it helps the architecture.
   - Add peer benchmarks for `FsToolkit.ErrorHandling`, `Ply`, and `IcedTasks`.
   - Extend the suite so the same scenarios can be compared across `Task` and `ValueTask` backbones where meaningful.

## Deferred

1. Decide whether `Option<'value>` and `ValueOption<'value>` should get implicit binding or only explicit conversion helpers.
2. Decide whether the core logging abstraction should stay generic or lean on `ILogger` adapters for ergonomics.

## Completed Work

- Core workflow split: `Flow`, `AsyncFlow`, and `TaskFlow` are separated, with package boundaries and CE surfaces aligned to that split.
- Cold execution model: `ColdTask<'value>` replaced the older aliases, and the docs explain hot versus cold lifting.
- Builder coverage: async, task, value-task, option, and `ColdTask` interop are implemented on the relevant builders.
- ValueTask decision: the task backbone was benchmarked, the risks were evaluated, and `TaskFlow` remains `Task`-backed for now.
- Docs: the user-facing docs now explain the workflow family, semantics, migration path, and benchmark story.
- Docs site: the site now builds with Docusaurus, has package-oriented API hubs, and includes generated runnable examples.
- Docs tooling: `scripts/build-docs-site.sh`, `scripts/preview-docs.sh`, and `scripts/generate-example-docs.sh` drive the site build and example generation.
- Benchmarks: the suite now uses BenchmarkDotNet, includes shared scenario helpers, and publishes results for reader overhead, railway short-circuiting, composition depth, cancellation flow, and synchronous completion.

## Done Means

This backlog is done when:

- the docs read like product documentation for the user
- the API reference is useful without opening the source
- semantic edge cases are documented and tested
- the project feels like a maintained library, not a design notebook
