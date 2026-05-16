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
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the value if the predicate succeeds; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// 5 |> Check.fromPredicate (fun x -> x &gt; 0) // Ok 5
    /// -1 |> Check.fromPredicate (fun x -> x &gt; 0) // Error ()
    /// </code>
    /// </example>
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
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the input fails; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// Check.okIf true |> Check.not // Error ()
    /// Check.okIf false |> Check.not // Ok ()
    /// </code>
    /// </example>
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
    /// <example>
    /// <code>
    /// Check.and (Check.okIf true) (Check.okIf true) // Ok ()
    /// Check.and (Check.okIf true) (Check.okIf false) // Error ()
    /// </code>
    /// </example>
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
    /// <example>
    /// <code>
    /// Check.or (Check.okIf true) (Check.okIf false) // Ok ()
    /// Check.or (Check.okIf false) (Check.okIf false) // Error ()
    /// </code>
    /// </example>
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
    /// <example>
    /// <code>
    /// [ Check.okIf true; Check.okIf true ] |> Check.all // Ok ()
    /// [ Check.okIf true; Check.okIf false ] |> Check.all // Error ()
    /// </code>
    /// </example>
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
    /// <example>
    /// <code>
    /// [ Check.okIf false; Check.okIf true ] |> Check.any // Ok ()
    /// [ Check.okIf false; Check.okIf false ] |> Check.any // Error ()
    /// </code>
    /// </example>
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
    /// <example>
    /// <code>
    /// Check.okIf (1 = 1) // Ok ()
    /// Check.okIf (1 = 2) // Error ()
    /// </code>
    /// </example>
    let okIf (cond: bool) : Check<unit> =
        if cond then Ok () else Error ()

    /// <summary>Returns success when the condition is false.</summary>
    /// <param name="cond">The boolean condition to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if <paramref name="cond" /> is false.</returns>
    /// <example>
    /// <code>
    /// Check.failIf (1 = 2) // Ok ()
    /// Check.failIf (1 = 1) // Error ()
    /// </code>
    /// </example>
    let failIf (cond: bool) : Check<unit> =
        if Microsoft.FSharp.Core.Operators.not cond then Ok () else Error ()

    /// <summary>Returns the value when the option is <c>Some</c>.</summary>
    /// <param name="opt">The <see cref="T:Microsoft.FSharp.Core.FSharpOption`1" /> to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the value if present; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// Some 5 |> Check.okIfSome // Ok 5
    /// None |> Check.okIfSome // Error ()
    /// </code>
    /// </example>
    let okIfSome (opt: 'a option) : Check<'a> =
        match opt with
        | Some value -> Ok value
        | None -> Error ()

    /// <summary>Returns success when the option is <c>None</c>.</summary>
    /// <param name="opt">The option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the option is <see cref="T:Microsoft.FSharp.Core.FSharpOption`1.None" />.</returns>
    /// <example>
    /// <code>
    /// None |> Check.okIfNone // Ok ()
    /// Some 5 |> Check.okIfNone // Error ()
    /// </code>
    /// </example>
    let okIfNone (opt: 'a option) : Check<unit> =
        match opt with
        | None -> Ok ()
        | Some _ -> Error ()

    /// <summary>Returns success when the option is <c>None</c>.</summary>
    /// <param name="opt">The option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the option is <see cref="T:Microsoft.FSharp.Core.FSharpOption`1.None" />.</returns>
    /// <example>
    /// <code>
    /// None |> Check.failIfSome // Ok ()
    /// Some 5 |> Check.failIfSome // Error ()
    /// </code>
    /// </example>
    let failIfSome (opt: 'a option) : Check<unit> =
        match opt with
        | Some _ -> Error ()
        | None -> Ok ()

    /// <summary>Returns the value when the option is <c>Some</c>.</summary>
    /// <param name="opt">The option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the value if present; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// Some 5 |> Check.failIfNone // Ok 5
    /// None |> Check.failIfNone // Error ()
    /// </code>
    /// </example>
    let failIfNone (opt: 'a option) : Check<'a> =
        match opt with
        | None -> Error ()
        | Some value -> Ok value

    /// <summary>Returns the value when the value option is <c>ValueSome</c>.</summary>
    /// <param name="opt">The <see cref="T:Microsoft.FSharp.Core.FSharpValueOption`1" /> to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the value if present; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// ValueSome 5 |> Check.okIfValueSome // Ok 5
    /// ValueNone |> Check.okIfValueSome // Error ()
    /// </code>
    /// </example>
    let okIfValueSome (opt: 'a voption) : Check<'a> =
        match opt with
        | ValueSome value -> Ok value
        | ValueNone -> Error ()

    /// <summary>Returns success when the value option is <c>ValueNone</c>.</summary>
    /// <param name="opt">The value option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the value option is <see cref="T:Microsoft.FSharp.Core.FSharpValueOption`1.ValueNone" />.</returns>
    /// <example>
    /// <code>
    /// ValueNone |> Check.okIfValueNone // Ok ()
    /// ValueSome 5 |> Check.okIfValueNone // Error ()
    /// </code>
    /// </example>
    let okIfValueNone (opt: 'a voption) : Check<unit> =
        match opt with
        | ValueNone -> Ok ()
        | ValueSome _ -> Error ()

    /// <summary>Returns success when the value option is <c>ValueNone</c>.</summary>
    /// <param name="opt">The value option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the value option is <see cref="T:Microsoft.FSharp.Core.FSharpValueOption`1.ValueNone" />.</returns>
    /// <example>
    /// <code>
    /// ValueNone |> Check.failIfValueSome // Ok ()
    /// ValueSome 5 |> Check.failIfValueSome // Error ()
    /// </code>
    /// </example>
    let failIfValueSome (opt: 'a voption) : Check<unit> =
        match opt with
        | ValueSome _ -> Error ()
        | ValueNone -> Ok ()

    /// <summary>Returns the value when the value option is <c>ValueSome</c>.</summary>
    /// <param name="opt">The value option to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the value if present; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// ValueSome 5 |> Check.failIfValueNone // Ok 5
    /// ValueNone |> Check.failIfValueNone // Error ()
    /// </code>
    /// </example>
    let failIfValueNone (opt: 'a voption) : Check<'a> =
        match opt with
        | ValueNone -> Error ()
        | ValueSome value -> Ok value

    /// <summary>Returns the value when it is not null.</summary>
    /// <param name="value">The value of type <c>'a</c> to check for null.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-null value; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "hello" |> Check.okIfNotNull // Ok "hello"
    /// null |> Check.okIfNotNull // Error ()
    /// </code>
    /// </example>
    let okIfNotNull (value: 'a when 'a : null) : Check<'a> =
        if isNull value then Error () else Ok value

    /// <summary>Returns success when the value is null.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the value is null; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// null |> Check.okIfNull // Ok ()
    /// "hello" |> Check.okIfNull // Error ()
    /// </code>
    /// </example>
    let okIfNull (value: 'a when 'a : null) : Check<unit> =
        if isNull value then Ok () else Error ()

    /// <summary>Returns success when the value is null.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the value is null; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// null |> Check.failIfNotNull // Ok ()
    /// "hello" |> Check.failIfNotNull // Error ()
    /// </code>
    /// </example>
    let failIfNotNull (value: 'a when 'a : null) : Check<unit> =
        if isNull value then Error () else Ok ()

    /// <summary>Returns the value when it is null.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the null value; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// null |> Check.failIfNull // Ok null
    /// "hello" |> Check.failIfNull // Error ()
    /// </code>
    /// </example>
    let failIfNull (value: 'a when 'a : null) : Check<'a> =
        if isNull value then Ok value else Error ()

    /// <summary>Returns the sequence when it is not empty.</summary>
    /// <param name="coll">The sequence of type <c>seq&lt;'a&gt;</c> to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-empty sequence; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// [1; 2] |> Check.okIfNotEmpty // Ok [1; 2]
    /// [] |> Check.okIfNotEmpty // Error ()
    /// </code>
    /// </example>
    let okIfNotEmpty (coll: seq<'a>) : Check<seq<'a>> =
        if Seq.isEmpty coll then Error () else Ok coll

    /// <summary>Returns success when the sequence is empty.</summary>
    /// <param name="coll">The sequence to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the sequence is empty; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// [] |> Check.okIfEmpty // Ok ()
    /// [1] |> Check.okIfEmpty // Error ()
    /// </code>
    /// </example>
    let okIfEmpty (coll: seq<'a>) : Check<unit> =
        if Seq.isEmpty coll then Ok () else Error ()

    /// <summary>Returns success when the sequence is empty.</summary>
    /// <param name="coll">The sequence to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the sequence is empty; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// [] |> Check.failIfNotEmpty // Ok ()
    /// [1] |> Check.failIfNotEmpty // Error ()
    /// </code>
    /// </example>
    let failIfNotEmpty (coll: seq<'a>) : Check<unit> =
        if Seq.isEmpty coll then Ok () else Error ()

    /// <summary>Returns the sequence when it is not empty.</summary>
    /// <param name="coll">The sequence to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-empty sequence; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// [1] |> Check.failIfEmpty // Ok [1]
    /// [] |> Check.failIfEmpty // Error ()
    /// </code>
    /// </example>
    let failIfEmpty (coll: seq<'a>) : Check<seq<'a>> =
        if Seq.isEmpty coll then Error () else Ok coll

    /// <summary>Returns success when the values are equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values are equal; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// Check.okIfEqual 5 5 // Ok ()
    /// Check.okIfEqual 5 6 // Error ()
    /// </code>
    /// </example>
    let okIfEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected = actual then Ok () else Error ()

    /// <summary>Returns success when the values are not equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values differ; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// Check.okIfNotEqual 5 6 // Ok ()
    /// Check.okIfNotEqual 5 5 // Error ()
    /// </code>
    /// </example>
    let okIfNotEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected <> actual then Ok () else Error ()

    /// <summary>Returns success when the values are equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values are equal; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// Check.failIfEqual 5 6 // Ok ()
    /// Check.failIfEqual 5 5 // Error ()
    /// </code>
    /// </example>
    let failIfEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected = actual then Error () else Ok ()

    /// <summary>Returns success when the values are not equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values differ; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// Check.failIfNotEqual 5 5 // Ok ()
    /// Check.failIfNotEqual 5 6 // Error ()
    /// </code>
    /// </example>
    let failIfNotEqual (expected: 'a) (actual: 'a) : Check<unit> =
        if expected <> actual then Error () else Ok ()

    /// <summary>Returns the string when it is not null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-empty string; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "hello" |> Check.okIfNonEmptyStr // Ok "hello"
    /// "" |> Check.okIfNonEmptyStr // Error ()
    /// </code>
    /// </example>
    let okIfNonEmptyStr (str: string) : Check<string> =
        if System.String.IsNullOrEmpty str then Error () else Ok str

    /// <summary>Returns success when the string is null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is null or empty; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "" |> Check.okIfEmptyStr // Ok ()
    /// "hello" |> Check.okIfEmptyStr // Error ()
    /// </code>
    /// </example>
    let okIfEmptyStr (str: string) : Check<unit> =
        if System.String.IsNullOrEmpty str then Ok () else Error ()

    /// <summary>Returns success when the string is null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is null or empty; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "" |> Check.failIfNonEmptyStr // Ok ()
    /// "hello" |> Check.failIfNonEmptyStr // Error ()
    /// </code>
    /// </example>
    let failIfNonEmptyStr (str: string) : Check<unit> =
        if System.String.IsNullOrEmpty str then Ok () else Error ()

    /// <summary>Returns the string when it is null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the empty or null string; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "hello" |> Check.failIfEmptyStr // Ok "hello"
    /// "" |> Check.failIfEmptyStr // Error ()
    /// </code>
    /// </example>
    let failIfEmptyStr (str: string) : Check<string> =
        if System.String.IsNullOrEmpty str then Error () else Ok str

    /// <summary>Returns the string when it is not blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-blank string; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "hello" |> Check.okIfNotBlank // Ok "hello"
    /// "  " |> Check.okIfNotBlank // Error ()
    /// </code>
    /// </example>
    let okIfNotBlank (str: string) : Check<string> =
        if System.String.IsNullOrWhiteSpace str then Error () else Ok str

    /// <summary>Returns the string when it is not blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-blank string; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "hello" |> Check.notBlank // Ok "hello"
    /// "  " |> Check.notBlank // Error ()
    /// </code>
    /// </example>
    let notBlank (str: string) : Check<string> =
        fromPredicate (fun value -> Microsoft.FSharp.Core.Operators.not (System.String.IsNullOrWhiteSpace value)) str

    /// <summary>Returns success when the string is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is null, empty, or whitespace; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "  " |> Check.okIfBlank // Ok ()
    /// "hello" |> Check.okIfBlank // Error ()
    /// </code>
    /// </example>
    let okIfBlank (str: string) : Check<unit> =
        if System.String.IsNullOrWhiteSpace str then Ok () else Error ()

    /// <summary>Returns success when the string is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is blank; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "  " |> Check.blank // Ok ()
    /// "hello" |> Check.blank // Error ()
    /// </code>
    /// </example>
    let blank (str: string) : Check<unit> =
        okIfBlank str

    /// <summary>Returns success when the string is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is blank; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "  " |> Check.failIfNotBlank // Ok ()
    /// "hello" |> Check.failIfNotBlank // Error ()
    /// </code>
    /// </example>
    let failIfNotBlank (str: string) : Check<unit> =
        if System.String.IsNullOrWhiteSpace str then Ok () else Error ()

    /// <summary>Returns the string when it is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the blank string; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "  " |> Check.failIfBlank // Ok "  "
    /// "hello" |> Check.failIfBlank // Error ()
    /// </code>
    /// </example>
    let failIfBlank (str: string) : Check<string> =
        if System.String.IsNullOrWhiteSpace str then Error () else Ok str

    /// <summary>Returns the value when it is not null.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-null value; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// "hello" |> Check.notNull // Ok "hello"
    /// null |> Check.notNull // Error ()
    /// </code>
    /// </example>
    let notNull (value: 'a when 'a : null) : Check<'a> =
        fromPredicate (fun inner -> Microsoft.FSharp.Core.Operators.not (isNull inner)) value

    /// <summary>Returns the sequence when it is not empty.</summary>
    /// <param name="coll">The sequence to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-empty sequence; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// [1] |> Check.notEmpty // Ok [1]
    /// [] |> Check.notEmpty // Error ()
    /// </code>
    /// </example>
    let notEmpty (coll: seq<'a>) : Check<seq<'a>> =
        fromPredicate (fun inner -> Microsoft.FSharp.Core.Operators.not (Seq.isEmpty inner)) coll

    /// <summary>Returns success when the values are equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values are equal; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// Check.equal 5 5 // Ok ()
    /// Check.equal 5 6 // Error ()
    /// </code>
    /// </example>
    let equal (expected: 'a) (actual: 'a) : Check<unit> =
        okIfEqual expected actual

    /// <summary>Returns success when the values are not equal.</summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the values differ; otherwise, an Error with unit.</returns>
    /// <example>
    /// <code>
    /// Check.notEqual 5 6 // Ok ()
    /// Check.notEqual 5 5 // Error ()
    /// </code>
    /// </example>
    let notEqual (expected: 'a) (actual: 'a) : Check<unit> =
        okIfNotEqual expected actual

    /// <summary>Maps a unit error into the supplied application error value.</summary>
    /// <remarks>
    /// This is the primary bridge from checks to domain-specific results.
    /// </remarks>
    /// <param name="error">The domain error of type <c>'error</c> to return on failure.</param>
    /// <param name="result">The source <see cref="T:FsFlow.Check`1" />.</param>
    /// <returns>A <see cref="T:System.Result`2" /> with the provided error value.</returns>
    /// <example>
    /// <code>
    /// "" |> Check.okIfNonEmptyStr |> Check.orError "Empty string" // Error "Empty string"
    /// "hello" |> Check.okIfNonEmptyStr |> Check.orError "Empty string" // Ok "hello"
    /// </code>
    /// </example>
    let orError (error: 'error) (result: Check<'value>) : Result<'value, 'error> =
        match result with
        | Ok value -> Ok value
        | Error () -> Error error

    /// <summary>Maps a unit error into an application error produced on demand.</summary>
    /// <param name="errorFn">A function of type <c>unit -> 'error</c> to produce the error.</param>
    /// <param name="result">The source <see cref="T:FsFlow.Check`1" />.</param>
    /// <returns>A <see cref="T:System.Result`2" /> with the produced error value.</returns>
    /// <example>
    /// <code>
    /// "" |> Check.okIfNonEmptyStr |> Check.orErrorWith (fun () -> "Empty string") // Error "Empty string"
    /// </code>
    /// </example>
    let orErrorWith (errorFn: unit -> 'error) (result: Check<'value>) : Result<'value, 'error> =
        match result with
        | Ok value -> Ok value
        | Error () -> Error(errorFn ())
