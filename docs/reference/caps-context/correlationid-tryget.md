---
title: CorrelationId.tryGet
linkTitle: tryGet
---

Reads the optional correlation identifier from a request context.


```fsharp
let tryGet (context: RequestContext) : string option
```




## Information

- **Module**: `CorrelationId`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L318)

## Examples

```fsharp
let correlationId = CorrelationId.tryGet context
```

