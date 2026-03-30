open System
open System.Threading
open System.Threading.Tasks
open EffectFs

// Getting started:
//
// - use plain F# for pure validation and transformation
// - use Effect when code needs configuration, async work, tasks, or typed failures
// - keep the effect boundary visible in the type

type AppConfig =
    { ApiBaseUrl: string
      ApiKey: string
      RetryCount: int
      RequestTimeout: TimeSpan
      Prefix: string
      FailuresBeforeSuccess: int
      SimulateLegacyFailure: bool }

type AppEnvironment =
    { Config: AppConfig
      AttemptCount: int ref
      WriteLog: LogEntry -> unit }

type ValidationError =
    | MissingValue of string
    | NonPositiveNumber of string

type AppError =
    | ValidationFailed of ValidationError
    | LegacyFailure of string
    | TimedOut
    | TransientFailure of int

type RequestPlan =
    { Banner: string
      Url: string
      RetryCount: int
      RequestTimeout: TimeSpan }

type Response =
    { StatusCode: int
      Body: string }

let execute<'env, 'value>
    (environment: 'env)
    (workflow: Effect<'env, AppError, 'value>)
    : Result<'value, AppError> =
    workflow
    |> Effect.execute environment
    |> Async.RunSynchronously

let requireNonEmpty (name: string) (value: string) : Result<string, ValidationError> =
    if String.IsNullOrWhiteSpace value then
        Error(MissingValue name)
    else
        Ok value

let requirePositive (name: string) (value: int) : Result<int, ValidationError> =
    if value > 0 then
        Ok value
    else
        Error(NonPositiveNumber name)

let writeLog (environment: AppEnvironment) (entry: LogEntry) : unit =
    environment.WriteLog entry

let logInformation (message: string) : Effect<AppEnvironment, AppError, unit> =
    Effect.log writeLog LogLevel.Information message

let createEnvironment (config: AppConfig) : AppEnvironment =
    { Config = config
      AttemptCount = ref 0
      WriteLog = fun entry -> printfn "[%A] %s" entry.Level entry.Message }

let validateConfig : Effect<AppConfig, AppError, RequestPlan> =
    effect {
        let! config = Effect.environment<AppConfig, AppError>

        let! apiBaseUrl =
            requireNonEmpty "apiBaseUrl" config.ApiBaseUrl
            |> Result.mapError ValidationFailed

        let! apiKey =
            requireNonEmpty "apiKey" config.ApiKey
            |> Result.mapError ValidationFailed

        let! retryCount =
            requirePositive "retryCount" config.RetryCount
            |> Result.mapError ValidationFailed

        let! timeoutMilliseconds =
            int config.RequestTimeout.TotalMilliseconds
            |> requirePositive "requestTimeoutMs"
            |> Result.mapError ValidationFailed

        let banner =
            sprintf "%s :: %s" config.Prefix apiKey
            |> fun value -> value.ToUpperInvariant()

        return
            { Banner = banner
              Url = sprintf "%s/ping" apiBaseUrl
              RetryCount = retryCount
              RequestTimeout = TimeSpan.FromMilliseconds(float timeoutMilliseconds) }
    }

let fetchResponse (plan: RequestPlan) : Effect<AppEnvironment, AppError, Response> =
    effect {
        let! environment = Effect.environment<AppEnvironment, AppError>
        let attempt = environment.AttemptCount.Value + 1
        environment.AttemptCount.Value <- attempt

        do! logInformation (sprintf "attempt=%d url=%s" attempt plan.Url)

        if attempt <= environment.Config.FailuresBeforeSuccess then
            return! Error(TransientFailure attempt)

        let! body =
            Task.FromResult(sprintf "GET %s (retries=%d)" plan.Url plan.RetryCount)

        return
            { StatusCode = 200
              Body = body }
    }
    |> Effect.retry
        { MaxAttempts = plan.RetryCount + 1
          Delay = fun attempt -> TimeSpan.FromMilliseconds(float (attempt * 10))
          ShouldRetry =
            function
            | TransientFailure _ -> true
            | _ -> false }
    |> Effect.timeout plan.RequestTimeout TimedOut

let runLegacyBoundary : Effect<AppConfig, AppError, unit> =
    Effect.delay(fun () ->
        effect {
            let! shouldFail = Effect.read (fun (config: AppConfig) -> config.SimulateLegacyFailure)

            if shouldFail then
                invalidOp "legacy logger exploded"

            return ()
        })
    |> Effect.catch (fun error -> LegacyFailure error.Message)

let program : Effect<AppConfig, AppError, string> =
    effect {
        let! appConfig = Effect.environment<AppConfig, AppError>
        let appEnvironment = createEnvironment appConfig

        let! () =
            Effect.withEnvironment (fun (_: AppConfig) -> appEnvironment) (logInformation "starting program")

        let! plan = validateConfig
        let! response = fetchResponse plan |> Effect.withEnvironment (fun (_: AppConfig) -> appEnvironment)
        let! () = runLegacyBoundary

        return
            sprintf
                "%s -> %d %s (attempts=%d)"
                plan.Banner
                response.StatusCode
                response.Body
                appEnvironment.AttemptCount.Value
    }

let printScenario (label: string) (config: AppConfig) : unit =
    printfn ""
    printfn "== %s ==" label
    printfn "input: %A" config

    let result = execute config program
    printfn "result: %A" result

[<EntryPoint>]
let main _ =
    let success =
        { ApiBaseUrl = "https://example.test"
          ApiKey = "demo-key"
          RetryCount = 2
          RequestTimeout = TimeSpan.FromMilliseconds 250
          Prefix = "demo"
          FailuresBeforeSuccess = 1
          SimulateLegacyFailure = false }

    let validationFailure =
        { success with
            ApiKey = ""
            RetryCount = 0
            RequestTimeout = TimeSpan.Zero }

    let exhaustedRetryFailure =
        { success with
            RetryCount = 1
            FailuresBeforeSuccess = 2 }

    let legacyFailure =
        { success with
            SimulateLegacyFailure = true }

    printScenario "Success" success
    printScenario "Validation Failure" validationFailure
    printScenario "Retries Exhausted" exhaustedRetryFailure
    printScenario "Legacy Failure Boundary" legacyFailure
    0
