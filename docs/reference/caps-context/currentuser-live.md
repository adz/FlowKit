---
title: CurrentUser.live
linkTitle: live
---

Reads the current claims principal from the runtime if one is available.


```fsharp
let live () : UserContext option
```




## Information

- **Module**: `CurrentUser`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Caps.Context/Context.fs#L136)

## Examples

```fsharp
let maybeUser = CurrentUser.live()
```

