namespace FsFlow

/// <summary>
/// A reusable predicate result that either preserves a value on success or acts as a gate with
/// <c>unit</c> on success, while carrying a unit failure placeholder until the caller maps it into
/// a domain-specific error.
/// </summary>
/// <remarks>
/// Use the <see cref="T:FsFlow.Check" /> module helpers to create and compose checks.
/// </remarks>
type Check<'value> = Result<'value, unit>

/// <summary>
/// Predicate helpers that return <see cref="T:System.Result`2" /> values with a unit error,
/// plus the bridge functions that turn those checks into application errors. Some helpers preserve
/// the source value; others are gates and return <c>unit</c> on success.
/// </summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Check =
    /// <summary>Builds a check from a predicate while preserving the successful value.</summary>
    /// <param name="predicate">A function of type <c>'value -> bool</c> to test the value.</param>
    /// <param name="value">The value of type <c>'value</c> to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the value if the predicate succeeds.</returns>
    let fromPredicate (predicate: 'value -> bool) (value: 'value) : Check<'value> =
        if predicate value then
            Ok value
        else
            Error ()

    /// <summary>Returns success when the supplied check fails.</summary>
    /// <remarks>
    /// This is a logical "not" operation for checks. Note that it discards the success value
    /// and returns <see cref="T:Microsoft.FSharp.Core.Unit" /> on success.
    /// </remarks>
    /// <param name="check">The source <see cref="T:FsFlow.Check`1" /> to invert.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the input fails.</returns>
    let not (check: Check<'value>) : Check<unit> =
        match check with
        | Ok _ -> Error ()
        | Error () -> Ok ()

    /// <summary>Returns success when both checks succeed.</summary>
    /// <remarks>
    /// This is a logical "and" operation. It short-circuits: if <paramref name="left" /> fails,
    /// <paramref name="right" /> is not evaluated.
    /// </remarks>
    /// <param name="left">The first check.</param>
    /// <param name="right">The second check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds only if both inputs succeed.</returns>
    let ``and`` (left: Check<'left>) (right: Check<'right>) : Check<unit> =
        match left with
        | Error () -> Error ()
        | Ok _ ->
            match right with
            | Ok _ -> Ok ()
            | Error () -> Error ()

    /// <summary>Returns success when either check succeeds.</summary>
    /// <remarks>
    /// This is a logical "or" operation. It short-circuits: if <paramref name="left" /> succeeds,
    /// <paramref name="right" /> is not evaluated.
    /// </remarks>
    /// <param name="left">The first check.</param>
    /// <param name="right">The second check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if either input succeeds.</returns>
    let ``or`` (left: Check<'left>) (right: Check<'right>) : Check<unit> =
        match left with
        | Ok _ -> Ok ()
        | Error () ->
            match right with
            | Ok _ -> Ok ()
            | Error () -> Error ()

    /// <summary>Returns success when every check in the sequence succeeds.</summary>
    /// <remarks>
    /// Sequentially evaluates each check in the <paramref name="checks" /> sequence.
    /// Stops at the first failure.
    /// </remarks>
    /// <param name="checks">A sequence of checks.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds only if all inputs succeed.</returns>
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
    /// <remarks>
    /// Sequentially evaluates each check in the <paramref name="checks" /> sequence.
    /// Stops at the first success.
    /// </remarks>
    /// <param name="checks">A sequence of checks.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if any input succeeds.</returns>
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
    /// <param name="cond">The boolean condition to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if <paramref name="cond" /> is true.</returns>
    let okIf (cond: bool) : Check<unit> =
        if cond then Ok () else Error ()

    /// <summary>Returns success when the condition is false.</summary>
    /// <param name="cond">The boolean condition to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if <paramref name="cond" /> is false.</returns>
    let failIf (cond: bool) : Check<unit> =
        if Microsoft.FSharp.Core.Operators.not cond then Ok () else Error ()

    /// <summary>Returns the value when the option is <c>Some</c>.</summary>
    /// <param name="opt">The <see cref="T:Microsoft.FSharp.Core.FSharpOption`1" /> to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the value if present.</returns>
    let okIfSome (opt: 'a option) : Check<'a> =
        match opt with
        | Some value -> Ok value
        | None -> Error ()

    /// <summary>Returns success when the option is <c>None</c>.</summary>
    /// <param name="opt">The option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the option is <see cref="T:Microsoft.FSharp.Core.FSharpOption`1.None" />.</returns>
    let okIfNone (opt: 'a option) : Check<unit> =
        match opt with
        | None -> Ok ()
        | Some _ -> Error ()

    /// <summary>Returns success when the option is <c>None</c>.</summary>
    /// <param name="opt">The option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the option is <see cref="T:Microsoft.FSharp.Core.FSharpOption`1.None" />.</returns>
    let failIfSome (opt: 'a option) : Check<unit> =
        match opt with
        | Some _ -> Error ()
        | None -> Ok ()

    /// <summary>Returns the value when the option is <c>Some</c>.</summary>
    /// <param name="opt">The option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the value if present.</returns>
    let failIfNone (opt: 'a option) : Check<'a> =
        match opt with
        | None -> Error ()
        | Some value -> Ok value

    /// <summary>Returns the value when the value option is <c>ValueSome</c>.</summary>
    /// <param name="opt">The <see cref="T:Microsoft.FSharp.Core.FSharpValueOption`1" /> to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the value if present.</returns>
    let okIfValueSome (opt: 'a voption) : Check<'a> =
        match opt with
        | ValueSome value -> Ok value
        | ValueNone -> Error ()

    /// <summary>Returns success when the value option is <c>ValueNone</c>.</summary>
    /// <param name="opt">The value option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the value option is <see cref="T:Microsoft.FSharp.Core.FSharpValueOption`1.ValueNone" />.</returns>
    let okIfValueNone (opt: 'a voption) : Check<unit> =
        match opt with
        | ValueNone -> Ok ()
        | ValueSome _ -> Error ()

    /// <summary>Returns success when the value option is <c>ValueNone</c>.</summary>
    /// <param name="opt">The value option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the value option is <see cref="T:Microsoft.FSharp.Core.FSharpValueOption`1.ValueNone" />.</returns>
    let failIfValueSome (opt: 'a voption) : Check<unit> =
        match opt with
        | ValueSome _ -> Error ()
        | ValueNone -> Ok ()

    /// <summary>Returns the value when the value option is <c>ValueSome</c>.</summary>
    /// <param name="opt">The value option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the value if present.</returns>
    let failIfValueNone (opt: 'a voption) : Check<'a> =
        match opt with
        | ValueNone -> Error ()
        | ValueSome value -> Ok value

    /// <summary>Returns the value when it is not null.</summary>
    /// <param name="value">The value of type <c>'a</c> to check for null.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-null value.</returns>
    let okIfNotNull (value: 'a when 'a : null) : Check<'a> =
        if isNull value then Error () else Ok value

    /// <summary>Returns success when the value is null.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the value is null.</returns>
    let okIfNull (value: 'a when 'a : null) : Check<unit> =
        if isNull value then Ok () else Error ()

    /// <summary>Returns success when the value is null.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the value is null.</returns>
    let failIfNotNull (value: 'a when 'a : null) : Check<unit> =
        if isNull value then Error () else Ok ()

    /// <summary>Returns the value when it is null.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the null value.</returns>
    let failIfNull (value: 'a when 'a : null) : Check<'a> =
        if isNull value then Ok value else Error ()

    /// <summary>Returns the sequence when it is not empty.</summary>
    /// <param name="coll">The sequence of type <c>seq&lt;'a&gt;</c> to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-empty sequence.</returns>
    let okIfNotEmpty (coll: seq<'a>) : Check<seq<'a>> =
        if Seq.isEmpty coll then Error () else Ok coll

    /// <summary>Returns success when the sequence is empty.</summary>
    /// <param name="coll">The sequence to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the sequence is empty.</returns>
    let okIfEmpty (coll: seq<'a>) : Check<unit> =
        if Seq.isEmpty coll then Ok () else Error ()

    /// <summary>Returns success when the sequence is empty.</summary>
    /// <param name="coll">The sequence to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the sequence is empty.</returns>
    let failIfNotEmpty (coll: seq<'a>) : Check<unit> =
        if Seq.isEmpty coll then Ok () else Error ()

    /// <summary>Returns the sequence when it is not empty.</summary>
    /// <param name="coll">The sequence to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-empty sequence.</returns>
    let failIfEmpty (coll: seq<'a>) : Check<seq<'a>> =
        if Seq.isEmpty coll then Error () else Ok coll

    /// <summary>Returns success when the values are equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values are equal.</returns>
    let okIfEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected = actual then Ok () else Error ()

    /// <summary>Returns success when the values are not equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values differ.</returns>
    let okIfNotEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected <> actual then Ok () else Error ()

    /// <summary>Returns success when the values are equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values are equal.</returns>
    let failIfEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected = actual then Error () else Ok ()

    /// <summary>Returns success when the values are not equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values differ.</returns>
    let failIfNotEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected <> actual then Error () else Ok ()

    /// <summary>Returns the string when it is not null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-empty string.</returns>
    let okIfNonEmptyStr (str: string) : Check<string> =
        if System.String.IsNullOrEmpty str then Error () else Ok str

    /// <summary>Returns success when the string is null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is null or empty.</returns>
    let okIfEmptyStr (str: string) : Check<unit> =
        if System.String.IsNullOrEmpty str then Ok () else Error ()

    /// <summary>Returns success when the string is null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is null or empty.</returns>
    let failIfNonEmptyStr (str: string) : Check<unit> =
        if System.String.IsNullOrEmpty str then Ok () else Error ()

    /// <summary>Returns the string when it is null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the empty or null string.</returns>
    let failIfEmptyStr (str: string) : Check<string> =
        if System.String.IsNullOrEmpty str then Error () else Ok str

    /// <summary>Returns the string when it is not blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-blank string.</returns>
    let okIfNotBlank (str: string) : Check<string> =
        if System.String.IsNullOrWhiteSpace str then Error () else Ok str

    /// <summary>Returns the string when it is not blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-blank string.</returns>
    let notBlank (str: string) : Check<string> =
        fromPredicate (fun value -> Microsoft.FSharp.Core.Operators.not (System.String.IsNullOrWhiteSpace value)) str

    /// <summary>Returns success when the string is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is null, empty, or whitespace.</returns>
    let okIfBlank (str: string) : Check<unit> =
        if System.String.IsNullOrWhiteSpace str then Ok () else Error ()

    /// <summary>Returns success when the string is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is blank.</returns>
    let blank (str: string) : Check<unit> =
        okIfBlank str

    /// <summary>Returns success when the string is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is blank.</returns>
    let failIfNotBlank (str: string) : Check<unit> =
        if System.String.IsNullOrWhiteSpace str then Ok () else Error ()

    /// <summary>Returns the string when it is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the blank string.</returns>
    let failIfBlank (str: string) : Check<string> =
        if System.String.IsNullOrWhiteSpace str then Error () else Ok str

    /// <summary>Returns the value when it is not null.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-null value.</returns>
    let notNull (value: 'a when 'a : null) : Check<'a> =
        fromPredicate (fun inner -> Microsoft.FSharp.Core.Operators.not (isNull inner)) value

    /// <summary>Returns the sequence when it is not empty.</summary>
    /// <param name="coll">The sequence to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-empty sequence.</returns>
    let notEmpty (coll: seq<'a>) : Check<seq<'a>> =
        fromPredicate (fun inner -> Microsoft.FSharp.Core.Operators.not (Seq.isEmpty inner)) coll

    /// <summary>Returns success when the values are equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values are equal.</returns>
    let equal (expected: 'a) (actual: 'a) : Check<unit> =
        okIfEqual expected actual

    /// <summary>Returns success when the values are not equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values differ.</returns>
    let notEqual (expected: 'a) (actual: 'a) : Check<unit> =
        okIfNotEqual expected actual

    /// <summary>Maps a unit error into the supplied application error value.</summary>
    /// <remarks>
    /// This is the primary bridge from checks to domain-specific results.
    /// </remarks>
    /// <param name="error">The domain error of type <c>'error</c> to return on failure.</param>
    /// <param name="result">The source <see cref="T:FsFlow.Check`1" />.</param>
    /// <returns>A <see cref="T:System.Result`2" /> with the provided error value.</returns>
    let orError (error: 'error) (result: Check<'value>) : Result<'value, 'error> =
        match result with
        | Ok value -> Ok value
        | Error () -> Error error

    /// <summary>Maps a unit error into an application error produced on demand.</summary>
    /// <param name="errorFn">A function of type <c>unit -> 'error</c> to produce the error.</param>
    /// <param name="result">The source <see cref="T:FsFlow.Check`1" />.</param>
    /// <returns>A <see cref="T:System.Result`2" /> with the produced error value.</returns>
    let orErrorWith (errorFn: unit -> 'error) (result: Check<'value>) : Result<'value, 'error> =
        match result with
        | Ok value -> Ok value
        | Error () -> Error(errorFn ())
