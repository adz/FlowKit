---
title: TaskFlow
description: API reference for TaskFlow
---

# TaskFlow

Represents a cold task-based workflow that reads an environment, observes a runtime cancellation token,
returns a typed result, and is executed explicitly through `TaskFlow.run`.


```fsharp
type TaskFlow<'env, 'error, 'value>
```




## Constructors

- `TaskFlow of ('env -> CancellationToken -> Task<Result<'value, 'error>>)` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L17)

## Information

- **Module**: Global
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L15)

