---
title: Interop
description: Source-documented task and async interop helpers for FsFlow.Net.
---

# Interop

This page shows the interop helpers that bridge task-based boundaries to sync and async boundaries when that is the honest runtime shape.

## TaskFlow bridges

- `TaskFlow.fromFlow` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Net/TaskFlow.fs#L176)
- `TaskFlow.fromAsyncFlow` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Net/TaskFlow.fs#L179)
- `TaskFlow.orElseTask` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Net/TaskFlow.fs#L104)
- `TaskFlow.orElseAsync` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Net/TaskFlow.fs#L116)
- `TaskFlow.orElseFlow` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Net/TaskFlow.fs#L128)
- `TaskFlow.orElseAsyncFlow` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Net/TaskFlow.fs#L142)
- `TaskFlow.orElseTaskFlow` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Net/TaskFlow.fs#L160)

## Builder extensions

The builder extension modules are the supported customization surface. The builder types themselves stay out of the narrative.

- module `TaskFlowBuilderExtensions` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Net/TaskFlow.fs#L907)
- module `AsyncFlowBuilderExtensions` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Net/TaskFlow.fs#L1034)

## Source

- [TaskFlow.fs](https://github.com/adz/FsFlow/blob/main/src/FsFlow.Net/TaskFlow.fs)
