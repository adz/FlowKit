# FSharpPlus Support Findings

This note records what would need to exist for FsFlow types to participate in FSharpPlus-style generic operators and related abstractions.

The short version:

- `Flow`, `AsyncFlow`, and `TaskFlow` already have the *behavior* needed for the common FP operators.
- FSharpPlus-style generic dispatch would still need the relevant static members on the flow types themselves.
- `Monoid` and `Alternative` are only a fit with a constrained or adapted meaning of `empty`.
- `bimap`-style support looks feasible for the success/error pair, but not as a blanket replacement for every three-parameter aspect of the flow types.

## What FSharpPlus Would Need

FSharpPlus generic operators dispatch through specific static member names on the concrete type.
For FsFlow, the current module functions are not enough on their own.

For the basic operator families, FsFlow would need these type members on `Flow`, `AsyncFlow`, and `TaskFlow`:

- `Map`
- `Return`
- `(>>=)`
- `(<*>)`
- `Lift2` if we want the optimized applicative path
- `Join` if we want the generic monad shortcut

The module-level combinators already exist:

- `map`
- `bind`
- `apply`
- `map2`
- `orElse`
- `orElseWith`
- `mapError`

So the missing work is mostly about adding the member surface that FSharpPlus looks for, not redesigning the underlying semantics.

## What Fits Cleanly

### Functor

This is the easiest fit.

- `Flow.map`
- `AsyncFlow.map`
- `TaskFlow.map`
- `Validation.map`
- `Result.map`-style behavior through the standard result surface

If the flow types exposed `Map`, FSharpPlus `map` / `<!>` would work directly.

### Applicative

This is also a good fit.

FsFlow already has the right behavior:

- `apply`
- `map2`
- `map3`
- `<!>`
- `<*>`

The remaining requirement is the static member shape that FSharpPlus expects.

### Monad

This is a good fit for the flow families.

- `bind`
- `>>=`

The semantics already match the usual monadic story:

- `Flow` is environment threaded
- `AsyncFlow` is environment threaded over `Async`
- `TaskFlow` is environment threaded over `Task` plus cancellation

### Bifunctor-Style Support

This is the part that looks especially promising for the success/error pair.

FsFlow already has:

- `map` for the success side
- `mapError` for the error side

That means `bimap`-style support is straightforward to adapt for:

- `Result`
- `Validation`
- the result layer carried by `Flow`, `AsyncFlow`, and `TaskFlow`

In practice, that means `bimap` is the most natural "both sides" extension point.
It is a better fit than trying to treat the whole flow type as a plain pair-like bifunctor, because the environment parameter is not part of the payload transformation story.

## What `empty` Could Mean

This is where the answer depends on which abstraction you want.

### Option 1: `empty` as a success identity

This is the closest fit to a monoid-like interpretation.

If the success value type itself is a monoid, then a plausible `empty` for a flow family would be:

- `Flow.empty` = a successful flow that returns `Monoid.zero` for the success value
- `AsyncFlow.empty` = same idea, but async
- `TaskFlow.empty` = same idea, but task-based

That makes `empty` a value identity, not a fallback choice.

This only works cleanly when the success type has a real identity element.
It does not solve the typed-error question by itself.

### Option 2: `empty` as a choice identity

This is the `Alternative` reading.

That is harder to make honest for typed flows because `empty` would need to mean "a computation that contributes no value and no failure", which is not the same thing as the current `Flow` / `AsyncFlow` / `TaskFlow` model.

If we want this meaning, we would probably need a constrained adaptation such as:

- a separate wrapper dedicated to choice/fallback semantics
- or a restricted instance where the error type is fixed in a way that gives a true identity

In other words, `orElse` already gives the fallback behavior, but it is not enough to justify a generic `Alternative` / `Monoid` instance on the flow types themselves.

### Recommendation

If we add `empty`, the least surprising version is:

- `empty` returns `ok zero` for success types that are themselves monoids
- `orElse` remains the explicit fallback operator

That keeps the identity story honest.

## What Else From FSharpPlus Could Fit

### Bimap / First / Second

`bimap`-style support is feasible for the result-shaped part of the flow families.

Likely useful members:

- `Bimap`
- `Map` on the success side
- `MapError` on the failure side

That would cover the "bi*" story the way most users expect it to work.

### Profunctor / Kleisli-Style Ideas

The flow families are already very close to Kleisli-style functions:

- `Flow<'env, 'error, 'value>` behaves like `env -> Result<value, error>`
- `AsyncFlow<'env, 'error, 'value>` behaves like `env -> Async<Result<value, error>>`
- `TaskFlow<'env, 'error, 'value>` behaves like `env -> CancellationToken -> Task<Result<value, error>>`

That makes `Kleisli`-style composition a plausible integration target.

The relevant FsFlow pieces already exist in spirit:

- `read`
- `localEnv`
- `provideLayer`
- `map`
- `bind`

So a generic arrow/profunctor story is conceptually possible, but it would need careful member naming because the environment parameter is not just "one side of a pair".

### Foldable / Traversable

These do not look like a direct fit for the flow wrappers themselves.

The flow types are not collections, so generic fold/traverse abstractions would be forced rather than natural.

Where traversal-like behavior does fit:

- `Validation.collect`
- `Validation.sequence`
- `Validation.traverse`
- the sequence/traverse helpers on the family modules

### Comonad

Not a good fit.

The flow families are effectful computations with environment and typed failure, not values that you can naturally `extract` and `extend` in a useful generic way.

## Findings Summary

1. `Functor`, `Applicative`, and `Monad` support are the cleanest FSharpPlus-style additions for the flow families.
2. `bimap`-style support is realistic for the success/error pair and is probably the best "both sides" extension.
3. A generic `empty` only fits cleanly if it means `ok zero` for a monoid-valued success type.
4. `Alternative` / `Monoid` on the flow wrappers themselves are not an obvious fit unless we introduce a more specialized choice wrapper or a fixed identity story.
5. `Foldable`, `Traversable`, and `Comonad` do not look like natural fits for the flow wrappers.

## Practical Recommendation

If the goal is FSharpPlus compatibility without compromising FsFlow semantics, the best path is:

- add the basic static members for `Functor` / `Applicative` / `Monad`
- add `MapError` / `Bimap`-style support for both sides of the result payload
- treat `empty` as an optional, constrained success identity rather than a universal choice identity
- leave true `Alternative` / `Monoid` support out unless there is a concrete specialized wrapper that makes the identity meaning unambiguous
