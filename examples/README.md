# Examples

Read this page when you want to see what EffectfulFlow looks like in code before reading the API surface in detail.

## Run The Examples

Main example:

```bash
dotnet run --project examples/EffectfulFlow.Examples/EffectfulFlow.Examples.fsproj --nologo
```

Maintenance example:

```bash
dotnet run --project examples/EffectfulFlow.MaintenanceExamples/EffectfulFlow.MaintenanceExamples.fsproj --nologo
```

Playground example:

```bash
dotnet run --project examples/EffectfulFlow.Playground/EffectfulFlow.Playground.fsproj --nologo
```

NativeAOT probe:

```bash
bash scripts/run-aot-probe.sh
```

## Main Example

The main example in [`examples/EffectfulFlow.Examples/Program.fs`](./EffectfulFlow.Examples/Program.fs) shows a small application-shaped set of flows:

- validate configuration with plain `Result`
- build a smaller runtime environment from config
- call a `Task<Result<_,_>>` dependency
- retry transient failures
- apply a timeout
- persist an audit record through a `Task` boundary
- scope an async resource with `use`
- compose smaller flows into a larger config-driven flow with `Flow.mapEnv`

Read it in this order:

1. `validateConfig`
2. `fetchResponse`
3. `saveAudit`
4. `program`

## Maintenance Example

The maintenance example in [`examples/EffectfulFlow.MaintenanceExamples/Program.fs`](./EffectfulFlow.MaintenanceExamples/Program.fs) is smaller and more focused. It shows:

- how to normalize awkward nested wrapper shapes one layer at a time
- the difference between cold task factories and already-created task values

## Playground Example

The playground example in [`examples/EffectfulFlow.Playground/Program.fs`](./EffectfulFlow.Playground/Program.fs) is the quickest way to feel the new surface in practice. It shows:

- plain `Result` validation first
- a small `flow {}` workflow
- projected environment reads through `Flow.read`
- one `.NET` boundary through `Flow.Task.fromCold`

## Next

If you want the smallest introduction, read [`docs/GETTING_STARTED.md`](../docs/GETTING_STARTED.md). If you are migrating from `Async<Result<_,_>>`, read [`docs/FSTOOLKIT_MIGRATION.md`](../docs/FSTOOLKIT_MIGRATION.md).
