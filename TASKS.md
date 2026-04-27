# FsFlow Tasks

This backlog is driven by one question:

"Why would an F# developer, comparing this with `Async<Result<_,_>>` plus FsToolkit, decide not to adopt it?"

The current answer is not mainly "the core idea is bad." The current answer is that the project still feels under-explained, under-proven, and unevenly polished.

## Active Backlog

1. [ ] Define the core workflow representations for the redesign: `Flow<'env,'error,'value>` as sync/result-only, `AsyncFlow<'env,'error,'value>` as core `Async`-based, and `TaskFlow<'env,'error,'value>` as `.NET` task-based.
2. [ ] Define the package split so `FsFlow` contains only sync/async core concepts and `FsFlow.Net` contains all task-oriented concepts, task interop, and task-specific runtime helpers.
3. [ ] Design the computation expression surface so `flow {}` is sync-only, `asyncFlow {}` lives in `FsFlow`, and `taskFlow {}` lives in `FsFlow.Net`.
4. [ ] Confirm whether F# computation expression extension members are sufficient for `FsFlow.Net` to add task-oriented binds to `asyncFlow {}` only when `FsFlow.Net` is referenced.
5. [ ] Design the shared internal combinator core so `map`, `bind`, `mapError`, environment projection, and related operations are not reimplemented ad hoc across `Flow`, `AsyncFlow`, and `TaskFlow`.
6. [ ] Remove task-oriented bind support from sync `flow {}` so the sync builder stays honest to the new representation.
7. [ ] Replace the current cold task aliases with a single nominal `ColdTask<'value>` wrapper representing delayed `CancellationToken -> Task<'value>`.
8. [ ] Remove the separate `ColdTaskResult` concept and treat `ColdTask<Result<'value,'error>>` as the typed-failure cold-task shape.
9. [ ] Design the `ColdTask` helper surface for constructing and adapting values from delayed task factories, started `Task<'value>` values where needed, and `ValueTask<'value>` factories or values where needed.
10. [ ] Document the semantic difference between lifting hot `Task`/`ValueTask` values versus lifting `ColdTask<'value>`, including rerun behavior, cancellation-token propagation, and when `ColdTask` is the preferred interop shape.
11. [ ] Support direct binding and return-from for `Async<'value>` and `Async<Result<'value,'error>>` in the async/task-oriented builders.
12. [ ] Support direct binding and return-from for `Task`, `Task<'value>`, and `Task<Result<'value,'error>>` in the `.NET`-oriented builders.
13. [ ] Support direct binding and return-from for `ValueTask`, `ValueTask<'value>`, and `ValueTask<Result<'value,'error>>` in the `.NET`-oriented builders.
14. [ ] Support direct binding and return-from for `ColdTask<'value>` and `ColdTask<Result<'value,'error>>` in the `.NET`-oriented builders.
15. [ ] Support `Option<'value>` and `ValueOption<'value>` as short-circuiting inputs, with implicit binding only when the workflow error type is `unit`.
16. [ ] Provide explicit helpers to adapt `Option<'value>` and `ValueOption<'value>` into workflows with custom error values.
17. [ ] Decide whether `TaskFlow` should internally remain `Task`-backed or whether any part of the execution backbone should be `ValueTask`-based.
18. [ ] Evaluate the correctness and DX risks of a `ValueTask`-based backbone, including single-await constraints, storage and reuse pitfalls, and interaction with workflow composition.
19. [ ] Confirm that no separate `valueTaskFlow` abstraction should exist unless benchmarks and ergonomics clearly justify the added conceptual split.
20. [ ] Benchmark candidate `.NET` representations so the `Task` versus `ValueTask` backbone decision is based on measured outcomes rather than intuition.
21. [ ] Review whether `Flow.value` should remain as a convenience alias or be removed in favor of the canonical `Flow.succeed` name.
22. [ ] Rewrite all user-facing docs so they speak to the library user only, not to the repo author, reviewer, or an implied chat partner.
23. [ ] Update user-facing docs to explain the new workflow family clearly, including when to choose `Flow`, `AsyncFlow`, or `TaskFlow`.
24. [ ] Update user-facing docs to explain the implications of lifting hot `Task` and `ValueTask` inputs versus cold `ColdTask` inputs.
25. [ ] Review the core combinator surface for missing high-value helpers such as `tapError`, `orElse`, `zip`, `map2`, or similar low-ceremony composition tools.
26. [ ] Revisit the environment story for larger applications if `localEnv` becomes repetitive plumbing under the redesigned workflow family.
27. [ ] Measure basic overhead against equivalent `Async<Result<_,_>>` workflows and publish the result.
28. [ ] Decide whether the direct `Async<Result<_,_>>` migration story needs more helpers beyond the current `Flow.fromAsyncResult` and `Flow.toAsyncResult` shape after the redesign lands.
29. [ ] Push `main` so the new CI, release, and Pages workflows exist on GitHub.
30. [ ] In GitHub repository settings, ensure Pages is configured to deploy from GitHub Actions.
31. [ ] Verify the `CI` workflow passes on `main`, including the docs job.
32. [ ] Verify the Pages deployment succeeds and the published docs site renders correctly.
33. [ ] Create and push tag `v0.2.0`.
34. [ ] Check that the release workflow attaches both `.nupkg` and `.snupkg` artifacts to the GitHub Release.
35. [ ] Push the `0.2.0` package to NuGet manually.
36. [ ] Confirm the NuGet package page renders the packed `README.md` correctly.

## Done Means

This backlog is done when:

- the docs read like product documentation for the user
- the API reference is useful without opening the source
- semantic edge cases are documented and tested
- the project feels like a maintained library, not a design notebook
