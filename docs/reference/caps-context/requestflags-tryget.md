---
title: RequestFlags.tryGet
linkTitle: tryGet
---

Reads a flag value by key if it exists.


```fsharp
let tryGet (key: string) (flags: Map<string, bool>) : bool option
```




## Information

- **Module**: `RequestFlags`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L434)

## Examples

```fsharp
let flag = RequestFlags.tryGet "canWriteAudit" context.Flags
```

