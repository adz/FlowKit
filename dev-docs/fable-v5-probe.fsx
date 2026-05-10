open System
open System.Threading
open System.Threading.Tasks

#if FABLE_COMPILER
open Fable.Core
#endif

// 1. The Unified Type Alias
#if FABLE_COMPILER
type Effect<'v, 'e> = JS.Promise<Result<'v, 'e>>
#else
type Effect<'v, 'e> = ValueTask<Result<'v, 'e>>
#endif

type Flow<'env, 'err, 'res> = 'env -> CancellationToken -> Effect<'res, 'err>

// 2. The Unified Builder
type FlowBuilder() =
    member _.Return(value: 'v) : Flow<'env, 'e, 'v> =
        fun _ _ -> 
            #if FABLE_COMPILER
            JS.Constructors.Promise.resolve(Ok value)
            #else
            ValueTask.FromResult(Ok value)
            #endif

    member _.Bind(flw: Flow<'env, 'e, 'v>, binder: 'v -> Flow<'env, 'e, 'u>) : Flow<'env, 'e, 'u> =
        fun env ct ->
            #if FABLE_COMPILER
            promise {
                let! res = flw env ct
                match res with
                | Ok v -> return! (binder v) env ct
                | Error e -> return Error e
            }
            #else
            let task = flw env ct
            if task.IsCompletedSuccessfully then
                match task.Result with
                | Ok v -> (binder v) env ct
                | Error e -> ValueTask.FromResult(Error e)
            else
                ValueTask<Result<'u, 'e>>(
                    task.AsTask().ContinueWith(fun (t: Task<Result<'v, 'e>>) ->
                        match t.Result with
                        | Ok v -> (binder v) env ct |> (fun vt -> vt.AsTask())
                        | Error e -> Task.FromResult(Error e)
                    ).Unwrap()
                )
            #endif

    member _.Bind(task: Task<'v>, binder: 'v -> Flow<'env, 'e, 'u>) : Flow<'env, 'e, 'u> =
        fun env ct ->
            #if FABLE_COMPILER
            promise {
                let! v = task
                return! (binder v) env ct
            }
            #else
            ValueTask<Result<'u, 'e>>(
                task.ContinueWith(fun (t: Task<'v>) ->
                    (binder t.Result) env ct |> (fun vt -> vt.AsTask())
                ).Unwrap()
            )
            #endif

    member _.Bind(res: Result<'v, 'e>, binder: 'v -> Flow<'env, 'e, 'u>) : Flow<'env, 'e, 'u> =
        match res with
        | Ok v -> binder v
        | Error e -> fun _ _ -> 
            #if FABLE_COMPILER
            JS.Constructors.Promise.resolve(Error e)
            #else
            ValueTask.FromResult(Error e)
            #endif

    member _.Zero() : Flow<'env, 'e, unit> =
        fun _ _ -> 
            #if FABLE_COMPILER
            JS.Constructors.Promise.resolve(Ok ())
            #else
            ValueTask.FromResult(Ok ())
            #endif

[<AutoOpen>]
module GlobalBuilder =
    let flow = FlowBuilder()

// 3. Test Usage
module Test =
    type Env = { Id: string }
    
    let getEnvId () : Flow<Env, string, string> =
        fun env _ -> 
            #if FABLE_COMPILER
            JS.Constructors.Promise.resolve(Ok env.Id)
            #else
            ValueTask.FromResult(Ok env.Id)
            #endif

    let combinedWorkflow : Flow<Env, string, string> =
        flow {
            let! id = getEnvId()
            let! taskVal = Task.FromResult(42)
            let! resVal = Ok "success"
            return $"{id}-{taskVal}-{resVal}"
        }

    let runTest () =
        let env = { Id = "test-env" }
        let ct = CancellationToken.None
        #if FABLE_COMPILER
        // In Fable, this would log to console
        combinedWorkflow env ct |> ignore
        #else
        let result = (combinedWorkflow env ct).GetAwaiter().GetResult()
        printfn "Result: %A" result
        #endif

Test.runTest()
