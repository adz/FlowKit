---
weight: 50
title: "Level 4: Nominal Capability Helpers"
description: Small named contracts for reusable helpers.
---

# Level 4: Nominal Capability Helpers

Use nominal capability helpers when you need a reusable helper that should state its dependency contract explicitly.

This is the deepest level in the guide because it is the most abstract. It is useful, but only when the simpler record shapes are no longer enough.

## The Contract

`Requires<'dep>` says that an environment exposes a single dependency.

```fsharp
type IClock =
    abstract UtcNow : unit -> DateTimeOffset

type IHasClock =
    inherit Requires<IClock>
    abstract Clock : IClock
```

The contract is tiny on purpose. When it starts to grow, that is usually a sign to return to level 1.

## Reading Through The Contract

`Resolver.resolve` is the matching read operation.

```fsharp
let getTimestamp : Flow<#IHasClock, unit, DateTimeOffset> =
    flow {
        let! clock = Resolver.resolve _.Clock
        return clock.UtcNow()
    }
```

This is a good fit for shared helpers that need a named capability surface.

## When It Is Worth It

Use this level when:

- a helper is shared across multiple areas
- the dependency contract is stable
- a record would be too feature-specific or too wide

## When It Is Not Worth It

Do not use nominal helpers just because they look more “architectural.”

If you only need one feature boundary, a record is better.
If you only need host/application separation, use `RuntimeContext`.
If you only need host registrations, use the provider edge.

The nominal layer is for reusable helper surfaces, not for making every dependency more abstract than it needs to be.

See the [Resolver reference](../../reference/capability/) for `Requires<'dep>`, `Resolve<'dep>`, and `Resolver.resolve`.
