---
title: RequestContext.withRequestId
linkTitle: withRequestId
---

Replaces the request identifier in a request context.


```fsharp
let withRequestId (requestId: string) (context: RequestContext) : RequestContext
```




## Information

- **Module**: `RequestContext`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L231)

## Examples

```fsharp
let next = RequestContext.withRequestId "req-2" context
```

