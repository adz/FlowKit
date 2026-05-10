---
title: Locale.get
linkTitle: get
---

Reads the culture from a request context.


```fsharp
let get (context: RequestContext) : CultureInfo
```




## Information

- **Module**: `Locale`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L358)

## Examples

```fsharp
let culture = Locale.get context
```

