---
title: RequestMetadata.tryGet
linkTitle: tryGet
---

Reads a metadata value by key if it exists.


```fsharp
let tryGet (key: string) (metadata: Map<string, string>) : string option
```




## Information

- **Module**: `RequestMetadata`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L395)

## Examples

```fsharp
let path = RequestMetadata.tryGet "path" context.Metadata
```

