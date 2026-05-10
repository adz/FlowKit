namespace FsFlow.Tests

open System
open System.Globalization
open System.Diagnostics
open System.Security.Claims
open System.Threading
open FsFlow.Caps.Context
open FsFlow.Tests.TestSupport
open Swensen.Unquote
open Xunit

module CapsContextTests =
    [<Fact>]
    let ``request context carries request, user, locale, metadata, and flags`` () =
        let user =
            CurrentUser.create
                "user-42"
                (Some "Ada")
                (Some "ada@example.com")
                [ "admin"; "auditor" ]
                [ "scope", [ "orders.read"; "orders.write" ] ]

        let context =
            RequestContext.create
                "req-1"
                (Some "corr-1")
                (Some "tenant-1")
                (Some user)
                (CultureInfo.GetCultureInfo "en-AU")
                [ "path", "/orders/42"
                  "method", "POST" ]
                [ "canWriteAudit", true
                  "isInternal", false ]

        test <@ RequestId.get context = "req-1" @>
        test <@ CorrelationId.tryGet context = Some "corr-1" @>
        test <@ TenantId.tryGet context = Some "tenant-1" @>
        test <@ Locale.get context = CultureInfo.GetCultureInfo "en-AU" @>
        test <@ RequestMetadata.tryGet "path" context.Metadata = Some "/orders/42" @>
        test <@ RequestMetadata.contains "method" context.Metadata @>
        test <@ RequestFlags.tryGet "canWriteAudit" context.Flags = Some true @>
        test <@ RequestFlags.isEnabled "canWriteAudit" context.Flags @>
        test <@ context.CurrentUser = Some user @>
        test <@ CurrentUser.hasRole "admin" user @>
        test <@ CurrentUser.claim "scope" user = Some "orders.read" @>

    [<Fact>]
    let ``live request context reads the current runtime state`` () =
        let claimsIdentity =
            ClaimsIdentity(
                [ Claim(ClaimTypes.NameIdentifier, "user-77")
                  Claim(ClaimTypes.Name, "Grace")
                  Claim(ClaimTypes.Email, "grace@example.com")
                  Claim(ClaimTypes.Role, "auditor") ],
                "demo"
            )

        let principal = ClaimsPrincipal claimsIdentity
        let previousPrincipal = Thread.CurrentPrincipal
        let activity = new Activity("context-test")
        activity.Start() |> ignore

        try
            Thread.CurrentPrincipal <- principal

            let context = RequestContext.live()
            let expectedUser =
                CurrentUser.create
                    "user-77"
                    (Some "Grace")
                    (Some "grace@example.com")
                    [ "auditor" ]
                    [ ClaimTypes.NameIdentifier, [ "user-77" ]
                      ClaimTypes.Name, [ "Grace" ]
                      ClaimTypes.Email, [ "grace@example.com" ] ]

            test <@ context.RequestId <> String.Empty @>
            test <@ context.CorrelationId.IsSome @>
            test <@ context.Culture = CultureInfo.CurrentCulture @>
            test <@ CurrentUser.live() = Some expectedUser @>
        finally
            Thread.CurrentPrincipal <- previousPrincipal
            activity.Stop() |> ignore
