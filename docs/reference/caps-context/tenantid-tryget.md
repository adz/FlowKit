---
title: TenantId.tryGet
linkTitle: tryGet
---

Reads the optional tenant identifier from a request context.


```fsharp
let tryGet (context: RequestContext) : string option
```




## Information

- **Module**: `TenantId`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L338)

## Examples

```fsharp
let tenantId = TenantId.tryGet context
```

