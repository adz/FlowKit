---
title: Support Types
description: Source-documented runtime support types in FsFlow.
---

# Support Types

This page shows the support types that stay close to the runtime helpers without taking over the main workflow story.

## Logging

- type `LogLevel`: Log levels used by runtime logging helpers and environment-provided logging functions. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L31)
- type `LogEntry`: A structured log entry written through a runtime logger. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L42)

## Retry policy

- type `RetryPolicy`: Defines how runtime retry helpers repeat typed failures in a controlled way. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L52)
- module `RetryPolicy`: Standard retry policies for runtime helpers. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L63)
- `RetryPolicy.noDelay` [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L64)

## Source

- [Flow.fs](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs)
