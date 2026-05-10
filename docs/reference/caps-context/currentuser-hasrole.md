---
title: CurrentUser.hasRole
linkTitle: hasRole
---

Checks whether the current user has a role.


```fsharp
let hasRole (role: string) (user: UserContext) : bool
```




## Information

- **Module**: `CurrentUser`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L146)

## Examples

```fsharp
let user = CurrentUser.create "user-42" None None [ "admin" ] []
let allowed = CurrentUser.hasRole "admin" user
```

