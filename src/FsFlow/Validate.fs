namespace FsFlow

open System

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
