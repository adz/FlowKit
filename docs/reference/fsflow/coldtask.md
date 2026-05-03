---
title: ColdTask
description: Source-documented delayed task helpers used by FsFlow.
---

# ColdTask

This page shows the delayed task helper surface used by `TaskFlow`, with source links so the cold/hot distinction stays anchored to the implementation.

## Core shape

- type `ColdTask`: Represents delayed task work that can observe a runtime cancellation token when it is started. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L23)
- module `ColdTask`: Core functions for creating and executing cold tasks. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L31)
- `ColdTask.run` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L55)

## Creation helpers

- `ColdTask.create` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L32)
- `ColdTask.fromTaskFactory` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L35)
- `ColdTask.fromTask` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L38)
- `ColdTask.fromValueTaskFactory` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L41)
- `ColdTask.fromValueTaskFactoryWithoutCancellation` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L46)
- `ColdTask.fromValueTask` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L51)

## Source

- [TaskFlow.fs](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs)
