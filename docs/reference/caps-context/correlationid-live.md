---
title: CorrelationId.live
linkTitle: live
---

Returns a live optional correlation identifier from the current runtime state.


```fsharp
let live () : string option
```




## Information

- **Module**: `CorrelationId`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L326)

## Examples

```fsharp
let correlationId = CorrelationId.live()
```

