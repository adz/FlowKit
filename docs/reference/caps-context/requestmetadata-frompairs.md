---
title: RequestMetadata.fromPairs
linkTitle: fromPairs
---

Creates a metadata map from name/value pairs.


```fsharp
let fromPairs (pairs: seq<string * string>) : Map<string, string>
```




## Information

- **Module**: `RequestMetadata`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L386)

## Examples

```fsharp
let metadata = RequestMetadata.fromPairs [ "path", "/orders/42" ]
```

