---
title: "AsyncFlow"
linkTitle: AsyncFlow
type: docs
weight: 20
---

Represents a cold async workflow that reads an environment, returns a typed result,
and is executed explicitly through `AsyncFlow.run`.





## Definitions

### `type AsyncFlow<'env, 'error, 'value>`

- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Core.fs#L34)

### `type AsyncFlow<'env, 'error, 'value> with static member CapabilityService (projection: 'env -> 'service) : AsyncFlow<'env, 'error, 'service>`

- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Core.fs#L284)

## Information

- **Module**: Global
- **Source**: [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Core.fs#L34)

