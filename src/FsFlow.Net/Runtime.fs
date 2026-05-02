namespace FsFlow.Net

open System
open System.Threading
open System.Threading.Tasks
open FsFlow

/// <summary>
/// Captures the two-context shape of a task workflow execution:
/// runtime services, application capabilities, and the cancellation token for the current run.
/// </summary>
/// <typeparam name="runtime">The type that carries runtime concerns.</typeparam>
/// <typeparam name="env">The type that carries application capabilities.</typeparam>
type RuntimeContext<'runtime, 'env> =
    {
        /// <summary>Runtime services for logging, metrics, tracing, or other operational concerns.</summary>
        Runtime: 'runtime

        /// <summary>Application dependencies and capabilities for the workflow.</summary>
        Environment: 'env

        /// <summary>The cancellation token for the current task execution.</summary>
        CancellationToken: CancellationToken
    }

/// <summary>Helpers for building and reshaping <see cref="RuntimeContext{runtime, env}" /> values.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module RuntimeContext =
    /// <summary>Creates a runtime context from the supplied runtime services, environment, and cancellation token.</summary>
    let create
        (runtime: 'runtime)
        (environment: 'env)
        (cancellationToken: CancellationToken)
        : RuntimeContext<'runtime, 'env> =
        {
            Runtime = runtime
            Environment = environment
            CancellationToken = cancellationToken
        }

    /// <summary>Reads the runtime half of a runtime context.</summary>
    let runtime (context: RuntimeContext<'runtime, 'env>) = context.Runtime

    /// <summary>Reads the application environment half of a runtime context.</summary>
    let environment (context: RuntimeContext<'runtime, 'env>) = context.Environment

    /// <summary>Reads the cancellation token stored in a runtime context.</summary>
    let cancellationToken (context: RuntimeContext<'runtime, 'env>) = context.CancellationToken

    /// <summary>Maps the runtime half of a runtime context.</summary>
    let mapRuntime
        (mapper: 'runtime -> 'nextRuntime)
        (context: RuntimeContext<'runtime, 'env>)
        : RuntimeContext<'nextRuntime, 'env> =
        {
            Runtime = mapper context.Runtime
            Environment = context.Environment
            CancellationToken = context.CancellationToken
        }

    /// <summary>Maps the application environment half of a runtime context.</summary>
    let mapEnvironment
        (mapper: 'env -> 'nextEnv)
        (context: RuntimeContext<'runtime, 'env>)
        : RuntimeContext<'runtime, 'nextEnv> =
        {
            Runtime = context.Runtime
            Environment = mapper context.Environment
            CancellationToken = context.CancellationToken
        }

    /// <summary>Replaces the runtime half of a runtime context.</summary>
    let withRuntime
        (runtime: 'nextRuntime)
        (context: RuntimeContext<'runtime, 'env>)
        : RuntimeContext<'nextRuntime, 'env> =
        mapRuntime (fun _ -> runtime) context

    /// <summary>Replaces the environment half of a runtime context.</summary>
    let withEnvironment
        (environment: 'nextEnv)
        (context: RuntimeContext<'runtime, 'env>)
        : RuntimeContext<'runtime, 'nextEnv> =
        mapEnvironment (fun _ -> environment) context
