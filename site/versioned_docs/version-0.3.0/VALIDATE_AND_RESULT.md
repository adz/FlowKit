---
title: Validate and Result
description: The validation-first path from plain Result logic into Flow, AsyncFlow, and TaskFlow.
---

# Validate and Result

This page shows how `FsFlow.Validate` fits into the main FsFlow story and how plain `Result` logic carries forward unchanged into `Flow`, `AsyncFlow`, and `TaskFlow`.

## Main Idea

`FsFlow.Validate` is not a side utility.
It is the first step in the main progression:

```text
Validate -> Result -> Flow -> AsyncFlow -> TaskFlow
```

`Validate` produces plain `Result` values.
Those results are intentionally independent of the flow types.
Because the flow builders bind `Result` directly, the same validation functions lift unchanged into every workflow family.

## Key Shapes

Some validation helpers return a value:

```fsharp
Validate.okIfNotBlank : string -> Result<string, unit>
Validate.okIfSome : 'a option -> Result<'a, unit>
```

Some validation helpers return `unit`:

```fsharp
Validate.okIf : bool -> Result<unit, unit>
Validate.okIfEmpty : seq<'a> -> Result<unit, unit>
```

Use `Validate.orElse` or `Validate.orElseWith` to attach the application error you actually want:

```fsharp
Validate.orElse : 'error -> Result<'value, unit> -> Result<'value, 'error>
Validate.orElseWith : (unit -> 'error) -> Result<'value, unit> -> Result<'value, 'error>
```

## Main Pattern

When the validation is just a check, return `Result<unit, 'error>`:

```fsharp
open FsFlow.Validate

type RegistrationError =
    | NameRequired
    | EmailRequired

let validateName (name: string) : Result<unit, RegistrationError> =
    name
    |> okIfNotBlank
    |> Result.map ignore
    |> orElse NameRequired

let validateEmail (email: string) : Result<unit, RegistrationError> =
    email
    |> okIfNotBlank
    |> Result.map ignore
    |> orElse EmailRequired
```

When the validation also produces a value, keep the value:

```fsharp
let requireName (name: string) : Result<string, RegistrationError> =
    name
    |> okIfNotBlank
    |> orElse NameRequired
```

## In Plain Result Code

You do not need to enter a flow early just to keep the code readable.

```fsharp
type RegisterUser =
    { Name: string
      Email: string }

let validateCommand (cmd: RegisterUser) : Result<RegisterUser, RegistrationError> =
    validateName cmd.Name
    |> Result.bind (fun () -> validateEmail cmd.Email)
    |> Result.map (fun () -> cmd)
```

This stays plain `Result` code because the problem is still plain validation.

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

## What Validate Is Not

`Validate` is for short-circuiting checks that produce plain `Result`.

It is not an applicative validation type, and FsFlow does not currently provide accumulated validation in `Validate`, `Flow`, `AsyncFlow`, or `TaskFlow`.

If you need to collect multiple independent validation errors, use a dedicated validation library such as Validus and feed the final `Result` into FsFlow after that step is complete.

## Next

Read [`docs/GETTING_STARTED.md`](./GETTING_STARTED.md) for the computation-family overview,
[`docs/TASK_ASYNC_INTEROP.md`](./TASK_ASYNC_INTEROP.md) for direct binding rules, and
[`docs/INTEGRATIONS_VALIDUS.md`](./INTEGRATIONS_VALIDUS.md) when the validation problem is richer than plain short-circuiting guards.
