---
title: RequestContext.withCulture
linkTitle: withCulture
---

Replaces the culture in a request context.


```fsharp
let withCulture (culture: CultureInfo) (context: RequestContext) : RequestContext
```




## Information

- **Module**: `RequestContext`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L267)

## Examples

```fsharp
let next = RequestContext.withCulture CultureInfo.InvariantCulture context
```

