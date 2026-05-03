open System
open System.Threading
open System.Threading.Tasks
open FsFlow

type User =
    { Id: int
      Name: string }

type AppDb =
    { FindUser: int -> User option }

type RequestEnv =
    { TraceId: Guid
      Prefix: string
      Db: AppDb
      LoadSuffix: ColdTask<string> }

let validateName (name: string) : Result<string, string> =
    Check.notBlank name
    |> Result.mapErrorTo "name is required"

let loadUser : Flow<RequestEnv, string, User> =
    flow {
        let! db = Flow.read _.Db // Flow<RequestEnv, string, AppDb>
        let! user = db.FindUser 42 |> Flow.fromOption "user not found" // Flow<RequestEnv, string, User>
        return user
    }

let renderTrace : AsyncFlow<RequestEnv, string, string> =
    asyncFlow {
        let! env = AsyncFlow.env // AsyncFlow<RequestEnv, string, RequestEnv>
        let! user = loadUser // AsyncFlow<RequestEnv, string, User>
        let! validName = validateName user.Name // AsyncFlow<RequestEnv, string, string>
        return $"{env.Prefix} [{env.TraceId}] {validName}"
    }

let publishResponse : TaskFlow<RequestEnv, string, string> =
    taskFlow {
        let! env = TaskFlow.env // TaskFlow<RequestEnv, string, RequestEnv>
        let! user = loadUser // TaskFlow<RequestEnv, string, User>
        let! suffix = env.LoadSuffix // TaskFlow<RequestEnv, string, string>
        return $"{env.Prefix} [{env.TraceId}] {user.Name}{suffix}"
    }

[<EntryPoint>]
let main _ =
    let environment =
        { TraceId = Guid.Parse "11111111-1111-1111-1111-111111111111"
          Prefix = "Hello"
          Db =
            { FindUser =
                function
                | 42 -> Some { Id = 42; Name = "Ada" }
                | _ -> None }
          LoadSuffix = ColdTask(fun _ -> Task.FromResult "!") }

    let syncResult =
        loadUser
        |> Flow.run environment

    let asyncResult =
        renderTrace
        |> AsyncFlow.run environment
        |> Async.RunSynchronously

    let taskResult =
        publishResponse
        |> TaskFlow.run environment CancellationToken.None
        |> fun task -> task.GetAwaiter().GetResult()

    printfn "Flow result: %A" syncResult
    printfn "AsyncFlow result: %A" asyncResult
    printfn "TaskFlow result: %A" taskResult
    0
