# FsFlow Plan

This file tracks the live architectural direction, the remaining design questions, and the product-shape changes that still need to land.

`dev-docs/TASKS.md` is the executable backlog.
`dev-docs/decisions/README.md` indexes the settled decisions and rationale that have been split out of this plan.

## North Star

FsFlow should not be framed as "boundaries only".

The stronger framing is:

> FsFlow is a single model for Result-based programs in F#.
> Start with validation and plain `Result`, then lift the same logic into `Flow`, `AsyncFlow`, or `TaskFlow` when you need environment, async, task, cancellation, logging, or runtime concerns.

The core progression is:

```text
Validate -> Result -> Flow -> AsyncFlow -> TaskFlow
```

The same validation vocabulary should work at every step. The user should not need separate helper worlds for `Result`, `Async<Result>`, and `Task<Result>`.

## Release Sequencing

The next release is `0.3.0`.

`0.3.0` should ship the current codebase with the improved docs and release shape already in progress. The validation graph, `result {}`, `validate {}`, and runtime/capability model are the post-`0.3.0` architectural track unless a small part of them is already safely present.

The point of `0.3.0` is:

- publish the current `Flow` / `AsyncFlow` / `TaskFlow` split cleanly
- make the docs site read like a product manual
- trim the README into a concise entry point
- keep API docs and examples coherent enough for a release
- avoid mixing the release with a larger validation/runtime redesign

## Decided Core Architecture

The current workflow model stays the same:

- `Flow<'env,'error,'value>` for sync/result-oriented work
- `AsyncFlow<'env,'error,'value>` for async workflows in core `FsFlow`
- `TaskFlow<'env,'error,'value>` for .NET task-based workflows in `FsFlow.Net`

The package split stays task-aware only where it needs to be:

- `FsFlow` owns the task-free core surface
- `FsFlow.Net` owns task interop, `ColdTask`, and .NET-specific runtime helpers

Workflow semantics stay cold and restartable:

- reruns start from scratch for `Flow`, `AsyncFlow`, and `TaskFlow`
- hot `Task` and `ValueTask` inputs are interop shapes, not the semantic identity of the workflow
- `ColdTask<'value>` remains the deferred shape when rerun fidelity and token flow matter

The settled decisions are already recorded in:

- [Flow architecture](decisions/flow-architecture.md)
- [TaskFlow and ValueTask](decisions/taskflow-valuetask.md)
- [Benchmark history](decisions/benchmark-history.md)
- [Docs source extraction](decisions/docs-source-extraction.md)
- [Reader-env `yield`](decisions/reader-env-yield.md)

## Result, Validation, and Validate

Validation is central, not a side utility, but it needs two different execution semantics:

- `result {}` is monadic and short-circuiting
- `validate {}` is applicative and accumulating

The split should be visible in the API. `Result<'value,'error>` is the fail-fast carrier. Accumulated validation should not pretend to be the same thing with a hidden list on the error side.

The intended public shape is:

```fsharp
result {
    let! name = parseName rawName
    let! email = parseEmail rawEmail
    return { Name = name; Email = email }
}
```

```fsharp
validate {
    let! name = validateName rawName
    and! email = validateEmail rawEmail
    and! age = validateAge rawAge
    return { Name = name; Email = email; Age = age }
}
```

Sequential `let!` means dependency and short-circuiting. Applicative `and!` means sibling checks can be evaluated independently and their failures can be merged.

The module split should follow that semantic split:

- `Result` owns fail-fast helpers such as `map`, `bind`, `mapError`, `sequence`, and `traverse`
- `result {}` owns fail-fast CE syntax for pure domain code
- `Validate.OkIf.*` and `Validate.FailIf.*` own mirrored predicate checks
- `Validate.Error.*` owns conversion from placeholder failures into domain errors
- `validate {}` owns accumulated validation syntax
- validation graph helpers live under `Validate` or a clearly related submodule

`Validate.OkIf.*` and `Validate.FailIf.*` should remain mirrored where practical. The user should be able to choose the predicate direction that reads naturally:

```fsharp
rawName
|> Validate.OkIf.notBlank
|> Validate.Error.is NameRequired
```

```fsharp
value
|> Validate.FailIf.isNull
|> Validate.Error.with (fun () -> MissingValue)
```

`Validate.Error.is` and `Validate.Error.with` are preferred over `OrElse`-style naming because they describe the operation being performed: assign or compute the domain error for a failed check.

## Validation Graph

