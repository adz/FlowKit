namespace FsFlow.Caps.Context

open System
open System.Collections.Generic
open System.Globalization
open System.Diagnostics
open System.Security.Claims

/// <summary>Represents the authenticated user attached to the current request.</summary>
type UserContext =
    {
        /// <summary>The stable user identifier used for authorization and auditing.</summary>
        UserId: string

        /// <summary>An optional display name for logs and human-readable output.</summary>
        DisplayName: string option

        /// <summary>An optional email address for user-facing notifications.</summary>
        Email: string option

        /// <summary>The roles attached to the current user.</summary>
        Roles: string list

        /// <summary>All non-role claims grouped by claim type.</summary>
        Claims: Map<string, string list>
    }

/// <summary>Represents the request-scoped execution context carried through FsFlow boundaries.</summary>
type RequestContext =
    {
        /// <summary>The request identifier for the current execution.</summary>
        RequestId: string

        /// <summary>An optional correlation identifier shared across related requests.</summary>
        CorrelationId: string option

        /// <summary>An optional tenant identifier for multi-tenant app code.</summary>
        TenantId: string option

        /// <summary>The current user attached to the request, when one exists.</summary>
        CurrentUser: UserContext option

        /// <summary>The locale and culture for the current execution.</summary>
        Culture: CultureInfo

        /// <summary>Request metadata, such as route values or inbound headers.</summary>
        Metadata: Map<string, string>

        /// <summary>Request-scoped feature flags or execution switches.</summary>
        Flags: Map<string, bool>
    }

/// <summary>Helpers for reading and shaping the current user context.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module CurrentUser =
    let private tryFindValue (principal: ClaimsPrincipal) (claimType: string) : string option =
        principal.Claims
        |> Seq.tryFind (fun claim -> claim.Type = claimType)
        |> Option.map (fun claim -> claim.Value)

    let private groupClaims (principal: ClaimsPrincipal) : Map<string, string list> =
        principal.Claims
        |> Seq.filter (fun claim -> claim.Type <> ClaimTypes.Role)
        |> Seq.groupBy (fun claim -> claim.Type)
        |> Seq.map (fun (claimType, claims) -> claimType, claims |> Seq.map (fun claim -> claim.Value) |> Seq.toList)
        |> Map.ofSeq

    /// <summary>Creates a current-user record from explicit values.</summary>
    /// <example>
    /// <code>
    /// let user =
    ///     CurrentUser.create
    ///         "user-42"
    ///         (Some "Ada")
    ///         (Some "ada@example.com")
    ///         [ "admin" ]
    ///         [ "scope", [ "orders.read" ] ]
    /// </code>
    /// </example>
    let create
        (userId: string)
        (displayName: string option)
        (email: string option)
        (roles: seq<string>)
        (claims: seq<string * string list>)
        : UserContext =
        {
            UserId = userId
            DisplayName = displayName
            Email = email
            Roles = roles |> Seq.distinct |> Seq.toList
            Claims = claims |> Map.ofSeq
        }

    /// <summary>Creates a current-user record from a claims principal when the identity is authenticated.</summary>
    /// <example>
    /// <code>
    /// let principal = ClaimsPrincipal(ClaimsIdentity([ Claim(ClaimTypes.NameIdentifier, "user-42") ], "demo"))
    /// let user = CurrentUser.fromClaimsPrincipal principal
    /// </code>
    /// </example>
    let fromClaimsPrincipal (principal: ClaimsPrincipal) : UserContext option =
        if isNull principal || isNull principal.Identity || not principal.Identity.IsAuthenticated then
            None
        else
            let userId =
                tryFindValue principal ClaimTypes.NameIdentifier
                |> Option.orElseWith (fun () -> tryFindValue principal "sub")

            match userId with
            | None -> None
            | Some userId ->
                let roles =
                    principal.Claims
                    |> Seq.filter (fun claim -> claim.Type = ClaimTypes.Role)
                    |> Seq.map (fun claim -> claim.Value)
                    |> Seq.distinct
                    |> Seq.toList

                Some(
                    create
                        userId
                        (tryFindValue principal ClaimTypes.Name)
                        (tryFindValue principal ClaimTypes.Email)
                        roles
                        (groupClaims principal |> Map.toSeq)
                )

    /// <summary>Reads the current claims principal from the runtime if one is available.</summary>
    /// <example>
    /// <code>
    /// let maybeUser = CurrentUser.live()
    /// </code>
    /// </example>
    let live () : UserContext option =
        ClaimsPrincipal.Current |> fromClaimsPrincipal

    /// <summary>Checks whether the current user has a role.</summary>
    /// <example>
    /// <code>
    /// let user = CurrentUser.create "user-42" None None [ "admin" ] []
    /// let allowed = CurrentUser.hasRole "admin" user
    /// </code>
    /// </example>
    let hasRole (role: string) (user: UserContext) : bool =
        user.Roles |> List.exists ((=) role)

    /// <summary>Reads a claim value from the current user.</summary>
    /// <example>
    /// <code>
    /// let user = CurrentUser.create "user-42" None None [] [ "scope", [ "orders.read" ] ]
    /// let scope = CurrentUser.claim "scope" user
    /// </code>
    /// </example>
    let claim (claimType: string) (user: UserContext) : string option =
        user.Claims
        |> Map.tryFind claimType
        |> Option.bind List.tryHead

