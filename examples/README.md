# Examples

Read this page when you want to see what Effect.FS looks like in code before reading the API surface in detail.

## Run The Examples

Main example:

```bash
dotnet run --project examples/EffectFs.Examples/EffectFs.Examples.fsproj --nologo
```

Maintenance example:

```bash
dotnet run --project examples/EffectFs.MaintenanceExamples/EffectFs.MaintenanceExamples.fsproj --nologo
```

## Main Example

The main example in [`examples/EffectFs.Examples/Program.fs`](./EffectFs.Examples/Program.fs) shows a small application-shaped workflow:

- validate configuration with plain `Result`
- build an application environment
- call a `Task<Result<_,_>>` dependency
- retry transient failures
- apply a timeout
- persist an audit record through a `Task` boundary
- clean up an async scope
- translate failures into a small application error type

Read it in this order:

1. `validateConfig`
2. `fetchResponse`
3. `saveAudit`
4. `program`

That gives you the pure validation layer first, then the dependency boundary, then persistence, then the composed workflow.

## Maintenance Example

The maintenance example in [`examples/EffectFs.MaintenanceExamples/Program.fs`](./EffectFs.MaintenanceExamples/Program.fs) is smaller and more focused. It shows:

- how to normalize awkward nested wrapper shapes one layer at a time
- the difference between cold task factories and already-created task values

Use it when your question is not "how do I build a workflow?" but "how do I keep weird boundaries readable?"

## What To Notice

Across both examples, the main patterns are:

- pure checks stay as plain `Result`
- `Effect` starts at the boundary where dependencies or async work begin
- environment access is explicit
- task boundaries are handled directly
- retry, timeout, and cleanup sit close to the workflow that needs them

## Next

If you want the smallest introduction, read [`docs/GETTING_STARTED.md`](../docs/GETTING_STARTED.md). If you are migrating from FsToolkit, read [`docs/FSTOOLKIT_MIGRATION.md`](../docs/FSTOOLKIT_MIGRATION.md).
