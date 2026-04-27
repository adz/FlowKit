open System
open System.Threading
open System.Threading.Tasks
open FsFlow

let run label workflow =
    let result =
        workflow
        |> Flow.toAsync () CancellationToken.None
        |> Async.RunSynchronously

    printfn "%s: %A" label result

let asyncAsyncResultExample : Flow<unit, string, int> =
    flow {
        let! (next: Async<Result<int, string>>) =
            async {
                return async { return Ok 42 }
            }

        let! (value: int) = next
        return value
    }

let resultOfAsyncExample : Flow<unit, string, int> =
    flow {
        let! (next: Async<int>) = Ok(async { return 42 })
        let! value = next
        return value
    }

let nestedResultExample : Flow<unit, string, int> =
    flow {
        let! (next: Result<int, string>) = Ok(Ok 42)
        let! value = next
        return value
    }

let coldTaskExample : Flow<unit, string, int> =
    let started = ref false

    Flow.Task.fromCold(fun (_: CancellationToken) ->
        started.Value <- true
        Task.FromResult 42)
    |> Flow.tap (fun _ ->
        flow {
            printfn "cold task started at execution time: %b" started.Value
            return ()
        })

let hotTaskValueExample () : Flow<unit, string, int> =
    let started = ref false

    let taskValue =
        started.Value <- true
        Task.FromResult 42

    printfn "hot task started before flow execution: %b" started.Value
    Flow.Task.fromHot taskValue

[<EntryPoint>]
let main _ =
    printfn "Normalize nested wrappers one layer at a time."
    run "Async<Async<Result<int,string>>>" asyncAsyncResultExample
    run "Result<Async<int>,string>" resultOfAsyncExample
    run "Result<Result<int,string>,string>" nestedResultExample
    run "Cold Task" coldTaskExample
    run "Hot Task Value" (hotTaskValueExample ())
    0
