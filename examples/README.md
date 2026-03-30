# Examples

The `examples/` area is meant to answer one question quickly:

"What does ordinary application code look like with this library?"

Run the example:

```bash
dotnet run --project examples/EffectFs.Examples/EffectFs.Examples.fsproj --nologo
```

## What The Current Example Shows

The example is intentionally application-shaped rather than tutorial-shaped.

It includes:

- plain `Result`-based validation
- explicit environment creation
- a `Task<Result<_,_>>` gateway dependency
- a `Task`-based persistence dependency
- direct logging through environment capabilities
- retry and timeout around the gateway call
- async cleanup with `usingAsync`
- typed failure mapping for validation, gateway, persistence, timeout, cancellation, and legacy exceptions

## Why This Matters

This is the point where the library either becomes credible or not.

If the example only showed `map` and `bind`, it would not prove much.

The current example is trying to show a workflow that looks like real .NET application code:

- validate configuration
- call an external dependency
- retry transient failures
- persist an audit record
- clean up a scope
- surface typed application errors

## Reading Order

Read the example in this order:

1. `validateConfig`
2. `fetchResponse`
3. `saveAudit`
4. `program`

That sequence shows the pure validation layer, the dependency/operational layer, the persistence layer, and the full composed workflow.
