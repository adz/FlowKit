---
title: RequestContext.withTenantId
linkTitle: withTenantId
---

Replaces the tenant identifier in a request context.


```fsharp
let withTenantId (tenantId: string option) (context: RequestContext) : RequestContext
```




## Information

- **Module**: `RequestContext`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L249)

## Examples

```fsharp
let next = RequestContext.withTenantId (Some "tenant-2") context
```

