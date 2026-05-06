---
title: fail
description: API reference for TaskFlow.fail
---

# fail

Creates a failing task flow.


```fsharp
let fail (error: 'error) : TaskFlow<'env, 'error, 'value>
```




## Parameters

- `error`: The failure value of type `'error`.

## Returns

A `TaskFlow` that always fails.

## Information

- **Module**: `TaskFlow`
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L150)

