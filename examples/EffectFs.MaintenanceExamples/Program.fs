open System
open System.Threading
open System.Threading.Tasks
open EffectFs

let run label workflow =
    let result =
        workflow
        |> Effect.execute ()
        |> Async.RunSynchronously

    printfn "%s: %A" label result

let asyncAsyncResultExample : Effect<unit, string, int> =
    effect {
        let! (next: Async<Result<int, string>>) =
            async {
                return async { return Ok 42 }
            }

        let! (value: int) = next
        return value
    }

let resultOfAsyncExample : Effect<unit, string, int> =
    effect {
        let! (next: Async<int>) = Ok(async { return 42 })
        let! value = next
        return value
    }

let nestedResultExample : Effect<unit, string, int> =
    effect {
        let! (next: Result<int, string>) = Ok(Ok 42)
        let! value = next
        return value
    }

let coldTaskExample : Effect<unit, string, int> =
    let started = ref false

    Effect.fromColdTask(fun (_: CancellationToken) ->
        started.Value <- true
        Task.FromResult 42)
    |> Effect.tap (fun value ->
        effect {
            printfn "cold task started at execution time: %b" started.Value
            return ()
        })

let hotTaskValueExample () : Effect<unit, string, int> =
    let started = ref false

    let taskValue =
        started.Value <- true
        Task.FromResult 42

    printfn "hot task started before effect execution: %b" started.Value
    Effect.fromTaskValue taskValue

[<EntryPoint>]
let main _ =
    printfn "Normalize nested wrappers one layer at a time."
    run "Async<Async<Result<int,string>>>" asyncAsyncResultExample
    run "Result<Async<int>,string>" resultOfAsyncExample
    run "Result<Result<int,string>,string>" nestedResultExample
    run "Cold Task" coldTaskExample
    run "Hot Task Value" (hotTaskValueExample ())
    0
