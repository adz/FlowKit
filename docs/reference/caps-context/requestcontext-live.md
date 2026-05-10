---
title: RequestContext.live
linkTitle: live
---

Creates a live request context from the current runtime state.


```fsharp
let live () : RequestContext
```




## Information

- **Module**: `RequestContext`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L215)

## Examples

```fsharp
let context = RequestContext.live()
```

