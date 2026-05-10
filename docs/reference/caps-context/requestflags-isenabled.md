---
title: RequestFlags.isEnabled
linkTitle: isEnabled
---

Checks whether the supplied request-scoped flag is enabled.


```fsharp
let isEnabled (key: string) (flags: Map<string, bool>) : bool
```




## Information

- **Module**: `RequestFlags`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L443)

## Examples

```fsharp
let enabled = RequestFlags.isEnabled "canWriteAudit" context.Flags
```

