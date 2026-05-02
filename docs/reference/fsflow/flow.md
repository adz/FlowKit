---
title: Flow
description: Source-documented synchronous and async workflow surface in FsFlow.
---

# Flow

This page shows the source-documented `Flow` and `AsyncFlow` surface, with source links on every member so the reference stays tied to the code.

## Flow

- type `Flow`: Represents a cold synchronous workflow that reads an environment, returns a typed result, and is executed explicitly through `Flow.run`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L12)
- module `Flow`: Core functions for creating, composing, executing, and adapting synchronous flows. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L150)
- `Flow.run`: Executes a synchronous flow with the provided environment. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L152)
- `Flow.succeed`: Creates a successful synchronous flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L156)
- `Flow.value`: Alias for `succeed` that reads well in some call sites. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L160)
- `Flow.fail`: Creates a failing synchronous flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L164)
- `Flow.fromResult`: Lifts a `Result` into a synchronous flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L168)
- `Flow.fromOption`: Lifts an option into a synchronous flow with the supplied error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L172)
- `Flow.fromValueOption`: Lifts a value option into a synchronous flow with the supplied error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L178)
- `Flow.orElseFlow`: Turns a pure validation result into a synchronous flow with environment-provided failure. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L184)
- `Flow.env`: Reads the current environment as the flow value. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L197)
- `Flow.read`: Projects a value from the current environment. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L201)
- `Flow.map`: Maps the successful value of a synchronous flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L205)
- `Flow.bind`: Sequences a synchronous continuation after a successful value. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L212)
- `Flow.tap`: Runs a synchronous side effect on success and preserves the original value. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L228)
- `Flow.tapError`: Runs a synchronous side effect on failure and preserves the original error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L239)
- `Flow.mapError`: Maps the error value of a synchronous flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L252)
- `Flow.catch`: Catches exceptions raised during execution and maps them to a typed error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L264)
- `Flow.orElse`: Falls back to another flow when the source flow fails. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L275)
- `Flow.zip`: Combines two flows into a tuple of their values. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L285)
- `Flow.map2`: Combines two flows with a mapping function. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L296)
- `Flow.localEnv`: Transforms the environment before running the flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L305)
- `Flow.delay`: Defers flow construction until execution time. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L312)
- `Flow.traverse`: Transforms a sequence of values into a flow and stops at the first failure. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L316)
- `Flow.sequence`: Transforms a sequence of flows into a flow of a sequence and stops at the first failure. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L335)

## AsyncFlow

- type `AsyncFlow`: Represents a cold async workflow that reads an environment, returns a typed result, and is executed explicitly through `AsyncFlow.run`. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L23)
- module `AsyncFlow`: Core functions for creating, composing, executing, and adapting async flows. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L343)
- `AsyncFlow.run`: Executes an async flow with the provided environment. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L345)
- `AsyncFlow.toAsync`: Converts an async flow into its raw async result shape. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L352)
- `AsyncFlow.succeed`: Creates a successful async flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L356)
- `AsyncFlow.fail`: Creates a failing async flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L360)
- `AsyncFlow.fromResult`: Lifts a `Result` into an async flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L364)
- `AsyncFlow.fromOption`: Lifts an option into an async flow with the supplied error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L368)
- `AsyncFlow.fromValueOption`: Lifts a value option into an async flow with the supplied error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L374)
- `AsyncFlow.orElseAsync`: Turns a pure validation result into an async flow with async-provided failure. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L380)
- `AsyncFlow.orElseAsyncFlow`: Turns a pure validation result into an async flow whose failure value comes from another async flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L393)
- `AsyncFlow.fromFlow`: Lifts a synchronous flow into an async flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L410)
- `AsyncFlow.fromAsync`: Lifts an async value into an async flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L414)
- `AsyncFlow.fromAsyncResult`: Lifts an async result into an async flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L422)
- `AsyncFlow.env`: Reads the current environment as the flow value. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L426)
- `AsyncFlow.read`: Projects a value from the current environment. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L430)
- `AsyncFlow.map`: Maps the successful value of an async flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L434)
- `AsyncFlow.bind`: Sequences an async continuation after a successful value. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L450)
- `AsyncFlow.tap`: Runs an async side effect on success and preserves the original value. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L470)
- `AsyncFlow.tapError`: Runs an async side effect on failure and preserves the original error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L481)
- `AsyncFlow.mapError`: Maps the error value of an async flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L500)
- `AsyncFlow.catch`: Catches exceptions raised during execution and maps them to a typed error. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L516)
- `AsyncFlow.orElse`: Falls back to another async flow when the source flow fails. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L529)
- `AsyncFlow.zip`: Combines two async flows into a tuple of their values. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L543)
- `AsyncFlow.map2`: Combines two async flows with a mapping function. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L554)
- `AsyncFlow.localEnv`: Transforms the environment before running the async flow. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L563)
- `AsyncFlow.delay`: Defers async flow construction until execution time. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L570)
- `AsyncFlow.traverse`: Transforms a sequence of values into an async flow and stops at the first failure. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L574)
- `AsyncFlow.sequence`: Transforms a sequence of async flows into an async flow of a sequence and stops at the first failure. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L597)

## Builder entry points

The builder entry points are the syntax layer on top of the module surface. Keep using the modules when you want the actual API members.

- `Builders.result`: The fail-fast `result { }` computation expression. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L1179)
- `Builders.flow`: The sync-only `flow { }` computation expression. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L1184)
- `Builders.asyncFlow`: The core `asyncFlow { }` computation expression. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L1189)
- `Builders.validate`: The accumulating `validate { }` computation expression. [source](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs#L1194)

## Source

- [Flow.fs](https://github.com/adz/FsFlow/blob/main/src/FsFlow/Flow.fs)