Accumulation should be richer than `Result<'value, 'error list>`.

A flat list is easy to understand, but it loses the structure that makes validation results useful in real applications. The better model is a mergeable diagnostics graph or forest:

- each node can hold local diagnostics
- each node can have child nodes
- sibling nodes merge when independent checks are combined
- nested validation keeps its own subtree
- a flat list is just the degenerate graph with no child branches

This graph can represent simple independent checks, nested objects, repeated branches, reusable validation components, and path-aware failures without baking UI terms such as field, form, or subform into the core API.

The core vocabulary should stay generic:

- `Diagnostic` or `Issue` for one failure item
- `Diagnostics<'error>` or `ValidationGraph<'error>` for the mergeable failure graph
- `Path` and `PathSegment` for location
- `node`, `branch`, `scope`, `child`, `at`, `under`, `merge`, and `collect` for structural operations

The likely implementation shape is:

```fsharp
type PathSegment =
    | Key of string
    | Index of int
    | Name of string

type Diagnostic<'error> =
    {
        Path : PathSegment list
        Error : 'error
    }

type Diagnostics<'error> =
    {
        Local : Diagnostic<'error> list
        Children : Map<PathSegment, Diagnostics<'error>>
    }
```

Names are still open, but the responsibilities are not:

- `Diagnostics.empty` creates an empty graph
- `Diagnostics.singleton` creates a graph from one diagnostic
- `Diagnostics.merge` recursively combines two graphs
- `Diagnostics.at` or `Diagnostics.under` nests a graph under a path segment
- `Diagnostics.toList` flattens the graph for simple display
- `Diagnostics.ofList` builds the degenerate flat graph

The validation carrier can then be explicit:

```fsharp
type Validation<'value, 'error> = Result<'value, Diagnostics<'error>>
```

This keeps the value-first convention of `Result<'value,'error>` while making the accumulated error shape visible. If the graph type ends up named differently, the same rule should hold: `Validation` fails with the structured diagnostics container, not with a hidden raw `'error list`.

The CE rules should be documented and tested:

- `return x` produces `Ok x`
- `return! validation` returns an existing validation
- `let! x = validation` is sequential and stops that branch on failure
- `let! x = result` can lift `Result<'value,'error>` into `Validation<'value,'error>` by wrapping the error in `Diagnostics.singleton`
- `and!` combines independent validation results with `Diagnostics.merge`
- a mixed block with `let!` followed by an `and!` group sequences first, then accumulates inside the group

For example:

```fsharp
validate {
    let! parsed = parseEnvelope input

    let! name = validateName parsed.Name
    and! email = validateEmail parsed.Email
    and! address = validateAddress parsed.Address |> Validate.at (Key "address")

    return build parsed name email address
}
```

If `parseEnvelope` fails, the sibling checks are not run. If parsing succeeds, the `and!` group accumulates every sibling failure into one diagnostics graph.

Batch helpers are the function form of the same behavior:

- `Validate.collect` runs many independent `Validation<'value,'error>` values and returns `Validation<'value list,'error>`
- `Validate.sequence` flips a sequence of validations into one validation of a sequence
- `Validate.map2` / `map3` / `apply` combine independent validations and merge diagnostics
- scoped helpers attach path or branch context before merging

This design uses category-theory language internally: `Result` is the monadic fail-fast path, and `Validation` is the applicative accumulating path. The public documentation should explain this in plain terms first and reserve the category names for advanced notes.

## Runtime and Capability Model

A likely next architectural step is a two-context model:

- `RuntimeContext` holds system/runtime concerns
- `Env` holds app/business dependencies

The runtime side is where operational concerns live:

- logging
- metrics
- tracing
- cancellation
- clocks
- annotations
- optional `IServiceProvider` access

The app side is where user dependencies live:

- database clients
- HTTP clients
- configuration
- domain services

The design direction is:

- keep runtime policy and observability separate from app dependencies
- allow block-level custom operations for policies like `withRetry`, `withTimeout`, `withSpan`, `annotate`, and `measure`
- keep action-style custom operations for effects like `log`, `sleep`, `ensureNotCanceled`, `cancellationToken`, and `utcNow`
- support both record-based capability access and `IServiceProvider`-style interop

The conceptual task-flow shape becomes:

```fsharp
RuntimeContext -> 'env -> Task<Result<'value,'error>>
```

or an equivalent representation that preserves the same separation:

```text
RuntimeContext = how the system runs
Env            = what the application needs
```

Runtime policies should apply to blocks, not just to individual function calls:

