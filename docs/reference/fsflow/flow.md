---
title: Flow
description: API reference for Flow
---

# Flow

Represents a cold synchronous workflow that reads an environment, returns a typed result,
and is executed explicitly through `Flow.run`.


```fsharp
type Flow<'env, 'error, 'value>
```




## Constructors

- `Flow of ('env -> Result<'value, 'error>)` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Core.fs#L15)

## Information

- **Module**: Global
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Core.fs#L13)

