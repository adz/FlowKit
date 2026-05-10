---
title: CurrentUser.fromClaimsPrincipal
linkTitle: fromClaimsPrincipal
---

Creates a current-user record from a claims principal when the identity is authenticated.


```fsharp
let fromClaimsPrincipal (principal: ClaimsPrincipal) : UserContext option
```




## Information

- **Module**: `CurrentUser`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L103)

## Examples

```fsharp
let principal = ClaimsPrincipal(ClaimsIdentity([ Claim(ClaimTypes.NameIdentifier, "user-42") ], "demo"))
let user = CurrentUser.fromClaimsPrincipal principal
```

