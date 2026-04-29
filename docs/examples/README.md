---
title: Runnable Examples
description: Application-shaped examples that are executed during docs generation and mirrored back into the site.
---

# Runnable Examples

This page shows the examples that are executed during the docs build, so the public docs stay tied to real code and observed output.

The examples below are built from the repository projects, run with the current source, and then written back into this page.

The code blocks keep the important API calls on the same lines as the values they bind, with trailing comments where that makes the signature easier to read.
Each example starts with the explicit lift form and then shows the shorter auto-lifted form beside it.

## Request Boundary Example

This example starts with the explicit lift form, then shows the shorter auto-lifted form for a request boundary that pulls a user from a database-like environment, threads a trace id through the request context, and bridges the same model across Flow, AsyncFlow, and TaskFlow.

Run it:

```bash
dotnet run --project examples/FsFlow.Examples/FsFlow.Examples.fsproj --nologo
```

Source:

- [Program.fs](https://github.com/adz/FsFlow/blob/main/examples/FsFlow.Examples/Program.fs)

Source code:

```fsharp
open System
open System.Threading
open System.Threading.Tasks
open FsFlow
open FsFlow.Net
open FsFlow.Validate

type User =
    { Id: int
      Name: string }

type AppDb =
    { FindUser: int -> User option }

type RequestEnv =
    { TraceId: Guid
      Prefix: string
      Db: AppDb
      LoadSuffix: ColdTask<string> }

let validateName (name: string) : Result<string, string> =
    okIfNotBlank name
    |> orElse "name is required"

let loadUser : Flow<RequestEnv, string, User> =
    flow {
        let! db = Flow.read _.Db // Flow<RequestEnv, string, AppDb>
        // Keep the explicit lift here so the example preserves the custom missing-user error.
        let! user = db.FindUser 42 |> Flow.fromOption "user not found" // Flow<RequestEnv, string, User>
        return user
    }

// Keep the explicit lift form here so the same request boundary is visible step by step.
let renderTraceManual : AsyncFlow<RequestEnv, string, string> =
    asyncFlow {
        let! env = AsyncFlow.env // AsyncFlow<RequestEnv, string, RequestEnv>
        let! user = loadUser |> AsyncFlow.fromFlow // AsyncFlow<RequestEnv, string, User>
        let! validName = validateName user.Name |> AsyncFlow.fromResult // AsyncFlow<RequestEnv, string, string>
        return $"{env.Prefix} [{env.TraceId}] {validName}"
    }

let renderTrace : AsyncFlow<RequestEnv, string, string> =
    asyncFlow {
        let! env = AsyncFlow.env // AsyncFlow<RequestEnv, string, RequestEnv>
        let! user = loadUser // AsyncFlow<RequestEnv, string, User>
        let! validName = validateName user.Name // AsyncFlow<RequestEnv, string, string>
        return $"{env.Prefix} [{env.TraceId}] {validName}"
    }

// Keep the explicit lift form here so the async and task boundary conversions stay easy to compare.
let publishResponseManual : TaskFlow<RequestEnv, string, string> =
    taskFlow {
        let! env = TaskFlow.env // TaskFlow<RequestEnv, string, RequestEnv>
        let! user = loadUser |> TaskFlow.fromFlow // TaskFlow<RequestEnv, string, User>
        let! suffix = env.LoadSuffix |> TaskFlow.fromTask // TaskFlow<RequestEnv, string, string>
        return $"{env.Prefix} [{env.TraceId}] {user.Name}{suffix}"
    }

let publishResponse : TaskFlow<RequestEnv, string, string> =
    taskFlow {
        let! env = TaskFlow.env // TaskFlow<RequestEnv, string, RequestEnv>
        let! user = loadUser // TaskFlow<RequestEnv, string, User>
        let! suffix = env.LoadSuffix // TaskFlow<RequestEnv, string, string>
        return $"{env.Prefix} [{env.TraceId}] {user.Name}{suffix}"
    }

[<EntryPoint>]
let main _ =
    let environment =
        { TraceId = Guid.Parse "11111111-1111-1111-1111-111111111111"
          Prefix = "Hello"
          Db =
            { FindUser =
                function
                | 42 -> Some { Id = 42; Name = "Ada" }
                | _ -> None }
          LoadSuffix = ColdTask(fun _ -> Task.FromResult "!") }

    let syncResult =
        loadUser
        |> Flow.run environment

    let asyncResult =
        renderTrace
        |> AsyncFlow.run environment
        |> Async.RunSynchronously

    let taskResult =
        publishResponse
        |> TaskFlow.run environment CancellationToken.None
        |> fun task -> task.GetAwaiter().GetResult()

    printfn "Flow result: %A" syncResult
    printfn "AsyncFlow result: %A" asyncResult
    printfn "TaskFlow result: %A" taskResult
    0

```

Observed output:

```text
Flow result: Ok { Id = 42
     Name = "Ada" }
AsyncFlow result: Ok "Hello [11111111-1111-1111-1111-111111111111] Ada"
TaskFlow result: Ok "Hello [11111111-1111-1111-1111-111111111111] Ada!"
```

## Playground Example

This example shows the same core boundary in both explicit-lift and auto-lift form, so you can compare the two styles side by side.

Run it:

```bash
dotnet run --project examples/FsFlow.Playground/FsFlow.Playground.fsproj --nologo
```

Source:

- [Program.fs](https://github.com/adz/FsFlow/blob/main/examples/FsFlow.Playground/Program.fs)

Source code:

```fsharp
open System
open System.Threading
open System.Threading.Tasks
open FsFlow
open FsFlow.Net

type AppEnv =
    { Prefix: string
      Name: string
      LoadSuffix: ColdTask<string> }

let greetingFlow : Flow<AppEnv, string, string> =
    Flow.read (fun env -> $"{env.Prefix} {env.Name}") // Flow<AppEnv, string, string>

// Keep the explicit lift form here so the example shows the conversion before the shorter CE version.
let greetingAsyncFlowManual : AsyncFlow<AppEnv, string, string> =
    asyncFlow {
        let! greeting = greetingFlow |> AsyncFlow.fromFlow // AsyncFlow<AppEnv, string, string>
        return greeting.ToUpperInvariant()
    }

let greetingAsyncFlow : AsyncFlow<AppEnv, string, string> =
    asyncFlow {
        let! greeting = greetingFlow // AsyncFlow<AppEnv, string, string>
        return greeting.ToUpperInvariant()
    }

// Keep the explicit lift form here so the task interop path stays visible before the auto-boundary version.
let greetingTaskFlowManual : TaskFlow<AppEnv, string, string> =
    taskFlow {
        let! env = TaskFlow.env // TaskFlow<AppEnv, string, AppEnv>
        let! greeting = greetingFlow |> TaskFlow.fromFlow // TaskFlow<AppEnv, string, string>
        let! suffix = env.LoadSuffix |> TaskFlow.fromTask // TaskFlow<AppEnv, string, string>
        return $"{greeting}{suffix}"
    }

let greetingTaskFlow : TaskFlow<AppEnv, string, string> =
    taskFlow {
        let! env = TaskFlow.env // TaskFlow<AppEnv, string, AppEnv>
        let! greeting = greetingFlow // TaskFlow<AppEnv, string, string>
        let! suffix = env.LoadSuffix // TaskFlow<AppEnv, string, string>
        return $"{greeting}{suffix}"
    }

[<EntryPoint>]
let main _ =
    let env =
        { Prefix = "Hello"
          Name = "Ada"
          LoadSuffix = ColdTask(fun _ -> Task.FromResult "!") }

    let syncResult =
        greetingFlow
        |> Flow.run env

    let asyncResult =
        greetingAsyncFlow
        |> AsyncFlow.run env
        |> Async.RunSynchronously

    let taskResult =
        greetingTaskFlow
        |> TaskFlow.run env CancellationToken.None
        |> fun task -> task.GetAwaiter().GetResult()

    printfn "Flow: %A" syncResult
    printfn "AsyncFlow: %A" asyncResult
    printfn "TaskFlow: %A" taskResult
    0

```

Observed output:

```text
Flow: Ok "Hello Ada"
AsyncFlow: Ok "HELLO ADA"
TaskFlow: Ok "Hello Ada!"
```

## Maintenance Example

This example shows smaller, focused shapes for maintenance and interop scenarios, first with explicit lifting and then with the shorter auto-boundary form.

Run it:

```bash
dotnet run --project examples/FsFlow.MaintenanceExamples/FsFlow.MaintenanceExamples.fsproj --nologo
```

Source:

- [Program.fs](https://github.com/adz/FsFlow/blob/main/examples/FsFlow.MaintenanceExamples/Program.fs)

Source code:

```fsharp
open System
open System.Threading
open System.Threading.Tasks
open FsFlow
open FsFlow.Net

let runFlow label env workflow =
    let result = Flow.run env workflow
    printfn "%s: %A" label result

let runAsyncFlow label env workflow =
    let result =
        workflow
        |> AsyncFlow.run env
        |> Async.RunSynchronously

    printfn "%s: %A" label result

let runTaskFlow label env workflow =
    let result =
        workflow
        |> TaskFlow.run env CancellationToken.None
        |> fun task -> task.GetAwaiter().GetResult()

    printfn "%s: %A" label result

let syncExample : Flow<int, string, int> =
    Flow.read id // Flow<int, string, int>
    |> Flow.map ((+) 1)

// Keep the explicit lift form here so the conversion from Flow to AsyncFlow is easy to compare.
let asyncExampleManual : AsyncFlow<int, string, int> =
    asyncFlow {
        let! value = syncExample |> AsyncFlow.fromFlow // AsyncFlow<int, string, int>
        return value * 2
    }

let asyncExample : AsyncFlow<int, string, int> =
    asyncFlow {
        let! value = syncExample // AsyncFlow<int, string, int>
        return value * 2
    }

// Keep the explicit lift form here so the ColdTask-to-TaskFlow conversion stays obvious.
let taskExampleManual : TaskFlow<int, string, int> =
    taskFlow {
        let! env = TaskFlow.env // TaskFlow<int, string, int>
        let! suffix = ColdTask(fun _ -> Task.FromResult 5) |> TaskFlow.fromTask // TaskFlow<int, string, int>
        return env + suffix
    }

let taskExample : TaskFlow<int, string, int> =
    taskFlow {
        let! env = TaskFlow.env // TaskFlow<int, string, int>
        let! suffix = ColdTask(fun _ -> Task.FromResult 5) // TaskFlow<int, string, int>
        return env + suffix
    }

[<EntryPoint>]
let main _ =
    runFlow "Flow" 20 syncExample
    runAsyncFlow "AsyncFlow" 20 asyncExample
    runTaskFlow "TaskFlow" 20 taskExample
    0

```

Observed output:

```text
Flow: Ok 21
AsyncFlow: Ok 42
TaskFlow: Ok 25
```