/// <summary>Helpers for building and reshaping request contexts.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module RequestContext =
    let private currentRequestId () =
        match Activity.Current with
        | null -> Guid.NewGuid().ToString("N")
        | activity when String.IsNullOrWhiteSpace activity.Id -> activity.TraceId.ToString()
        | activity -> activity.Id

    let private currentCorrelationId () =
        match Activity.Current with
        | null -> None
        | activity -> Some(activity.TraceId.ToString())

    /// <summary>Creates a request context from explicit values.</summary>
    /// <example>
    /// <code>
    /// let context =
    ///     RequestContext.create
    ///         "req-1"
    ///         (Some "corr-1")
    ///         (Some "tenant-1")
    ///         None
    ///         CultureInfo.InvariantCulture
    ///         [ "path", "/orders/42" ]
    ///         [ "canWriteAudit", true ]
    /// </code>
    /// </example>
    let create
        (requestId: string)
        (correlationId: string option)
        (tenantId: string option)
        (currentUser: UserContext option)
        (culture: CultureInfo)
        (metadata: seq<string * string>)
        (flags: seq<string * bool>)
        : RequestContext =
        {
            RequestId = requestId
            CorrelationId = correlationId
            TenantId = tenantId
            CurrentUser = currentUser
            Culture = culture
            Metadata = metadata |> Map.ofSeq
            Flags = flags |> Map.ofSeq
        }

    /// <summary>Creates a live request context from the current runtime state.</summary>
    /// <example>
    /// <code>
    /// let context = RequestContext.live()
    /// </code>
    /// </example>
    let live () : RequestContext =
        create
            (currentRequestId ())
            (currentCorrelationId ())
            None
            (CurrentUser.live ())
            CultureInfo.CurrentCulture
            []
            []

    /// <summary>Replaces the request identifier in a request context.</summary>
    /// <example>
    /// <code>
    /// let next = RequestContext.withRequestId "req-2" context
    /// </code>
    /// </example>
    let withRequestId (requestId: string) (context: RequestContext) : RequestContext =
        { context with RequestId = requestId }

    /// <summary>Replaces the correlation identifier in a request context.</summary>
    /// <example>
    /// <code>
    /// let next = RequestContext.withCorrelationId (Some "corr-2") context
    /// </code>
    /// </example>
    let withCorrelationId (correlationId: string option) (context: RequestContext) : RequestContext =
        { context with CorrelationId = correlationId }

    /// <summary>Replaces the tenant identifier in a request context.</summary>
    /// <example>
    /// <code>
    /// let next = RequestContext.withTenantId (Some "tenant-2") context
    /// </code>
    /// </example>
    let withTenantId (tenantId: string option) (context: RequestContext) : RequestContext =
        { context with TenantId = tenantId }

    /// <summary>Replaces the current user in a request context.</summary>
    /// <example>
    /// <code>
    /// let next = RequestContext.withCurrentUser (Some user) context
    /// </code>
    /// </example>
    let withCurrentUser (currentUser: UserContext option) (context: RequestContext) : RequestContext =
        { context with CurrentUser = currentUser }

    /// <summary>Replaces the culture in a request context.</summary>
    /// <example>
    /// <code>
    /// let next = RequestContext.withCulture CultureInfo.InvariantCulture context
    /// </code>
    /// </example>
    let withCulture (culture: CultureInfo) (context: RequestContext) : RequestContext =
        { context with Culture = culture }

    /// <summary>Replaces the request metadata in a request context.</summary>
    /// <example>
    /// <code>
    /// let next = RequestContext.withMetadata (Map.ofList [ "path", "/orders/42" ]) context
    /// </code>
    /// </example>
    let withMetadata (metadata: Map<string, string>) (context: RequestContext) : RequestContext =
        { context with Metadata = metadata }

    /// <summary>Replaces the request-scoped flags in a request context.</summary>
    /// <example>
    /// <code>
    /// let next = RequestContext.withFlags (Map.ofList [ "canWriteAudit", true ]) context
    /// </code>
    /// </example>
    let withFlags (flags: Map<string, bool>) (context: RequestContext) : RequestContext =
        { context with Flags = flags }