```fsharp
taskFlow {
    withRetry networkRetry
    withTimeout timeout TimedOut
    withSpan "device.read"
    annotate "deviceId" deviceId
    measure "device.poll"

    do! log Info "Reading device"

    let! client = service<IDeviceClient>
    return! client.Read deviceId
}
```

Nested computation expressions define policy scope. An inner `taskFlow { withRetry ... }` block should apply the retry only to that inner block.

This likely requires a plan/spec stage in the CE rather than having custom operations immediately produce a final flow. The design to pressure-test is:

```fsharp
type TaskFlowSpec<'env,'error,'value> =
    {
        Policies : RuntimePolicy list
        Build : unit -> TaskFlow<'env,'error,'value>
    }
```

The builder `Run` step can then build the flow and apply the collected policies in a well-defined order.

Capability access needs two modes:

- record-based F# environments: `service _.DeviceClient`
- .NET host interop: `service<IDeviceClient>`

Those are related, but they should not force every user through `IServiceProvider`. The planned shape is to support pure records as the preferred F# model and DI-backed lookup as the ASP.NET/AppHost integration model.

Layering is a separate concept from program execution:

- `taskFlow {}` describes a program that consumes capabilities
- `runtime {}` assembles concrete runtime services
- `layer {}` describes a reusable recipe for deriving capabilities from other capabilities or config

The likely layer shape is:

```fsharp
type Layer<'inEnv,'error,'outEnv> = TaskFlow<'inEnv,'error,'outEnv>
```

and the expected composition is:

```fsharp
program
|> TaskFlow.provideLayer appLayer
```

The broader runtime/capability idea is still a design frontier, not a settled contract. It likely belongs in `FsFlow.Net` or a dedicated runtime layer, but the implementation must preserve the separation between runtime concerns, app dependencies, block policies, and service provisioning.

## Docs and Product Shape

The public-facing story should feel like a polished product site, not a generated dump.

For `0.3.0`, the docs should describe the current codebase and the current three-family model. The future validation graph and runtime/capability model can be mentioned as direction, but not presented as available APIs.

The long-term direction is:

- explicit docs navigation
- versioned docs
- separate API home pages for `FsFlow` and `FsFlow.Net`
- source-aware API pages generated from code comments
- executable examples that stay green
- a short README that points into the docs

The long-term site structure should lead with the model rather than the implementation details:

```text
Start
- Home
- Getting Started
- Common Shapes

Core Model
- The FsFlow Model
- Validate and Result
- Validation Graphs
- Flow / AsyncFlow / TaskFlow
- Execution Semantics
- Task and Async Interop
- Environment Slicing
- Architectural Styles

Patterns
- Common Patterns
- Runtime Patterns
- Troubleshooting Types
- Benchmarks

Ecosystem
- FsToolkit Comparison
- Validus Integration
- IcedTasks Integration
- FSharpPlus Integration
- Effect-TS Comparison

Reference
- API Reference
```

The docs should explain:

- the `Validate -> Result -> Flow` continuum
- how the validation graph accumulates sibling failures while preserving nested structure
- why `do!` and `let!` mean different things
- where cold/restartable semantics matter
- how task and async interop work without making them the center of the story
- how the runtime/capability model fits if and when it lands

## API Ergonomics

The API surface should keep improving in the direction of fewer conceptual seams.

The main ergonomic items are:

- a `Result` CE for pure validation/domain code
- `Result` helpers that support the main FsFlow path without bloating the surface
- direct `Result` binding into all flow builders
- reader-env `yield` as shorthand for environment projection, while keeping `Flow.read` as the explicit API
- a validation CE that exposes applicative accumulation over the diagnostics graph

The `yield` work stays intentionally conservative:

- `yield _.Field` is allowed as reader-style projection
- `YieldFrom` remains the normal flow passthrough
- function-valued `yield` remains an explicit documentation concern because of the ambiguity

## Open Questions

These still need explicit decisions:

- `Option<'value>` and `ValueOption<'value>` short-circuit behavior and implicit binding rules
- the core logging abstraction versus `ILogger` adapters and ergonomics
- the final scope of the runtime/capability model and whether it becomes a core contract or a `FsFlow.Net` concern
- the final public name of the diagnostics graph type and its child/merge helpers

## Done Means

This backlog is done when:

- the docs read like product documentation for the user
- the API reference is useful without opening the source
- semantic edge cases are documented and tested
- the project feels like a maintained library, not a design notebook
