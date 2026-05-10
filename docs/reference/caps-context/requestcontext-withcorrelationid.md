---
title: RequestContext.withCorrelationId
linkTitle: withCorrelationId
---

Replaces the correlation identifier in a request context.


```fsharp
let withCorrelationId (correlationId: string option) (context: RequestContext) : RequestContext
```




## Information

- **Module**: `RequestContext`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L240)

## Examples

```fsharp
let next = RequestContext.withCorrelationId (Some "corr-2") context
```

