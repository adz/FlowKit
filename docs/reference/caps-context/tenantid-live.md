---
title: TenantId.live
linkTitle: live
---

Returns a live optional tenant identifier from the current runtime state.


```fsharp
let live () : string option
```




## Information

- **Module**: `TenantId`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L346)

## Examples

```fsharp
let tenantId = TenantId.live()
```

