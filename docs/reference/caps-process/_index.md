---
title: "Capabilities Process"
---

This page shows the source-documented `FsFlow.Capabilities.Process` surface: the process runner interface and its helpers.

## Capability

- [`Capabilities.Process.IProcess`](./t-capabilities-process-iprocess.md): Provides asynchronous access to external process execution.
- [`Capabilities.Process.ProcessResult`](./t-capabilities-process-processresult.md): Represents the outcome of an external process execution.

## Helpers

- [`Capabilities.Process.Process.execute`](./m-capabilities-process-process-execute.md): Executes a process using the process environment and returns the result.
- [`Capabilities.Process.Process.live`](./m-capabilities-process-process-live.md): Creates a live process runner backed by <a href="https://learn.microsoft.com/dotnet/api/system.diagnostics.process">Process</a>.

