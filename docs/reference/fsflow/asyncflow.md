---
title: AsyncFlow
description: API reference for AsyncFlow
---

# AsyncFlow

Represents a cold async workflow that reads an environment, returns a typed result,
and is executed explicitly through `AsyncFlow.run`.


```fsharp
type AsyncFlow<'env, 'error, 'value>
```




## Constructors

- `AsyncFlow of ('env -> Async<Result<'value, 'error>>)` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Core.fs#L26)

## Information

- **Module**: Global
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Core.fs#L24)

