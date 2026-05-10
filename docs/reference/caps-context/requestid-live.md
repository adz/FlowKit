---
title: RequestId.live
linkTitle: live
---

Returns a live request identifier from the current runtime state.


```fsharp
let live () : string
```




## Information

- **Module**: `RequestId`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L306)

## Examples

```fsharp
let requestId = RequestId.live()
```

