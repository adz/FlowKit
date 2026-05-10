---
title: RequestContext.create
linkTitle: create
---

Creates a request context from explicit values.


```fsharp
let create (requestId: string) (correlationId: string option) (tenantId: string option) (currentUser: UserContext option) (culture: CultureInfo) (metadata: seq<string * string>) (flags: seq<string * bool>) : RequestContext
```




## Information

- **Module**: `RequestContext`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L190)

## Examples

```fsharp
let context =
    RequestContext.create
        "req-1"
        (Some "corr-1")
        (Some "tenant-1")
        None
        CultureInfo.InvariantCulture
        [ "path", "/orders/42" ]
        [ "canWriteAudit", true ]
```

