namespace FsFlow.Tests

open System
open FsFlow
open FsFlow.Capabilities.Core
open Swensen.Unquote
open Xunit

module CapsCoreTests =
    type private MockCapabilities =
        {
            Clock: IClock
            Random: IRandom
            Guid: IGuid
            EnvVars: IEnvironmentVariables
        }
        interface Requires<IClock> with member this.Dep = this.Clock
        interface Requires<IRandom> with member this.Dep = this.Random
        interface Requires<IGuid> with member this.Dep = this.Guid
        interface Requires<IEnvironmentVariables> with member this.Dep = this.EnvVars

    [<Fact>]
    let ``clock random guid and environment variable helpers are deterministic when fixed`` () =
        let capabilities =
            {
                Clock = Clock.fromValue (DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero))
                Random = Random.fromValue 4
                Guid = Guid.fromValue (global.System.Guid.Parse "11111111-1111-1111-1111-111111111111")
                EnvVars =
                    EnvironmentVariables.fromPairs
                        [ "FSFLOW_CAPS_PORT", "8080"
                          "FSFLOW_CAPS_ENABLED", "true"
                          "FSFLOW_CAPS_SESSION", "22222222-2222-2222-2222-222222222222" ]
            }

        test <@ Flow.runSync capabilities Clock.now = Exit.Success (DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero)) @>
        test <@ Flow.runSync capabilities (Random.nextInt 0 10) = Exit.Success 4 @>
        test <@ Flow.runSync capabilities Guid.newGuid = Exit.Success (global.System.Guid.Parse "11111111-1111-1111-1111-111111111111") @>
        test <@ Flow.runSync capabilities (EnvironmentVariables.tryGet "FSFLOW_CAPS_PORT") = Exit.Success (Some "8080") @>
        test <@ Flow.runSync capabilities (EnvironmentVariable.get "FSFLOW_CAPS_PORT") = Exit.Success "8080" @>
        test <@ Flow.runSync capabilities (EnvironmentVariable.getInt "FSFLOW_CAPS_PORT") = Exit.Success 8080 @>
        test <@ Flow.runSync capabilities (EnvironmentVariable.getBool "FSFLOW_CAPS_ENABLED") = Exit.Success true @>
        test <@ Flow.runSync capabilities (EnvironmentVariable.getGuid "FSFLOW_CAPS_SESSION") = Exit.Success (global.System.Guid.Parse "22222222-2222-2222-2222-222222222222") @>

    [<Fact>]
    let ``environment variable helpers return typed errors for missing and invalid values`` () =
        let capabilities =
            {
                Clock = Clock.live
                Random = Random.live
                Guid = Guid.live
                EnvVars =
                    EnvironmentVariables.fromPairs
                        [ "FSFLOW_CAPS_PORT_TEXT", "abc"
                          "FSFLOW_CAPS_ENABLED_TEXT", "maybe" ]
            }

        test <@ Flow.runSync capabilities (EnvironmentVariable.get "FSFLOW_CAPS_MISSING") = Exit.Failure(Cause.Fail (EnvironmentVariableError.MissingVariable "FSFLOW_CAPS_MISSING")) @>
        test <@ Flow.runSync capabilities (EnvironmentVariable.getInt "FSFLOW_CAPS_PORT_TEXT") = Exit.Failure(Cause.Fail (EnvironmentVariableError.InvalidVariable("FSFLOW_CAPS_PORT_TEXT", "abc", "an integer"))) @>
        test <@ Flow.runSync capabilities (EnvironmentVariable.getBool "FSFLOW_CAPS_ENABLED_TEXT") = Exit.Failure(Cause.Fail (EnvironmentVariableError.InvalidVariable("FSFLOW_CAPS_ENABLED_TEXT", "maybe", "a boolean"))) @>

    [<Fact>]
    let ``live providers work correctly with real runtime`` () =
        let capabilities =
            {
                Clock = Clock.live
                Random = Random.live
                Guid = Guid.live
                EnvVars = EnvironmentVariables.live
            }

        let timestamp = Flow.runSync capabilities Clock.now |> function Exit.Success t -> t | _ -> failwith "Failed"
        let randomValue = Flow.runSync capabilities (Random.nextInt 0 10) |> function Exit.Success v -> v | _ -> failwith "Failed"
        let generatedGuid = Flow.runSync capabilities Guid.newGuid |> function Exit.Success g -> g | _ -> failwith "Failed"
        
        let envName = $"FSFLOW_CAPS_CORE_{global.System.Guid.NewGuid():N}"
        let previous = Environment.GetEnvironmentVariable envName

        try
            Environment.SetEnvironmentVariable(envName, "live-value")

            test <@ timestamp <= DateTimeOffset.UtcNow.AddMinutes(1.0) @>
            test <@ randomValue >= 0 && randomValue < 10 @>
            test <@ generatedGuid <> global.System.Guid.Empty @>
            test <@ Flow.runSync capabilities (EnvironmentVariable.get envName) = Exit.Success "live-value" @>
        finally
            Environment.SetEnvironmentVariable(envName, previous)
