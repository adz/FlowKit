---
title: RequestId.get
linkTitle: get
---

Reads the request identifier from a request context.


```fsharp
let get (context: RequestContext) : string
```




## Information

- **Module**: `RequestId`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L298)

## Examples

```fsharp
let requestId = RequestId.get context
```

