open System
open System.IO
open System.Threading
open FsFlow.Net
open FsFlow.Validate

type ReadmeEnv =
    { Root: string }

type FileReadError =
    | NotFound of path: string

let readTextFile (path: string) : TaskFlow<ReadmeEnv, FileReadError, string> =
    taskFlow {
        // In production, map access and path exceptions separately at the boundary.
        do! okIf (File.Exists path)
            |> orElse (NotFound path)

        return! ColdTask(fun ct -> File.ReadAllTextAsync(path, ct)) // ColdTask<string>
    }

let program : TaskFlow<ReadmeEnv, FileReadError, string * string> =
    taskFlow {
        let! root = TaskFlow.read _.Root // ReadmeEnv.Root -> string
        let settingsFile = Path.Combine(root, "settings.json")
        let featureFlagsFile = Path.Combine(root, "feature-flags.json")

        // The cancellation token is passed implicitly through both file reads.
        let! settings = readTextFile settingsFile // TaskFlow<ReadmeEnv, FileReadError, string>
        let! featureFlags = readTextFile featureFlagsFile // TaskFlow<ReadmeEnv, FileReadError, string>

        return settings, featureFlags // TaskFlow<ReadmeEnv, FileReadError, string * string>
    }

[<EntryPoint>]
let main _ =
    let root =
        Path.Combine(Path.GetTempPath(), "FsFlow.ReadmeExample", Guid.NewGuid().ToString "N")

    Directory.CreateDirectory root |> ignore

    let settingsPath = Path.Combine(root, "settings.json")
    let featureFlagsPath = Path.Combine(root, "feature-flags.json")

    File.WriteAllText(settingsPath, """{"name":"Ada"}""")
    File.WriteAllText(featureFlagsPath, """{"darkMode":true}""")

    let readPairResult =
        program
        |> TaskFlow.run { Root = root } CancellationToken.None
        |> fun task -> task.GetAwaiter().GetResult()

    printfn "Config pair result: %A" readPairResult
    0
