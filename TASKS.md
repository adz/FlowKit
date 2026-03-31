# EffectfulFlow Tasks

This backlog is driven by one question:

"Why would an F# developer, comparing this with `Async<Result<_,_>>` plus FsToolkit, decide not to adopt it?"

The current answer is not mainly "the core idea is bad." The current answer is that the project still feels under-explained, under-proven, and unevenly polished.

## Top Priorities

- Add XML doc comments to the new public `Flow` surface and its nested `Task` and `Runtime` modules.
- Rewrite all user-facing docs so they speak to the library user only, not to the repo author, reviewer, or an implied chat partner.
- Add XML doc comments to every public data type, union case where appropriate, module, and function, each with a minimal example suitable for generated API docs.
- Prove the library's value against FsToolkit with sharper side-by-side examples and explicit "choose this when / do not choose this when" guidance.
- Lock down semantics for timeout, cancellation, exception capture, and cleanup, then test those semantics directly.
- Improve trust signals: standard test project structure, stronger edge-case coverage, and docs that read like a maintained product rather than a design memo.

## Flow Follow-Up Work

Problem:
The public API and repo identity now center on `EffectfulFlow` plus the `Flow` programming model, but the Flow API itself still needs final polishing.

Tasks:

- Add XML doc comments for the public `Flow<'env, 'error, 'value>` type, `Flow` module, `Flow.Task`, `Flow.Runtime`, the `flow` builder, and all public functions in [`src/EffectfulFlow/Flow.fs`](/home/adam/projects/mylibs/effect.fs/main/src/EffectfulFlow/Flow.fs).
- Review whether `Flow.value` should remain as a convenience alias or be removed in favor of the canonical `Flow.succeed` name.
- Review whether direct `Task` bind support in `flow {}` is sufficient now that task interop has a more explicit `Flow.Task` home.

## 1. Rewrite The Documentation Voice

Problem:
The docs often read like internal reasoning notes or fragments of an LLM conversation. They drift between explaining the product, defending the design, and answering an imagined question. That weakens trust and makes the reader do extra work.

Tasks:

- Rewrite [`README.md`](/home/adam/projects/mylibs/effect.fs/main/README.md) so it opens with user value, then shows the shortest credible example, then gives adoption guidance.
- Rewrite [`docs/GETTING_STARTED.md`](/home/adam/projects/mylibs/effect.fs/main/docs/GETTING_STARTED.md) as a journey-based guide for a new user building their first small workflow.
- Rewrite [`docs/FSTOOLKIT_MIGRATION.md`](/home/adam/projects/mylibs/effect.fs/main/docs/FSTOOLKIT_MIGRATION.md) as a practical migration guide, not a positioning essay.
- Rewrite the type-troubleshooting guide so it teaches boundary normalization through real compiler errors rather than vague “weird shapes” framing.
- Rewrite [`docs/EFFECT_TS_COMPARISON.md`](/home/adam/projects/mylibs/effect.fs/main/docs/EFFECT_TS_COMPARISON.md) so it is brief, factual, and clearly secondary to the F# adoption story.
- Rewrite [`examples/README.md`](/home/adam/projects/mylibs/effect.fs/main/examples/README.md) so it tells the reader what to run, what to look at first, and what each example proves.
- Remove rhetorical question-and-answer framing that sounds like a transcript rather than docs.
- Remove author-side phrases like "the real question is" and "you asked about this shape" unless the page is explicitly an FAQ.
- Make each page answer a single user need: getting started, migration, awkward interop, examples, or positioning.
- Ensure each page starts with "when you should read this page" and ends with "where to go next."

## 2. Establish A Documentation Style Guide

Problem:
The repo needs a stable docs voice, otherwise rewritten pages will drift back into design-note mode.

Tasks:

- Add a short docs style guide to the repo describing audience, tone, structure, and forbidden patterns.
- Set the audience explicitly to the end user of the library, not the maintainer and not an imagined conversation partner.
- Require examples to start from the user's task, not from the library's abstraction.
- Prefer direct instruction and clear tradeoffs over debate-style prose.
- Ban conversational scaffolding that sounds like generated dialogue.
- Ban sections that primarily justify the existence of the section instead of teaching something.
- Prefer "use this when" / "avoid this when" wording over abstract product framing.

## 3. Add API Docstrings Everywhere

Problem:
Generated API docs will currently be thin and unhelpful. A library competing on ergonomics cannot leave the reference surface undocumented.

Tasks:

- Add XML doc comments for the public `Flow<'env, 'error, 'value>` type in [`src/EffectfulFlow/Flow.fs`](/home/adam/projects/mylibs/effect.fs/main/src/EffectfulFlow/Flow.fs).
- Add XML doc comments for public data types in [`src/EffectfulFlow/Flow.fs`](/home/adam/projects/mylibs/effect.fs/main/src/EffectfulFlow/Flow.fs): `LogLevel`, `LogEntry`, and `RetryPolicy<'error>`.
- Add XML doc comments for public modules in [`src/EffectfulFlow/Flow.fs`](/home/adam/projects/mylibs/effect.fs/main/src/EffectfulFlow/Flow.fs): `RetryPolicy`, `Flow`, `Flow.Task`, and `Flow.Runtime`.
- Add XML doc comments for every public function in [`src/EffectfulFlow/Flow.fs`](/home/adam/projects/mylibs/effect.fs/main/src/EffectfulFlow/Flow.fs).
- Add XML doc comments for the public builder type and `flow` entry point in [`src/EffectfulFlow/Flow.fs`](/home/adam/projects/mylibs/effect.fs/main/src/EffectfulFlow/Flow.fs).
- Give every doc comment a small example with minimal boilerplate.
- Keep examples short enough to work inside generated API docs without scrolling through app setup.
- Ensure examples focus on one function at a time rather than embedding full applications.
- Standardize a docstring structure: summary, parameters where useful, return semantics, exceptions or cancellation notes where relevant, and one small example.

