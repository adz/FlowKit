---
title: Home
description: FsFlow technical guides, semantics, and API reference.
---

<div class="docs-home-hero">

<div class="docs-home-copy">

<span class="eyebrow">Typed results, explicit context, and async/task interop</span>

# Typed results and explicit context for F#.

<p class="lede">
FsFlow provides a lightweight abstraction for the `Env -> Result<'T, 'E>` shape across sync, async, and task-based code. It preserves cold execution and rerun behavior while simplifying mixed-runtime composition without hiding execution details.
</p>

<div class="docs-home-meta">
<span class="docs-chip">Reader-style environment</span>
<span class="docs-chip">Typed failure (Result)</span>
<span class="docs-chip">Async / Task interop</span>
<span class="docs-chip">Cold execution semantics</span>
</div>

</div>

<aside class="docs-home-panel">
<section class="docs-home-panel-card">
<span class="label"><a href="reference/fsflow/validate">Zero-Wrapper Pure Validation</a></span>
<strong>Bind plain `Result<'T, 'E>` directly into flows without extra lifting.</strong>
</section>

<section class="docs-home-panel-card">
<span class="label">Built on Core .NET</span>
<strong>FsFlow keeps `Async` and `Task` underneath, stays explicit at execution boundaries, and stays narrow so it improves application code without trying to become a concurrency platform.</strong>
</section>
</aside>

</div>

## Example: file reads with typed errors

```fsharp
open System
open System.IO
open System.Threading
open FsFlow.Net
open FsFlow.Validate

type ReadmeEnv =
    { Root: string }

type FileReadError =
    | NotFound of path: string

let readTextFile (path: string) : TaskFlow<ReadmeEnv, FileReadError, string> =
    taskFlow {
        // In production, map access and path exceptions separately at the boundary.
        do! okIf (File.Exists path)
            |> orElse (NotFound path)

        return! ColdTask(fun ct -> File.ReadAllTextAsync(path, ct))
    }

let program : TaskFlow<ReadmeEnv, FileReadError, string * string> =
    taskFlow {
        let! root = TaskFlow.read _.Root
        let settingsFile = Path.Combine(root, "settings.json")
        let featureFlagsFile = Path.Combine(root, "feature-flags.json")

        let! settings = readTextFile settingsFile
        let! featureFlags = readTextFile featureFlagsFile

        return settings, featureFlags
    }
```

This snippet shows the core shape. The full runnable example, including `main` and temp-directory setup,
is in [`examples/FsFlow.ReadmeExample/Program.fs`](https://github.com/adz/FsFlow/blob/main/examples/FsFlow.ReadmeExample/Program.fs).

It reads a `Root` value from `'env`, then reads two files in one `taskFlow {}` so the cancellation token is
passed implicitly into both reads. It uses a simple `File.Exists` guard in the example, with a note that
production code should map path and access failures more explicitly at the boundary.

Run side:

```fsharp
use cts = new CancellationTokenSource()

program
|> TaskFlow.run { Root = root } cts.Token
|> Async.AwaitTask
|> Async.RunSynchronously
```

<div class="docs-grid">

<section class="docs-card">
<span class="label">Fundamentals</span>
<h2><a href="GETTING_STARTED">Getting Started</a></h2>
<p>An overview of the `Flow`, `AsyncFlow`, and `TaskFlow` computation families and when to choose each runtime.</p>
</section>

<section class="docs-card">
<span class="label">Patterns</span>
<h2><a href="TINY_EXAMPLES">Common Shapes</a></h2>
<p>Technical reference for `flow {}`, `asyncFlow {}`, `taskFlow {}`, and `ColdTask` composition patterns.</p>
</section>

<section class="docs-card">
<span class="label">Boundaries</span>
<h2><a href="TASK_ASYNC_INTEROP">Runtime Interop</a></h2>
<p>Type-level interop rules for binding `Async`, `Task`, `ValueTask`, and `Result` across different families.</p>
</section>

</div>

<div class="docs-grid">

<section class="docs-card">
<span class="label">Architecture</span>
<h3><a href="WHY_FSFLOW">Design Rationale</a></h3>
<p>The motivation for Reader-style boundaries and the tradeoffs compared to manual dependency threading.</p>
</section>

<section class="docs-card">
<span class="label">Runtime</span>
<h3><a href="SEMANTICS">Execution Semantics</a></h3>
<p>Detailed rules for cold execution, rerun behavior, exception handling, and cancellation propagation.</p>
</section>

<section class="docs-card">
<span class="label">Ecosystem</span>
<h3><a href="INTEGRATIONS">Integrations</a></h3>
<p>Compatibility surface with `FsToolkit.ErrorHandling`, `Validus`, `IcedTasks`, and `FSharpPlus`.</p>
</section>

</div>
