# FsFlow

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/content/img/fsflow-readme-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="docs/content/img/fsflow-readme-light.svg">
  <img alt="FsFlow" src="docs/content/img/fsflow-readme-light.svg" width="160">
</picture>

FsFlow keeps F# application workflows cold, typed, and explicit.

Use it when dependency threading, async work, and expected failures all show up in the same use case.

[![ci](https://github.com/adz/FsFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/adz/FsFlow/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/FsFlow.svg)](https://www.nuget.org/packages/FsFlow)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

Docs: [adz.github.io/FsFlow](https://adz.github.io/FsFlow/)

## What You Get

- `Flow` for synchronous workflows
- `AsyncFlow` for `Async`-based workflows in the core package
- `TaskFlow` for `.NET Task`-based workflows in `FsFlow.Net`

## Start Here

- [`docs/GETTING_STARTED.md`](docs/GETTING_STARTED.md) for the workflow-family overview
- [`docs/TINY_EXAMPLES.md`](docs/TINY_EXAMPLES.md) for the smallest runnable snippets
- [`docs/SEMANTICS.md`](docs/SEMANTICS.md) for the execution model
- [`docs/INTEGRATIONS_FSTOOLKIT.md`](docs/INTEGRATIONS_FSTOOLKIT.md) for FsToolkit coexistence and migration

## Install

- `FsFlow` for `Flow` and `AsyncFlow`
- `FsFlow.Net` for `TaskFlow`

Keep the domain plain F#.
Put FsFlow at the application boundary where the runtime shape is part of the problem.
