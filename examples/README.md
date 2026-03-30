# Examples

The `examples/` area is meant to answer one question quickly:

"What does normal application code look like with this library?"

Run the current example:

```bash
dotnet run --project examples/EffectFs.Examples/EffectFs.Examples.fsproj --nologo
```

## What The Example Covers

The sample is intentionally small, but it shows the main workflow shape:

- validate config with plain `Result`
- bind that validation directly inside `effect {}`
- project a larger application environment into smaller dependency views
- log through an explicit environment capability
- call `Task`-based work without leaving the workflow
- retry expected transient failures
- apply a timeout at the effect boundary
- convert legacy exceptions into typed application errors

## Why This Example Matters

The point is not architecture theater.

The point is that a single workflow can show:

- what data it needs
- what failures it produces
- where dependency access happens
- where operational behavior like retry and timeout lives

without turning the happy path into wrapper plumbing.

## Expected Scenarios

The example prints four scenarios:

- `Success`: one transient failure, then a successful retry
- `Validation Failure`: bad config rejected before any request work starts
- `Retries Exhausted`: retry policy stops after the configured attempts
- `Legacy Failure Boundary`: a thrown exception is translated into a typed error deliberately

## Suggested Reading Order

If you want the fastest orientation:

1. Read `validateConfig`
2. Read `fetchResponse`
3. Read `program`

That gives you the pure validation story, the dependency/logging story, and the composed application flow in that order.
