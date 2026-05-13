---
title: "Resolver"
---

This page shows the source-documented resolver and layer surface, including Resolve request tokens, environment management helpers, and the runtime/application split used by RuntimeContext.

## Resolve tokens

- [`Requires`](./t-requires-1.md): Describes the capability contract for a single dependency.
- [`Resolve`](./t-resolve-1.md): Request token for binding a whole dependency inside a workflow.
- [`Resolve`](./t-resolve-2.md): Request token for projecting a value from a dependency.

## Dependencies

- [`MissingCapability`](./t-missingcapability.md): Describes a missing service-provider capability.
- [`Resolver.resolve`](./m-resolver-resolve.md): Reads a dependency from the environment using the provided projection.
- [`Resolver.runtime`](./m-resolver-runtime.md): Reads the current runtime from the environment.
- [`Resolver.environment`](./m-resolver-environment.md): Reads the application environment from the environment.
- [`Resolver.fromProvider`](./m-resolver-fromprovider.md): Reads a dependency from <a href="https://learn.microsoft.com/dotnet/api/iserviceprovider">IServiceProvider</a> and fails when it is not registered.

## Layers

- [`Layer.provideLayer`](./m-layer-providelayer.md): Provides a derived environment from a layer flow to a downstream flow.

