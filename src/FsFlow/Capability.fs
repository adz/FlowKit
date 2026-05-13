namespace FsFlow

open System
open System.Threading
open System.Threading.Tasks

[<AutoOpen>]
module ResolverExtensions =
    type Flow<'env, 'error, 'value> with
#if FABLE_COMPILER
        static member inline ResolveService
            (projection: 'env -> 'resolve)
            : Flow<'env, 'error, 'resolve> =
            Flow(fun environment _ -> EffectFlow.ofValue (projection environment))

        /// <summary>Reads a dependency from <see cref="IServiceProvider" /> and fails when it is not registered.</summary>
        static member inline ResolveFromProvider
            ()
            : Flow<IServiceProvider, MissingCapability, 'resolve> =
            Flow(fun provider _ ->
                match provider.GetService typeof<'resolve> with
                | null ->
                    EffectFlow.ofError
                        {
                            CapabilityType = typeof<'resolve>
                        }
                | value -> EffectFlow.ofValue (unbox<'resolve> value))
#else
        static member ResolveService
            (projection: 'env -> 'resolve)
            : Flow<'env, 'error, 'resolve> =
            Flow(fun environment _ -> EffectFlow.ofValue (projection environment))

        /// <summary>Reads a dependency from <see cref="IServiceProvider" /> and fails when it is not registered.</summary>
        static member ResolveFromProvider
            ()
            : Flow<IServiceProvider, MissingCapability, 'resolve> =
            Flow(fun provider _ ->
                match provider.GetService typeof<'resolve> with
                | null ->
                    EffectFlow.ofError
                        {
                            CapabilityType = typeof<'resolve>
                        }
                | value -> EffectFlow.ofValue (unbox<'resolve> value))
#endif

        static member ProvideLayer
            (
                layer: Flow<'input, 'error, 'environment>,
                flow: Flow<'environment, 'error, 'value>
            ) : Flow<'input, 'error, 'value> =
            let (Flow layerOperation) = layer
            let (Flow flowOperation) = flow

            Flow(fun environment ct ->
                #if FABLE_COMPILER
                async {
                    let! exit = layerOperation environment ct
                    match exit with
                    | Exit.Success environment -> return! flowOperation environment ct
                    | Exit.Failure cause -> return Exit.Failure cause
                }
                #else
                match (layerOperation environment ct).GetAwaiter().GetResult() with
                | Exit.Success environment -> flowOperation environment ct
                | Exit.Failure cause -> EffectFlow.ofCause cause
                #endif
            )

/// <summary>Helpers for working with capabilities in task workflows.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Resolver =
    /// <summary>Reads a dependency from the environment using the provided projection.</summary>
    let resolve
        (projection: 'env -> 'resolve)
        : Flow<RuntimeContext<'runtime, 'env>, 'error, 'resolve> =
        Flow.read (fun context -> projection context.Environment)

    /// <summary>Reads the current runtime from the environment.</summary>
    let runtime<'runtime, 'env, 'error> () : Flow<RuntimeContext<'runtime, 'env>, 'error, 'runtime> =
        Flow.read (fun context -> context.Runtime)

    /// <summary>Reads the application environment from the environment.</summary>
    let environment<'runtime, 'env, 'error> () : Flow<RuntimeContext<'runtime, 'env>, 'error, 'env> =
        Flow.read (fun context -> context.Environment)

    /// <summary>Reads a dependency from <see cref="IServiceProvider" /> and fails when it is not registered.</summary>
    #if FABLE_COMPILER
    let inline fromProvider<'resolve> : Flow<IServiceProvider, MissingCapability, 'resolve> =
        Flow.ResolveFromProvider()
    #else
    let fromProvider<'resolve> : Flow<IServiceProvider, MissingCapability, 'resolve> =
        Flow.ResolveFromProvider()
    #endif

/// <summary>Helpers for deriving an environment in one flow and consuming it in another.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Layer =
    /// <summary>Provides a derived environment from a layer flow to a downstream flow.</summary>
    let provideLayer
        (layer: Flow<'input, 'error, 'environment>)
        (flow: Flow<'environment, 'error, 'value>)
        : Flow<'input, 'error, 'value> =
        Flow.ProvideLayer(layer, flow)
