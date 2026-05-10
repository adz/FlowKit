---
title: RequestContext.withCurrentUser
linkTitle: withCurrentUser
---

Replaces the current user in a request context.


```fsharp
let withCurrentUser (currentUser: UserContext option) (context: RequestContext) : RequestContext
```




## Information

- **Module**: `RequestContext`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L258)

## Examples

```fsharp
let next = RequestContext.withCurrentUser (Some user) context
```

