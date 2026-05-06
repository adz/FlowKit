---
title: TaskFlowSpec
description: Source-documented task workflow specification for FsFlow.
---

# TaskFlowSpec

This page shows the source-documented `TaskFlowSpec` surface, used for defining and running task workflows with explicit configurations.

## Core type

- type [`TaskFlowSpec`](./taskflow-spec.md): Describes a task-flow program that is built against a runtime context and later executed with a cancellation token. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L713)

## Module functions

- module [`TaskFlowSpec`](./taskflow-spec.md): Helpers for creating and running `TaskFlowSpec{runtime, env, error, value}` values. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L728)
- [`TaskFlowSpec.create`](./taskflowspec-create.md): Creates a task-flow spec from runtime services, application dependencies, and a build function. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L730)
- [`TaskFlowSpec.run`](./taskflowspec-run.md): Runs the spec with the supplied cancellation token. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/TaskFlow.fs#L742)

