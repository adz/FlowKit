---
title: Overview
description: Landing page for the main FsFlow API surface.
---

# FsFlow

This page shows the main FsFlow surface by what you do with it, not by namespace.

Use this page when you want the synchronous, async, and task family shape in one place.

## What belongs here

This package groups the synchronous, async, and task workflow families plus the pure validation bridge:

- `Flow<'env, 'error, 'value>` and the `Flow` module
- `AsyncFlow<'env, 'error, 'value>` and the `AsyncFlow` module
- `TaskFlow<'env, 'error, 'value>` and the `TaskFlow` module
- `ColdTask<'value>` and the `ColdTask` module
- `Check` for pure `Result<'value, unit>` predicates
- `Result` for fail-fast helpers
- `Validation` for accumulated diagnostics
- support types that shape runtime logging and retry behavior
- the `flow {}`, `asyncFlow {}`, and `taskFlow {}` entry points

The builder types themselves stay below the surface. The families and their modules are the public story.

## Flow

`Flow` is the synchronous family:

- build from values, options, and results
- read the environment explicitly
- compose with `map`, `bind`, `tap`, and `orElse`
- collect lists and sequences
- bridge into `AsyncFlow` or validation when needed

<div class="docs-grid">

<section class="docs-card">
<span class="label">Flow</span>
<h2>Sync boundaries</h2>
<p>The `Flow&lt;'env, 'error, 'value&gt;` type and `Flow` module cover synchronous composition, explicit environment reads, short-circuiting, and execution.</p>
<p>The next page expands the full member map, including creation helpers, composition helpers, environment access, traversals, runtime helpers, and interop points.</p>
<div class="docs-card-links">
<a href="flow">Open the Flow page</a>
</div>
</section>

<section class="docs-card">
<span class="label">AsyncFlow</span>
<h2>Async boundaries</h2>
<p>The `AsyncFlow&lt;'env, 'error, 'value&gt;` type and `AsyncFlow` module cover async composition, `Async` interop, explicit environment reads, and execution.</p>
<p>The next page expands the full member map, including creation helpers, composition helpers, traversals, runtime helpers, and async bridges.</p>
<div class="docs-card-links">
<a href="asyncflow">Open the AsyncFlow page</a>
</div>
</section>

<section class="docs-card">
<span class="label">TaskFlow</span>
<h2>Task boundaries</h2>
<p>The `TaskFlow&lt;'env, 'error, 'value&gt;` type and `TaskFlow` module cover task-native composition, explicit environment reads, cancellation, and execution.</p>
<p>The next pages expand the full member map for the task surface, including cold task helpers, runtime helpers, and async bridges.</p>
<div class="docs-card-links">
<a href="taskflow">Open the TaskFlow page</a>
</div>
</section>

<section class="docs-card">
<span class="label">Check</span>
<h2>Pure validation</h2>
<p>`FsFlow.Check` gives pure `Result&lt;_, unit&gt;` checks for booleans, options, value options, nulls, collections, equality, and strings.</p>
<p>`FsFlow.Result` and `FsFlow.Validation` cover the fail-fast and accumulating sides, and the next page expands the full helper family and the bridges that turn unit failures into application errors.</p>
<div class="docs-card-links">
<a href="validate">Open the validation page</a>
</div>
</section>

</div>

<div class="docs-stack">

<section class="docs-card">
<span class="label">Support types</span>
<h3>Supporting types</h3>
<p>These types are useful but narrower in scope: `LogEntry`, `LogLevel`, and `RetryPolicy`.</p>
<div class="docs-card-links">
<a href="support-types">Open the support types page</a>
</div>
</section>

<section class="docs-card">
<span class="label">Entry points</span>
<h3>Workflow builders</h3>
<p>The `flow {}`, `asyncFlow {}`, and `taskFlow {}` entry points are available, but the builder types themselves are treated as plumbing rather than headline API.</p>
<p>Use the builders for readable orchestration, and use the modules for the actual API surface.</p>
</section>

</div>

## AsyncFlow

`AsyncFlow` follows the same shape as `Flow`, but its execution model is async-first:

- lift from synchronous flows when the boundary stays the same
- lift from `Async` and `Async<Result<_, _>>` when the runtime already is async
- keep the same explicit environment model
- bridge to `TaskFlow` when the application boundary moves to tasks

## TaskFlow

`TaskFlow` follows the same shape as `Flow` and `AsyncFlow`, but its runtime is task-first:

- lift from `Flow` and `AsyncFlow` when the outer boundary already carries them
- lift from `Task`, `Task<Result<_, _>>`, `ValueTask`, `ValueTask<Result<_, _>>`, and `ColdTask`
- keep the same explicit environment model
- preserve cancellation through the boundary

## Read the subpages

<div class="docs-grid">

<section class="docs-card">
<span class="label">Flow</span>
<h3><a href="flow">Sync boundaries</a></h3>
<p>Type, module, composition, environment, and execution in the synchronous family.</p>
</section>

<section class="docs-card">
<span class="label">AsyncFlow</span>
<h3><a href="asyncflow">Async boundaries</a></h3>
<p>The async family surface, including async bridges and the same explicit environment model.</p>
</section>

<section class="docs-card">
<span class="label">TaskFlow</span>
<h3><a href="taskflow">Task boundaries</a></h3>
<p>The task family surface, including cold task helpers, task-native runtime helpers, and async bridges.</p>
</section>

<section class="docs-card">
<span class="label">Check</span>
<h3><a href="validate">Validation helpers</a></h3>
<p>Pure predicates, fail-fast result helpers, accumulating validation, and the bridge to effectful error creation.</p>
</section>

<section class="docs-card">
<span class="label">Support types</span>
<h3><a href="support-types">Supporting types</a></h3>
<p>Logging and retry helpers that support the core boundary model without taking over the page.</p>
</section>

</div>
