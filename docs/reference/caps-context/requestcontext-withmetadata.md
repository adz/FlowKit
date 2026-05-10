---
title: RequestContext.withMetadata
linkTitle: withMetadata
---

Replaces the request metadata in a request context.


```fsharp
let withMetadata (metadata: Map<string, string>) (context: RequestContext) : RequestContext
```




## Information

- **Module**: `RequestContext`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L276)

## Examples

```fsharp
let next = RequestContext.withMetadata (Map.ofList [ "path", "/orders/42" ]) context
```

