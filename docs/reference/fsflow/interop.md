---
title: Interop
description: Source-documented task and async interop helpers for FsFlow.
---

# Interop

This page shows the interop helpers that bridge task, async, and synchronous boundaries in FsFlow.

## TaskFlow bridges

- [`TaskFlow.fromFlow`](./taskflow-fromflow.md) [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L253)
- [`TaskFlow.fromAsyncFlow`](./taskflow-fromasyncflow.md) [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L256)
- [`TaskFlow.orElseTask`](./taskflow-orelsetask.md) [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L177)
- [`TaskFlow.orElseAsync`](./taskflow-orelseasync.md): Turns a pure validation result into a task flow with task-provided failure. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L192)
- [`TaskFlow.orElseFlow`](./taskflow-orelseflow.md) [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L205)
- [`TaskFlow.orElseAsyncFlow`](./taskflow-orelseasyncflow.md) [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L219)
- [`TaskFlow.orElseTaskFlow`](./taskflow-orelsetaskflow.md) [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L237)
- [`Flow.provideLayer`](./flow-providelayer.md): Provides a derived environment from a layer flow to a downstream flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L272)
- [`AsyncFlow.provideLayer`](./asyncflow-providelayer.md): Provides a derived environment from a layer flow to a downstream flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/AsyncFlow.fs#L254)
- [`TaskFlow.provideLayer`](./taskflow-providelayer.md): Provides a derived environment from a layer flow to a downstream task flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L510)

## Builder extensions

- module `TaskFlowBuilderExtensions` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L1023)
- module `AsyncFlowBuilderExtensions`: [omit] [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L1097)

