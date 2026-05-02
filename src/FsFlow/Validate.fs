namespace FsFlow

open System

/// <summary>Location markers used to describe where a diagnostic belongs in a validation graph.</summary>
[<RequireQualifiedAccess>]
type PathSegment =
    | Key of string
    | Index of int
    | Name of string

/// <summary>A path through a validation graph.</summary>
type Path = PathSegment list

/// <summary>A single failure item attached to a path in a validation graph.</summary>
type Diagnostic<'error> =
    {
        Path: Path
        Error: 'error
    }

/// <summary>
/// A mergeable validation graph that carries local diagnostics and nested child branches.
/// </summary>
type Diagnostics<'error> =
    {
        Local: Diagnostic<'error> list
        Children: Map<PathSegment, Diagnostics<'error>>
    }

/// <summary>
/// An accumulating validation result that keeps the structured diagnostics graph visible.
/// </summary>
type Validation<'value, 'error> = private Validation of Result<'value, Diagnostics<'error>>

/// <summary>
/// Helpers for building, merging, and flattening validation diagnostics graphs.
/// </summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Diagnostics =
    let empty<'error> : Diagnostics<'error> =
        {
            Local = []
            Children = Map.empty
        }

    let singleton (diagnostic: Diagnostic<'error>) : Diagnostics<'error> =
        {
            Local = [ diagnostic ]
            Children = Map.empty
        }

    let rec merge (left: Diagnostics<'error>) (right: Diagnostics<'error>) : Diagnostics<'error> =
        let addBranch children key branch =
            match Map.tryFind key children with
            | Some existing -> Map.add key (merge existing branch) children
            | None -> Map.add key branch children

        {
            Local = left.Local @ right.Local
            Children = Map.fold addBranch left.Children right.Children
        }

    let flatten (graph: Diagnostics<'error>) : Diagnostic<'error> list =
        let rec flattenWithPrefix (prefix: Path) (node: Diagnostics<'error>) =
            let local =
                node.Local
                |> List.map (fun diagnostic -> { diagnostic with Path = prefix @ diagnostic.Path })

            let children =
                node.Children
                |> Map.toList
                |> List.collect (fun (segment, child) -> flattenWithPrefix (prefix @ [ segment ]) child)

            local @ children

        flattenWithPrefix [] graph

/// <summary>
/// Helpers for accumulating validation results with mergeable diagnostics.
/// </summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Validation =
    let private unwrap (Validation result) = result

    let toResult (validation: Validation<'value, 'error>) : Result<'value, Diagnostics<'error>> =
        unwrap validation

    let succeed (value: 'value) : Validation<'value, 'error> =
        Validation (Ok value)

    let fail (diagnostics: Diagnostics<'error>) : Validation<'value, 'error> =
        Validation (Error diagnostics)

    let fromResult (result: Result<'value, 'error>) : Validation<'value, 'error> =
        match result with
        | Ok value -> succeed value
        | Error error -> fail (Diagnostics.singleton { Path = []; Error = error })

    let map
        (mapper: 'value -> 'next)
        (validation: Validation<'value, 'error>)
        : Validation<'next, 'error> =
        validation |> unwrap |> Result.map mapper |> Validation

    let bind
        (binder: 'value -> Validation<'next, 'error>)
        (validation: Validation<'value, 'error>)
        : Validation<'next, 'error> =
        match unwrap validation with
        | Ok value -> binder value
        | Error diagnostics -> fail diagnostics

    let mapError
        (mapper: 'error -> 'nextError)
        (validation: Validation<'value, 'error>)
        : Validation<'value, 'nextError> =
        let rec mapDiagnostics (graph: Diagnostics<'error>) : Diagnostics<'nextError> =
            {
                Local =
                    graph.Local
                    |> List.map (fun diagnostic ->
                        {
                            Path = diagnostic.Path
                            Error = mapper diagnostic.Error
                        })
                Children =
                    graph.Children
                    |> Map.map (fun _ child -> mapDiagnostics child)
            }

        validation |> unwrap |> Result.mapError mapDiagnostics |> Validation

    let map2
        (mapper: 'left -> 'right -> 'value)
        (left: Validation<'left, 'error>)
        (right: Validation<'right, 'error>)
        : Validation<'value, 'error> =
        Validation(
            match unwrap left, unwrap right with
            | Ok leftValue, Ok rightValue -> Ok(mapper leftValue rightValue)
            | Error leftDiagnostics, Ok _ -> Error leftDiagnostics
            | Ok _, Error rightDiagnostics -> Error rightDiagnostics
            | Error leftDiagnostics, Error rightDiagnostics -> Error(Diagnostics.merge leftDiagnostics rightDiagnostics)
        )

    let apply
        (validation: Validation<'value -> 'next, 'error>)
        (value: Validation<'value, 'error>)
        : Validation<'next, 'error> =
        map2 (fun mapper input -> mapper input) validation value

    let collect (validations: seq<Validation<'value, 'error>>) : Validation<'value list, 'error> =
        let folder
            (state: Validation<'value list, 'error>)
            (validation: Validation<'value, 'error>) =
            map2 (fun values value -> values @ [ value ]) state validation

        Seq.fold folder (succeed []) validations

    let sequence (validations: seq<Validation<'value, 'error>>) : Validation<'value list, 'error> =
        collect validations

    let merge (left: Validation<'value, 'error>) (right: Validation<'next, 'error>) : Validation<'value * 'next, 'error> =
        map2 (fun leftValue rightValue -> leftValue, rightValue) left right

/// <summary>
/// A reusable predicate result that carries a unit failure placeholder until the caller
/// maps it into a domain-specific error.
/// </summary>
type Check<'value> = Result<'value, unit>

/// <summary>
/// Pure predicate helpers that return <see cref="T:System.Result`2" /> values with a unit error,
/// plus the bridge functions that turn those checks into application errors.
/// </summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Check =
    /// <summary>Returns success when the supplied check fails.</summary>
    let not (check: Check<'value>) : Check<unit> =
        match check with
        | Ok _ -> Error ()
        | Error () -> Ok ()

    /// <summary>Returns success when both checks succeed.</summary>
    let ``and`` (left: Check<'left>) (right: Check<'right>) : Check<unit> =
        match left with
        | Error () -> Error ()
        | Ok _ ->
            match right with
            | Ok _ -> Ok ()
            | Error () -> Error ()

    /// <summary>Returns success when either check succeeds.</summary>
    let ``or`` (left: Check<'left>) (right: Check<'right>) : Check<unit> =
        match left with
        | Ok _ -> Ok ()
        | Error () ->
            match right with
            | Ok _ -> Ok ()
            | Error () -> Error ()

    /// <summary>Returns success when every check in the sequence succeeds.</summary>
    let all (checks: seq<Check<'value>>) : Check<unit> =
        use enumerator = checks.GetEnumerator()

        let mutable result = Ok ()
        let mutable continueLoop = true

        while continueLoop && enumerator.MoveNext() do
            match enumerator.Current with
            | Ok _ -> ()
            | Error () ->
                result <- Error ()
                continueLoop <- false

        result

    /// <summary>Returns success when at least one check in the sequence succeeds.</summary>
    let any (checks: seq<Check<'value>>) : Check<unit> =
        use enumerator = checks.GetEnumerator()

        let mutable result = Error ()
        let mutable continueLoop = true

        while continueLoop && enumerator.MoveNext() do
            match enumerator.Current with
            | Ok _ ->
                result <- Ok ()
                continueLoop <- false
            | Error () -> ()

        result

    /// <summary>Returns success when the condition is true.</summary>
    let okIf (cond: bool) : Check<unit> =
        if cond then Ok () else Error ()

    /// <summary>Returns success when the condition is false.</summary>
    let failIf (cond: bool) : Check<unit> =
        if Operators.not cond then Ok () else Error ()

    /// <summary>Returns the value when the option is <c>Some</c>.</summary>
    let okIfSome (opt: 'a option) : Check<'a> =
        match opt with
        | Some value -> Ok value
        | None -> Error ()

    /// <summary>Returns success when the option is <c>None</c>.</summary>
    let okIfNone (opt: 'a option) : Check<unit> =
        match opt with
        | None -> Ok ()
        | Some _ -> Error ()

    /// <summary>Returns success when the option is <c>None</c>.</summary>
    let failIfSome (opt: 'a option) : Check<unit> =
        match opt with
        | Some _ -> Error ()
        | None -> Ok ()

    /// <summary>Returns the value when the option is <c>Some</c>.</summary>
    let failIfNone (opt: 'a option) : Check<'a> =
        match opt with
        | None -> Error ()
        | Some value -> Ok value

    /// <summary>Returns the value when the value option is <c>ValueSome</c>.</summary>
    let okIfValueSome (opt: 'a voption) : Check<'a> =
        match opt with
        | ValueSome value -> Ok value
        | ValueNone -> Error ()

    /// <summary>Returns success when the value option is <c>ValueNone</c>.</summary>
    let okIfValueNone (opt: 'a voption) : Check<unit> =
        match opt with
        | ValueNone -> Ok ()
        | ValueSome _ -> Error ()

    /// <summary>Returns success when the value option is <c>ValueNone</c>.</summary>
    let failIfValueSome (opt: 'a voption) : Check<unit> =
        match opt with
        | ValueSome _ -> Error ()
        | ValueNone -> Ok ()

    /// <summary>Returns the value when the value option is <c>ValueSome</c>.</summary>
    let failIfValueNone (opt: 'a voption) : Check<'a> =
        match opt with
        | ValueNone -> Error ()
        | ValueSome value -> Ok value

    /// <summary>Returns the value when it is not null.</summary>
    let okIfNotNull (value: 'a when 'a : null) : Check<'a> =
        if isNull value then Error () else Ok value

    /// <summary>Returns success when the value is null.</summary>
    let okIfNull (value: 'a when 'a : null) : Check<unit> =
        if isNull value then Ok () else Error ()

    /// <summary>Returns success when the value is null.</summary>
    let failIfNotNull (value: 'a when 'a : null) : Check<unit> =
        if isNull value then Error () else Ok ()

    /// <summary>Returns the value when it is null.</summary>
    let failIfNull (value: 'a when 'a : null) : Check<'a> =
        if isNull value then Ok value else Error ()

    /// <summary>Returns the sequence when it is not empty.</summary>
    let okIfNotEmpty (coll: seq<'a>) : Check<seq<'a>> =
        if Seq.isEmpty coll then Error () else Ok coll

    /// <summary>Returns success when the sequence is empty.</summary>
    let okIfEmpty (coll: seq<'a>) : Check<unit> =
        if Seq.isEmpty coll then Ok () else Error ()

    /// <summary>Returns success when the sequence is empty.</summary>
    let failIfNotEmpty (coll: seq<'a>) : Check<unit> =
        if Seq.isEmpty coll then Ok () else Error ()

    /// <summary>Returns the sequence when it is not empty.</summary>
    let failIfEmpty (coll: seq<'a>) : Check<seq<'a>> =
        if Seq.isEmpty coll then Error () else Ok coll

    /// <summary>Returns success when the values are equal.</summary>
    let okIfEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected = actual then Ok () else Error ()

    /// <summary>Returns success when the values are not equal.</summary>
    let okIfNotEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected <> actual then Ok () else Error ()

    /// <summary>Returns success when the values are equal.</summary>
    let failIfEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected = actual then Error () else Ok ()

    /// <summary>Returns success when the values are not equal.</summary>
    let failIfNotEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected <> actual then Error () else Ok ()

    /// <summary>Returns the string when it is not null or empty.</summary>
    let okIfNonEmptyStr (str: string) : Check<string> =
        if String.IsNullOrEmpty str then Error () else Ok str

    /// <summary>Returns success when the string is null or empty.</summary>
    let okIfEmptyStr (str: string) : Check<unit> =
        if String.IsNullOrEmpty str then Ok () else Error ()

    /// <summary>Returns success when the string is null or empty.</summary>
    let failIfNonEmptyStr (str: string) : Check<unit> =
        if String.IsNullOrEmpty str then Ok () else Error ()

    /// <summary>Returns the string when it is null or empty.</summary>
    let failIfEmptyStr (str: string) : Check<string> =
        if String.IsNullOrEmpty str then Error () else Ok str

    /// <summary>Returns the string when it is not blank.</summary>
    let okIfNotBlank (str: string) : Check<string> =
        if String.IsNullOrWhiteSpace str then Error () else Ok str

    /// <summary>Returns success when the string is blank.</summary>
    let okIfBlank (str: string) : Check<unit> =
        if String.IsNullOrWhiteSpace str then Ok () else Error ()

    /// <summary>Returns success when the string is blank.</summary>
    let failIfNotBlank (str: string) : Check<unit> =
        if String.IsNullOrWhiteSpace str then Ok () else Error ()

    /// <summary>Returns the string when it is blank.</summary>
    let failIfBlank (str: string) : Check<string> =
        if String.IsNullOrWhiteSpace str then Error () else Ok str

    /// <summary>Maps a unit error into the supplied application error value.</summary>
    let orElse (error: 'e) (result: Check<'value>) : Result<'value, 'e> =
        Result.mapError (fun () -> error) result

    /// <summary>Maps a unit error into an application error produced on demand.</summary>
    let orElseWith (errorFn: unit -> 'e) (result: Check<'value>) : Result<'value, 'e> =
        Result.mapError (fun () -> errorFn ()) result

/// <summary>
/// Backward-compatible aliases for the old validation module name.
/// </summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Validate =
    let not = Check.not
    let (``and``) = Check.``and``
    let (``or``) = Check.``or``
    let all = Check.all
    let any = Check.any
    let okIf = Check.okIf
    let failIf = Check.failIf
    let okIfSome = Check.okIfSome
    let okIfNone = Check.okIfNone
    let failIfSome = Check.failIfSome
    let failIfNone = Check.failIfNone
    let okIfValueSome = Check.okIfValueSome
    let okIfValueNone = Check.okIfValueNone
    let failIfValueSome = Check.failIfValueSome
    let failIfValueNone = Check.failIfValueNone
    let okIfNotNull = Check.okIfNotNull
    let okIfNull = Check.okIfNull
    let failIfNotNull = Check.failIfNotNull
    let failIfNull = Check.failIfNull
    let okIfNotEmpty = Check.okIfNotEmpty
    let okIfEmpty = Check.okIfEmpty
    let failIfNotEmpty = Check.failIfNotEmpty
    let failIfEmpty = Check.failIfEmpty
    let okIfEqual = Check.okIfEqual
    let okIfNotEqual = Check.okIfNotEqual
    let failIfEqual = Check.failIfEqual
    let failIfNotEqual = Check.failIfNotEqual
    let okIfNonEmptyStr = Check.okIfNonEmptyStr
    let okIfEmptyStr = Check.okIfEmptyStr
    let failIfNonEmptyStr = Check.failIfNonEmptyStr
    let failIfEmptyStr = Check.failIfEmptyStr
    let okIfNotBlank = Check.okIfNotBlank
    let okIfBlank = Check.okIfBlank
    let failIfNotBlank = Check.failIfNotBlank
    let failIfBlank = Check.failIfBlank
    let orElse = Check.orElse
    let orElseWith = Check.orElseWith

/// <summary>
/// Computation expression builder for accumulating <see cref="T:FsFlow.Validation`2" /> workflows.
/// </summary>
/// <exclude/>
type ValidateBuilder() =
    member _.Return(value: 'value) : Validation<'value, 'error> =
        Validation.succeed value

    member _.ReturnFrom(validation: Validation<'value, 'error>) : Validation<'value, 'error> =
        validation

    member _.ReturnFrom(result: Result<'value, 'error>) : Validation<'value, 'error> =
        Validation.fromResult result

    member _.Zero() : Validation<unit, 'error> =
        Validation.succeed ()

    member _.Bind
        (
            validation: Validation<'value, 'error>,
            binder: 'value -> Validation<'next, 'error>
        ) : Validation<'next, 'error> =
        Validation.bind binder validation

    member _.Bind
        (
            result: Result<'value, 'error>,
            binder: 'value -> Validation<'next, 'error>
        ) : Validation<'next, 'error> =
        result
        |> Validation.fromResult
        |> Validation.bind binder

    member _.Delay(factory: unit -> Validation<'value, 'error>) : Validation<'value, 'error> =
        factory ()

    member _.Run(validation: Validation<'value, 'error>) : Validation<'value, 'error> =
        validation

    member _.Combine
        (
            first: Validation<unit, 'error>,
            second: Validation<'value, 'error>
        ) : Validation<'value, 'error> =
        Validation.bind (fun () -> second) first

    member _.MergeSources
        (
            left: Validation<'left, 'error>,
            right: Validation<'right, 'error>
        ) : Validation<'left * 'right, 'error> =
        Validation.map2 (fun leftValue rightValue -> leftValue, rightValue) left right

    member _.TryWith
        (
            validation: Validation<'value, 'error>,
            handler: exn -> Validation<'value, 'error>
        ) : Validation<'value, 'error> =
        try
            validation
        with error ->
            handler error

    member _.TryFinally(validation: Validation<'value, 'error>, compensation: unit -> unit) : Validation<'value, 'error> =
        try
            validation
        finally
            compensation ()

    member this.Using
        (
            resource: 'resource,
            binder: 'resource -> Validation<'value, 'error>
        ) : Validation<'value, 'error>
        when 'resource :> IDisposable =
        this.TryFinally(
            binder resource,
            fun () ->
                if not (isNull (box resource)) then
                    resource.Dispose()
        )

    member this.While
        (
            guard: unit -> bool,
            body: Validation<unit, 'error>
        ) : Validation<unit, 'error> =
        if guard () then
            this.Bind(body, fun () -> this.While(guard, body))
        else
            this.Zero()

    member this.For
        (
            sequence: seq<'value>,
            binder: 'value -> Validation<unit, 'error>
        ) : Validation<unit, 'error> =
        this.Using(
            sequence.GetEnumerator(),
            fun enumerator -> this.While(enumerator.MoveNext, this.Delay(fun () -> binder enumerator.Current))
        )
