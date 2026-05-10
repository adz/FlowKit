---
title: RequestContext.withFlags
linkTitle: withFlags
---

Replaces the request-scoped flags in a request context.


```fsharp
let withFlags (flags: Map<string, bool>) (context: RequestContext) : RequestContext
```




## Information

- **Module**: `RequestContext`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L285)

## Examples

```fsharp
let next = RequestContext.withFlags (Map.ofList [ "canWriteAudit", true ]) context
```

