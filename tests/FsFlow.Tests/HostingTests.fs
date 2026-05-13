namespace FsFlow.Tests

open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open FsFlow
open FsFlow.Hosting
open FsFlow.Capabilities.Core
open Swensen.Unquote
open Xunit

type RecordingLogger() =
    let entries = ResizeArray<Microsoft.Extensions.Logging.LogLevel * string>()
    member _.Entries = entries |> Seq.toList
    interface ILogger with
        member _.Log(level, _, state, _, _) = entries.Add(level, string state)
        member _.IsEnabled(_) = true
        member _.BeginScope(_) = { new IDisposable with member _.Dispose() = () }

type RecordingLoggerFactory(logger: RecordingLogger) =
    interface ILoggerFactory with
        member _.AddProvider(_) = ()
        member _.CreateLogger(_) = logger
        member _.Dispose() = ()

type private MockEnv =
    { EnvVars: IEnvironmentVariables }
    interface Requires<IEnvironmentVariables> with member this.Dep = this.EnvVars

module HostingTests =
    [<Fact>]
    let ``FsFlowLogger: forwards entries correctly`` () =
        let innerLogger = RecordingLogger()
        let fsLogger = FsFlowLogger(innerLogger)
        
        fsLogger.Log { Level = FsFlow.LogLevel.Information; Message = "Hello"; TimestampUtc = DateTimeOffset.UtcNow }
        
        test <@ innerLogger.Entries |> List.exists (fun (l, m) -> l = Microsoft.Extensions.Logging.LogLevel.Information && m.Contains("Hello")) @>

    [<Fact>]
    let ``Startup: validateEnvironment detects missing variables`` () =
        let flow : Flow<#Requires<IEnvironmentVariables>, EnvironmentVariableError, string> =
            EnvironmentVariable.get "FSFLOW_HOSTING_MISSING"
        let result = Startup.validateEnvironment flow
        
        match result with
        | Error [ message ] -> test <@ message.Contains("FSFLOW_HOSTING_MISSING") && message.Contains("not set") @>
        | _ -> failwithf "Expected missing variable error, got %A" result
