---
title: Validate and Result
description: The validation-first path from Check and Result into Validation, Flow, AsyncFlow, and TaskFlow.
---

# Validate and Result

This page shows how `Check`, `Result`, and `Validation` fit into the main FsFlow story and how plain
`Result` logic carries forward unchanged into `Flow`, `AsyncFlow`, and `TaskFlow`.

## Main Idea

`Check` is the reusable predicate layer.
`Result` is the fail-fast carrier.
`Validation` is the accumulating carrier.

Those are the first steps in the main progression:

```text
Check -> Result -> Validation -> Flow -> AsyncFlow -> TaskFlow
```

`Check` produces plain `Result` values with a unit error.
`Result.mapErrorTo` turns those unit failures into application errors.
`Validation` keeps the structured diagnostics graph visible when multiple sibling failures should be merged.
Because the flow builders bind `Result` directly, the same validation functions lift unchanged into every workflow family.

## Key Shapes

Some validation helpers return a value:

```fsharp
Check.notBlank : string -> Result<string, unit>
Check.notNull : 'a -> Result<'a, unit>
```

Some validation helpers return `unit`:

```fsharp
Check.okIf : bool -> Result<unit, unit>
Check.okIfEmpty : seq<'a> -> Result<unit, unit>
```

Use `Result.mapErrorTo` to attach the application error you actually want:

```fsharp
Result.mapErrorTo : 'error -> Result<'value, unit> -> Result<'value, 'error>
```

The accumulated carrier keeps the graph visible:

```fsharp
type Validation<'value, 'error> = Result<'value, Diagnostics<'error>>
```

The `Validate` module remains as a backward-compatible alias for the older name, but `Check` and
`Validation` are the canonical terms in the current docs.

## Main Pattern

The primary pattern is to keep the validated value.
This allows you to bind the result and use the "clean" value in subsequent steps.

```fsharp
open FsFlow

type RegistrationError =
    | NameRequired
    | EmailRequired

let requireName (name: string) : Result<string, RegistrationError> =
    name
    |> Check.notBlank
    |> Result.mapErrorTo NameRequired

let requireEmail (email: string) : Result<string, RegistrationError> =
    email
    |> Check.notBlank
    |> Result.mapErrorTo EmailRequired
```

When you only need a check and do not need the value, you can still return `Result<unit, 'error>` by ignoring the value:

```fsharp
let validateName (name: string) : Result<unit, RegistrationError> =
    requireName name |> Result.map ignore

let validateEmail (email: string) : Result<unit, RegistrationError> =
    requireEmail email |> Result.map ignore
```

## In Plain Result Code

You do not need to enter a flow early just to keep the code readable.

```fsharp
type RegisterUser =
    { Name: string
      Email: string }

let validateCommand (cmd: RegisterUser) : Result<RegisterUser, RegistrationError> =
    result {
        let! name = requireName cmd.Name
        let! email = requireEmail cmd.Email
        return { cmd with Name = name; Email = email }
    }
```

This stays plain `Result` code because the problem is still fail-fast validation.

If the checks are independent and should all report back, switch to `validate {}`:

```fsharp
let validateCommandAllAtOnce (cmd: RegisterUser) : Validation<RegisterUser, RegistrationError> =
    validate {
        let! name = requireName cmd.Name
        and! email = requireEmail cmd.Email
        return { cmd with Name = name; Email = email }
    }
```

## In Flow Code

The same functions lift unchanged into every workflow family:

```fsharp
let registerUser userId : TaskFlow<AppEnv, RegistrationError, User> =
    taskFlow {
        let! loadUser = TaskFlow.read _.LoadUser
        let! user = loadUser userId

        do! validateName user.Name
        do! validateEmail user.Email

        return user
    }
```

There is no need to wrap those validations in `TaskFlow.fromResult` inside the computation expression.
`taskFlow {}` already binds `Result` directly.

The same style works in `flow {}` and `asyncFlow {}`:

```fsharp
let greet name : Flow<AppEnv, RegistrationError, string> =
    flow {
        let! validName = requireName name
        let! prefix = Flow.read _.Prefix
        return $"{prefix} {validName}"
    }
```

## `do!` Versus `let!`

Use `do!` when the validation returns `Result<unit, 'error>`:

```fsharp
do! validateName user.Name
do! validateEmail user.Email
```

Use `let!` when the validation produces a value:

```fsharp
let! validName = requireName user.Name
```

That is the main rule.
It keeps the validation functions small and avoids unnecessary helper shapes.

## What Validate And Result Are Not

`Check` is for reusable predicates.
`Result` is for fail-fast workflows.
`Validation` is for accumulated sibling failures.

They are not all the same thing with a hidden list on the error side.
When you need structured accumulation, use `Validation` and `validate {}` explicitly.
When you need short-circuiting, keep the code on `Result` or a flow builder.

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the computation-family overview,
[`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for direct binding rules, and
[`docs/INTEGRATIONS_VALIDUS.md`](./INTEGRATIONS_VALIDUS.md) when the validation problem is richer than plain short-circuiting guards.