## 4. Cover Every Public API Surface Explicitly

Problem:
The API should not rely on readers opening the source to guess semantics.

Tasks:

- Document constructors and lifting functions: `succeed`, `value`, `fail`, `fromResult`, `fromAsync`, `fromAsyncResult`, `fromCold`, `fromHot`, `fromColdResult`, `fromHotResult`, `fromColdUnit`, and `fromHotUnit`.
- Document environment functions: `env`, `read`, and `mapEnv`.
- Document execution functions: `run` and `toAsyncResult`.
- Document composition functions: `map`, `bind`, `tap`, `mapError`, and `delay`.
- Document error and cancellation helpers: `catch`, `catchCancellation`, `ensureNotCanceled`, and `cancellationToken`.
- Document operational helpers: `sleep`, `retry`, `timeout`, `tryFinally`, `useWithAcquireRelease`, `log`, and `logWith`.
- Document `RetryPolicy.noDelay` with explicit retry-attempt semantics.
- For all task-related functions, state clearly whether the API expects a cold factory or an already-created task value.
- For all cancellation-related APIs, state whether cancellation is observed, translated, or propagated.

## 5. Make The FsToolkit Comparison Convincing

Problem:
The current docs explain the idea, but they do not yet prove that adopting this library is worth the switch.

Tasks:

- Add a dedicated comparison page with 3 to 5 realistic side-by-side workflows: plain `Async<Result<_ ,_>>`, FsToolkit, and `Flow`.
- Use examples with dependencies, typed error mapping, and at least one `Task` boundary, since that is where this library claims an advantage.
- Add a concise "choose FsToolkit when" section.
- Add a concise "choose EffectfulFlow when" section.
- Show one migration that starts with `Async<Result<_,_>>` and ends in `Flow<'env, 'error, 'value>` with minimal churn.
- Remove positioning language that sounds defensive or aspirational and replace it with concrete code comparisons.

## 6. Clarify Semantics And Edge Cases

Problem:
The library exposes timeout, retry, cancellation, and resource helpers, but the docs and tests do not yet make their guarantees obvious enough.

Tasks:

- Write a semantics page covering success, typed failure, thrown exception, cancellation, timeout, and cleanup behavior.
- Document whether `timeout` only returns a timeout error or also cancels underlying work.
- Document how `catch` and `catchCancellation` interact with exceptions thrown before versus during async execution.
- Document whether builder `use` / `use!` and `useWithAcquireRelease` run release logic on success, typed failure, thrown exception, and cancellation.
- Document the exact meaning of `RetryPolicy.MaxAttempts`, including whether the first run counts as an attempt.
- Add tests for release-on-error and release-on-cancellation paths.
- Add tests for timeout behavior against work that continues unless explicitly canceled.
- Add tests for exception capture in both synchronous and asynchronous failure cases.

## 7. Improve Example Strategy

Problem:
The examples are credible, but they are still trying to carry too much of the teaching load that should live in docs and API references.

Tasks:

- Keep one main example that demonstrates ordinary application composition.
- Keep one maintenance example for awkward interop and task temperature.
- Add a tiny example set for generated docs and quick-start usage.
- Ensure each example has a clear lesson and a short README note explaining that lesson.
- Add an example specifically focused on incremental migration from existing `Async<Result<_,_>>` code.

## 8. Strengthen Trust Signals

Problem:
A library dealing with effects and execution semantics needs stronger signals that it is stable and deliberate.

Tasks:

- Move from the custom console-style test harness toward a standard unit test setup.
- Add CI coverage for tests and doc generation.
- Add API documentation generation to the normal maintenance flow.
- Add a release checklist covering docs, examples, API docs, and semantic edge-case tests.
- Consider package separation for compatibility helpers if the core package identity starts to blur.

## 9. Product Gaps Worth Addressing After Docs

Problem:
Even with better docs, some users will still compare the ergonomics with FsToolkit and conclude the helper surface is too thin.

Tasks:

- Review the core combinator surface for missing high-value helpers such as `tapError`, `orElse`, `zip`, `map2`, or similar low-ceremony composition tools.
- Revisit the environment story for larger applications if `mapEnv` becomes repetitive plumbing.
- Measure basic overhead against equivalent `Async<Result<_,_>>` workflows and publish the result.
- Decide whether the direct `Async<Result<_,_>>` migration story needs more helpers beyond `Flow.fromAsyncResult` and `Flow.toAsyncResult`.

## Done Means

This backlog is done when:

- the docs read like product documentation for the user
- the API reference is useful without opening the source
- the comparison with FsToolkit is concrete and fair
- semantic edge cases are documented and tested
- the project feels like a maintained library, not a design notebook