/// <summary>Helpers for the request identifier.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module RequestId =
    /// <summary>Reads the request identifier from a request context.</summary>
    /// <example>
    /// <code>
    /// let requestId = RequestId.get context
    /// </code>
    /// </example>
    let get (context: RequestContext) : string = context.RequestId

    /// <summary>Returns a live request identifier from the current runtime state.</summary>
    /// <example>
    /// <code>
    /// let requestId = RequestId.live()
    /// </code>
    /// </example>
    let live () : string = RequestContext.live().RequestId

/// <summary>Helpers for the correlation identifier.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module CorrelationId =
    /// <summary>Reads the optional correlation identifier from a request context.</summary>
    /// <example>
    /// <code>
    /// let correlationId = CorrelationId.tryGet context
    /// </code>
    /// </example>
    let tryGet (context: RequestContext) : string option = context.CorrelationId

    /// <summary>Returns a live optional correlation identifier from the current runtime state.</summary>
    /// <example>
    /// <code>
    /// let correlationId = CorrelationId.live()
    /// </code>
    /// </example>
    let live () : string option = RequestContext.live().CorrelationId

/// <summary>Helpers for the tenant identifier.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module TenantId =
    /// <summary>Reads the optional tenant identifier from a request context.</summary>
    /// <example>
    /// <code>
    /// let tenantId = TenantId.tryGet context
    /// </code>
    /// </example>
    let tryGet (context: RequestContext) : string option = context.TenantId

    /// <summary>Returns a live optional tenant identifier from the current runtime state.</summary>
    /// <example>
    /// <code>
    /// let tenantId = TenantId.live()
    /// </code>
    /// </example>
    let live () : string option = RequestContext.live().TenantId

/// <summary>Helpers for the current culture and locale.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Locale =
    /// <summary>Reads the culture from a request context.</summary>
    /// <example>
    /// <code>
    /// let culture = Locale.get context
    /// </code>
    /// </example>
    let get (context: RequestContext) : CultureInfo = context.Culture

    /// <summary>Returns the current culture from the runtime state.</summary>
    /// <example>
    /// <code>
    /// let culture = Locale.live()
    /// </code>
    /// </example>
    let live () : CultureInfo = RequestContext.live().Culture

/// <summary>Helpers for request metadata.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module RequestMetadata =
    /// <summary>Creates an empty metadata map.</summary>
    /// <example>
    /// <code>
    /// let metadata = RequestMetadata.empty
    /// </code>
    /// </example>
    let empty : Map<string, string> = Map.empty

    /// <summary>Creates a metadata map from name/value pairs.</summary>
    /// <example>
    /// <code>
    /// let metadata = RequestMetadata.fromPairs [ "path", "/orders/42" ]
    /// </code>
    /// </example>
    let fromPairs (pairs: seq<string * string>) : Map<string, string> =
        pairs |> Map.ofSeq

    /// <summary>Reads a metadata value by key if it exists.</summary>
    /// <example>
    /// <code>
    /// let path = RequestMetadata.tryGet "path" context.Metadata
    /// </code>
    /// </example>
    let tryGet (key: string) (metadata: Map<string, string>) : string option =
        Map.tryFind key metadata

    /// <summary>Checks whether the metadata contains a key.</summary>
    /// <example>
    /// <code>
    /// let hasPath = RequestMetadata.contains "path" context.Metadata
    /// </code>
    /// </example>
    let contains (key: string) (metadata: Map<string, string>) : bool =
        Map.containsKey key metadata

/// <summary>Helpers for request-scoped feature flags.</summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module RequestFlags =
    /// <summary>Creates an empty flag map.</summary>
    /// <example>
    /// <code>
    /// let flags = RequestFlags.empty
    /// </code>
    /// </example>
    let empty : Map<string, bool> = Map.empty

    /// <summary>Creates a flag map from name/value pairs.</summary>
    /// <example>
    /// <code>
    /// let flags = RequestFlags.fromPairs [ "canWriteAudit", true ]
    /// </code>
    /// </example>
    let fromPairs (pairs: seq<string * bool>) : Map<string, bool> =
        pairs |> Map.ofSeq

    /// <summary>Reads a flag value by key if it exists.</summary>
    /// <example>
    /// <code>
    /// let flag = RequestFlags.tryGet "canWriteAudit" context.Flags
    /// </code>
    /// </example>
    let tryGet (key: string) (flags: Map<string, bool>) : bool option =
        Map.tryFind key flags

    /// <summary>Checks whether the supplied request-scoped flag is enabled.</summary>
    /// <example>
    /// <code>
    /// let enabled = RequestFlags.isEnabled "canWriteAudit" context.Flags
    /// </code>
    /// </example>
    let isEnabled (key: string) (flags: Map<string, bool>) : bool =
        Map.tryFind key flags |> Option.defaultValue false
