namespace FsFlow.Caps.Context.Examples

open System
open System.Globalization
open System.Threading
open FsFlow
open FsFlow.Caps.Context

module ContextExample =
    type ILogger =
        abstract WriteLine : string -> unit

    type IAuditSink =
        abstract Record : string -> unit

    type IAuthorizer =
        abstract CanAccess : context: RequestContext -> resource: string -> bool

    type RecordingLogger() =
        let lines = ResizeArray<string>()

        member _.Lines = lines |> Seq.toList

        interface ILogger with
            member _.WriteLine(message: string) = lines.Add message

    type RecordingAuditSink() =
        let entries = ResizeArray<string>()

        member _.Entries = entries |> Seq.toList

        interface IAuditSink with
            member _.Record(entry: string) = entries.Add entry

    type RoleAuthorizer(requiredRole: string) =
        interface IAuthorizer with
            member _.CanAccess(context: RequestContext) (_resource: string) =
                match context.CurrentUser with
                | None -> false
                | Some user -> CurrentUser.hasRole requiredRole user && RequestFlags.isEnabled "canWriteAudit" context.Flags

    type AppEnv =
        {
            Context: RequestContext
            Logger: ILogger
            Audit: IAuditSink
            Authorizer: IAuthorizer
        }

    let handleRequest (resource: string) : Flow<AppEnv, string, string> =
        flow {
            let! env = Flow.env
            let context = env.Context
            let requestId = RequestId.get context
            let correlationId = CorrelationId.tryGet context |> Option.defaultValue "none"
            let tenantId = TenantId.tryGet context |> Option.defaultValue "public"
            let locale = Locale.get context
            let userLabel =
                context.CurrentUser
                |> Option.map (fun user -> user.DisplayName |> Option.defaultValue user.UserId)
                |> Option.defaultValue "anonymous"

            if not (env.Authorizer.CanAccess context resource) then
                return! Flow.fail "forbidden"

            let path = RequestMetadata.tryGet "path" context.Metadata |> Option.defaultValue "/"
            env.Logger.WriteLine $"log request={requestId} correlation={correlationId} tenant={tenantId} user={userLabel} locale={locale.Name}"
            env.Audit.Record $"audit request={requestId} resource={resource} path={path}"
            return $"approved:{resource}:{requestId}"
        }

    let run () =
        let user =
            CurrentUser.create
                "user-77"
                (Some "Grace")
                (Some "grace@example.com")
                [ "auditor" ]
                [ "scope", [ "orders.read" ] ]

        let context =
            RequestContext.create
                "req-77"
                (Some "corr-77")
                (Some "tenant-1")
                (Some user)
                (CultureInfo.GetCultureInfo "en-AU")
                [ "path", "/orders/42" ]
                [ "canWriteAudit", true ]

        let logger = RecordingLogger()
        let audit = RecordingAuditSink()
        let authorizer = RoleAuthorizer "auditor"

        let env =
            {
                Context = context
                Logger = logger :> ILogger
                Audit = audit :> IAuditSink
                Authorizer = authorizer :> IAuthorizer
            }

        let outcome =
            handleRequest "orders.read"
            |> Flow.run env

        printfn "result=%A" outcome
        logger.Lines |> List.iter (printfn "%s")
        audit.Entries |> List.iter (printfn "%s")
