---
title: CurrentUser.claim
linkTitle: claim
---

Reads a claim value from the current user.


```fsharp
let claim (claimType: string) (user: UserContext) : string option
```




## Information

- **Module**: `CurrentUser`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L156)

## Examples

```fsharp
let user = CurrentUser.create "user-42" None None [] [ "scope", [ "orders.read" ] ]
let scope = CurrentUser.claim "scope" user
```

