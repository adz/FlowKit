---
weight: 10
title: Getting Started
description: The fastest path from Check and Result into Flow.
---


# Getting Started

This page shows the fastest path from plain checks into the right FsFlow family as the execution context grows.

The core `FsFlow` package contains Flow, Check, Result, Validation, and [`validate {}`]({{< relref "builders-validate.md" >}}).
The task APIs now live in the `FsFlow` namespace and ship in the main package.

## 1. Start With The Continuum

FsFlow is meant to scale one Result-based style through richer boundaries:

```text
Check -> Result -> Validation -> Flow
```

Start as small as possible, then lift only when the boundary truly needs more runtime context.

## 2. Start With Checks And Result

Use Check for reusable predicates and Result for fail-fast domain logic:

```fsharp
open FsFlow

type ValidationError =
    | MissingName

let requireName (name: string) : Result<string, ValidationError> =
    name
    |> Check.notBlank
    |> Check.orError MissingName
```

That keeps the pure validation surface small and easy to reuse.

When the same source needs to cross into `flow {}`, use `Guard.Of` or `Guard.MapError`.
Guard keeps the source value visible to the computation expression and carries the failure value alongside it:

```fsharp
let login username password =
    flow {
        let! user = tryGetUser username |> Guard.MapError MissingName
        do! Check.notBlank password |> Guard.Of InvalidPassword
        return user
    }
```

## 3. Add Validation When Siblings Are Independent

Use Validation and [`validate {}`]({{< relref "builders-validate.md" >}}) when you want sibling checks to accumulate instead of stopping at the first one:

```fsharp
type Registration =
    { Name: string
      Email: string }

type RegistrationError =
    | NameRequired
    | EmailRequired

let validateName name =
    name |> Check.notBlank |> Check.orError NameRequired

let validateEmail email =
    email |> Check.notBlank |> Check.orError EmailRequired

let validateRegistration (input: Registration) : Validation<Registration, RegistrationError> =
    validate {
        let! name = validateName input.Name
        and! email = validateEmail input.Email
        return { Name = name; Email = email }
    }
```

Use [`result {}`]({{< relref "builders-result.md" >}}) when the next step depends on the previous one and should stop immediately on failure.

## 4. Choose The Smallest Honest Boundary

Use `Flow<'env, 'error, 'value>` when the computation boundary is honest about the runtime shape:

- synchronous bodies when no async runtime is needed
- async bodies when the computation naturally binds `Async`
- task bodies when the computation naturally binds `.NET Task`

Pick the boundary that matches the code you are writing.

## 5. Use Flow For Synchronous Boundaries

Use Flow when the computation needs dependencies and typed failure, but no async runtime.
The validation code stays exactly the same:

```fsharp
type AppEnv =
    { Prefix: string }

let greet input : Flow<AppEnv, ValidationError, string> =
    flow {
        let! name = requireName input
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {name}"
    }
```

Run a Flow synchronously:

```fsharp
let result =
    greet "Ada"
    |> Flow.run { Prefix = "Hello" }
```

Choose Flow when:

- the computation body is sync
- you want the smallest representation
- carrying a runtime `CancellationToken` would be noise

## 6. Use Flow For `Async`-Based Boundaries

Use Flow when the computation body is built around F# `Async`:

```fsharp
type AsyncEnv =
    { Prefix: string
      LoadName: int -> Async<string> }

let greetAsync userId : Flow<AsyncEnv, ValidationError, string> =
    flow {
        let! loadName = Flow.read _.LoadName
        let! loadedName = loadName userId
        let! validName = requireName loadedName
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

Run a Flow that binds `Async` directly:

```fsharp
let result =
    greetAsync 42
    |> Flow.run
        { Prefix = "Hello"
          LoadName = fun _ -> async { return "Ada" } }
```

Choose Flow when:

- the surrounding code already uses `Async`
- the core package can stay free of `.NET Task` concepts
- `Async` is the natural runtime for the computation

## 7. Use Flow For `.NET Task`-Based Boundaries

Use Flow when the computation body is task-oriented end to end:

```fsharp
open System.Threading.Tasks

type TaskEnv =
    { Prefix: string
      LoadName: int -> Task<string> }

let greetTask userId : Flow<TaskEnv, ValidationError, string> =
    flow {
        let! loadName = Flow.read _.LoadName
        let! loadedName = loadName userId
        let! validName = requireName loadedName
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

Run a Flow that binds `Task` directly:

```fsharp
let result =
    greetTask 42
    |> Flow.run
        { Prefix = "Hello"
          LoadName = fun _ -> Task.FromResult "Ada" }
```

Choose Flow when:

- the boundary is `.NET Task`
- task interop is central to the code path
- runtime cancellation can be part of execution

## 8. Read From The Environment

Each computation family has the same environment pattern:

- `Flow.read`
- `Flow.env`

Use the projected form when you only need one dependency:

```fsharp
let greetWithPrefix input : Flow<AppEnv, ValidationError, string> =
    flow {
        let! name = requireName input
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {name}"
    }
```

Use the whole environment only when you genuinely need it:

```fsharp
let describe : Flow<AsyncEnv, ValidationError, string> =
    flow {
        let! env = Flow.env
        return env.Prefix
    }
```

When task work has separate runtime services from application capabilities, use
`RuntimeContext<'runtime, 'env>` and the `Flow.readRuntime`, `Flow.readEnvironment`, or `Capability`
helpers from the task surface.

When the application capability boundary itself deserves a name, define a cap set with
`Needs<'dep>` and read it with `Env<'dep>` or `Env<'dep, 'value>`. Public task boundaries can
stay flexible with shapes like `Flow<#LoginCaps, _, _>` so a larger runtime still satisfies
the smaller workflow contract.

## 9. Compose Upward, Not Sideways

Flow is one boundary model with direct binds for sync values, `Async`, `Task`, `ValueTask`, and `ColdTask`.
Keep the smallest honest computation at each boundary, then lift it only when the outer runtime really changes.

That means small sync boundaries can stay sync and be reused inside async or task-oriented boundaries:

```fsharp
let validateGreeting input : Flow<AppEnv, ValidationError, string> =
    flow {
        let! name = requireName input
        return name
    }

let greetTaskValidated input : Flow<TaskEnv, ValidationError, string> =
    flow {
        let! validName = validateGreeting input
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

Inside the computation expression, prefer direct binds like this over explicit wrapping.

## 10. Keep The Semantic Boundary Clear

Check, Result, and Flow are short-circuiting.
They are for ordered workflows that stop on the first typed failure.

Validation and [`validate {}`]({{< relref "builders-validate.md" >}}) are for sibling checks that should accumulate into a structured diagnostics graph.
Do not assume that a flow builder is trying to merge independent failures.

## 11. What To Read Next

- **[Validation & Results](../validation-results/)**: Learn the full story from pure checks to structured diagnostics.
- **[Straightforward Examples](./basic-examples/)**: Practical snippets for common tasks.
- **[Task & Async Interop](../core-model/task-async-interop/)**: Deep dive into binding tasks and async blocks.
- **[Managing Dependencies](../core-model/managing-dependencies/)**: Design environment and capability boundaries.
