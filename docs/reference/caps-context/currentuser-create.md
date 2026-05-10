---
title: CurrentUser.create
linkTitle: create
---

Creates a current-user record from explicit values.


```fsharp
let create (userId: string) (displayName: string option) (email: string option) (roles: seq<string>) (claims: seq<string * string list>) : UserContext
```




## Information

- **Module**: `CurrentUser`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L81)

## Examples

```fsharp
let user =
    CurrentUser.create
        "user-42"
        (Some "Ada")
        (Some "ada@example.com")
        [ "admin" ]
        [ "scope", [ "orders.read" ] ]
```

