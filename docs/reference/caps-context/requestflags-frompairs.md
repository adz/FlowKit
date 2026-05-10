---
title: RequestFlags.fromPairs
linkTitle: fromPairs
---

Creates a flag map from name/value pairs.


```fsharp
let fromPairs (pairs: seq<string * bool>) : Map<string, bool>
```




## Information

- **Module**: `RequestFlags`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L425)

## Examples

```fsharp
let flags = RequestFlags.fromPairs [ "canWriteAudit", true ]
```

