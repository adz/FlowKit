---
title: RequestMetadata.contains
linkTitle: contains
---

Checks whether the metadata contains a key.


```fsharp
let contains (key: string) (metadata: Map<string, string>) : bool
```




## Information

- **Module**: `RequestMetadata`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L404)

## Examples

```fsharp
let hasPath = RequestMetadata.contains "path" context.Metadata
```

