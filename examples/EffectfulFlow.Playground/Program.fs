open System
open System.Threading
open System.Threading.Tasks
open EffectfulFlow

type AppEnv =
    { Prefix: string
      LoadSuffix: CancellationToken -> Task<string> }

type AppError =
    | MissingName
    | SuffixFailed of string

let validateName (name: string) =
    if String.IsNullOrWhiteSpace name then
        Error MissingName
    else
        Ok name

let greet (name: string) : Flow<AppEnv, AppError, string> =
    flow {
        let! validName = validateName name
        let! prefix = Flow.read _.Prefix
        let! loadSuffix = Flow.read _.LoadSuffix

        let! suffix =
            loadSuffix
            |> Flow.Task.fromCold
            |> Flow.catch (fun error -> SuffixFailed error.Message)

        return $"{prefix} {validName}{suffix}"
    }

[<EntryPoint>]
let main _ =
    let env =
        { Prefix = "Hello"
          LoadSuffix = fun _ -> Task.FromResult "!" }

    let run input =
        greet input
        |> Flow.run env CancellationToken.None
        |> Async.RunSynchronously
        |> printfn "%s -> %A" input

    printfn "Playground:"
    run "Ada"
    run ""
    0
