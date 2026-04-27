# FsFlow Tasks

This backlog is driven by one question:

"Why would an F# developer, comparing this with `Async<Result<_,_>>` plus FsToolkit, decide not to adopt it?"

The current answer is not mainly "the core idea is bad." The current answer is that the project still feels under-explained, under-proven, and unevenly polished.

## Top Priorities

- [ ] Rewrite all user-facing docs so they speak to the library user only, not to the repo author, reviewer, or an implied chat partner.

## Flow Follow-Up Work

Problem:
The public API and repo identity now center on `FsFlow` plus the `Flow` programming model, but the Flow API itself still needs final polishing.

Tasks:

- Review whether `Flow.value` should remain as a convenience alias or be removed in favor of the canonical `Flow.succeed` name.
- Review whether direct `Task` bind support in `flow {}` is sufficient now that task interop has a more explicit `Flow.Task` home.

## 1. Cut The First Public Release

Problem:
The repo now has package metadata, release workflows, and API docs generation, but the first public release still needs the final manual publish steps.

Tasks:

- Push `main` so the new CI, release, and Pages workflows exist on GitHub.
- In GitHub repository settings, ensure Pages is configured to deploy from GitHub Actions.
- Verify the `CI` workflow passes on `main`, including the docs job.
- Verify the Pages deployment succeeds and the published docs site renders correctly.
- Create and push tag `v0.1.0`.
- Check that the release workflow attaches both `.nupkg` and `.snupkg` artifacts to the GitHub Release.
- Push the `0.1.0` package to NuGet manually.
- Confirm the NuGet package page renders the packed `README.md` correctly.

## 2. Product Gaps Worth Addressing After Docs

Problem:
Even with better docs, some users will still compare the ergonomics with FsToolkit and conclude the helper surface is too thin.

Tasks:

- Review the core combinator surface for missing high-value helpers such as `tapError`, `orElse`, `zip`, `map2`, or similar low-ceremony composition tools.
- Revisit the environment story for larger applications if `localEnv` becomes repetitive plumbing.
- Measure basic overhead against equivalent `Async<Result<_,_>>` workflows and publish the result.
- Decide whether the direct `Async<Result<_,_>>` migration story needs more helpers beyond `Flow.fromAsyncResult` and `Flow.toAsyncResult`.

## Done Means

This backlog is done when:

- the docs read like product documentation for the user
- the API reference is useful without opening the source
- semantic edge cases are documented and tested
- the project feels like a maintained library, not a design notebook
