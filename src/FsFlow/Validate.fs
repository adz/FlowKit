namespace FsFlow

open System

/// <summary>Location markers used to describe where a diagnostic belongs in a validation graph.</summary>
[<RequireQualifiedAccess>]
type PathSegment =
    /// <summary>A string-based key, usually for map or record fields.</summary>
    | Key of string
    /// <summary>A zero-based integer index, usually for lists or arrays.</summary>
    | Index of int
    /// <summary>A descriptive name for a property or field.</summary>
    | Name of string

/// <summary>A path through a validation graph, represented as a list of <see cref="T:FsFlow.PathSegment" />.</summary>
type Path = PathSegment list

/// <summary>A single failure item attached to a path in a validation graph.</summary>
type Diagnostic<'error> =
    {
        /// <summary>The path to the source of the error.</summary>
        Path: Path
        /// <summary>The application-specific error value of type <c>'error</c>.</summary>
        Error: 'error
    }

/// <summary>
/// A mergeable validation graph that carries local errors and nested child branches.
/// </summary>
/// <remarks>
/// <para>
/// <c>Errors</c> holds the application errors attached exactly to the current node, while
/// <c>Children</c> holds nested branches keyed by <see cref="T:FsFlow.PathSegment" />.
/// </para>
/// <para>
/// This structure allows hierarchical validation to stay navigable before flattening.
/// Use <see cref="T:FsFlow.Diagnostics.flatten" /> to convert it into a linear list.
/// </para>
/// </remarks>
type Diagnostics<'error> =
    {
        /// <summary>Errors that occurred exactly at this node in the graph.</summary>
        Errors: 'error list
        /// <summary>Nested diagnostic branches, keyed by <see cref="T:FsFlow.PathSegment" />.</summary>
        Children: Map<PathSegment, Diagnostics<'error>>
    }

/// <summary>
/// An accumulating validation result that keeps the structured diagnostics graph visible.
/// </summary>
/// <remarks>
/// Unlike <see cref="T:Microsoft.FSharp.Core.FSharpResult`2" />, this type is designed for applicative
/// composition using <c>and!</c> in the <c>validate { }</c> builder, which merges errors instead of
/// short-circuiting.
/// </remarks>
type Validation<'value, 'error> = private Validation of Result<'value, Diagnostics<'error>>

/// <summary>
/// Helpers for building, merging, and flattening validation diagnostics graphs.
/// </summary>
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Diagnostics =
    /// <summary>Creates an empty diagnostics graph with no errors.</summary>
    /// <returns>An empty <see cref="T:FsFlow.Diagnostics`1" />.</returns>
    let empty<'error> : Diagnostics<'error> =
        {
            Errors = []
            Children = Map.empty
        }

    /// <summary>Creates a diagnostics graph containing exactly one error at the root.</summary>
    /// <param name="error">The application error to store at the root.</param>
    /// <returns>A <see cref="T:FsFlow.Diagnostics`1" /> with a single error.</returns>
    let singleton (error: 'error) : Diagnostics<'error> =
        {
            Errors = [ error ]
            Children = Map.empty
        }

    /// <summary>Recursively merges two diagnostics graphs, combining shared branches and local errors.</summary>
    /// <remarks>
    /// This is the core operation for applicative validation. It ensures that errors from sibling
    /// fields are collected together into a single structured graph.
    /// </remarks>
    /// <param name="left">The first graph of type <see cref="T:FsFlow.Diagnostics`1" />.</param>
    /// <param name="right">The second graph of type <see cref="T:FsFlow.Diagnostics`1" />.</param>
    /// <returns>A new <see cref="T:FsFlow.Diagnostics`1" /> containing the union of both inputs.</returns>
    let rec merge (left: Diagnostics<'error>) (right: Diagnostics<'error>) : Diagnostics<'error> =
        let addBranch children key branch =
            match Map.tryFind key children with
            | Some existing -> Map.add key (merge existing branch) children
            | None -> Map.add key branch children

        {
            Errors = left.Errors @ right.Errors
            Children = Map.fold addBranch left.Children right.Children
        }

    /// <summary>Renders a diagnostics graph in a YAML-like layout for display.</summary>
    /// <remarks>
    /// This is intended for human-readable output. Empty sections are omitted, and children are
    /// shown directly under their branch labels at the same indentation level as errors. Errors
    /// render as YAML-style bullet items without an `Errors:` key. Use
    /// <see cref="T:FsFlow.Diagnostics.flatten" /> when you need path-bearing diagnostics for
    /// reporting or assertions.
    /// </remarks>
    /// <param name="graph">The diagnostics graph to render.</param>
    /// <returns>A formatted string representation of the graph.</returns>
    let toString (graph: Diagnostics<'error>) : string =
        let indent level = String.replicate (level * 2) " "

        let segmentText = function
            | PathSegment.Key key -> key
            | PathSegment.Index index -> $"[{index}]"
            | PathSegment.Name name -> name

        let rec renderNode level (node: Diagnostics<'error>) =
            let lines = ResizeArray<string>()

            match node.Errors with
            | [] -> ()
            | errors ->
                errors
                |> List.iter (fun error -> lines.Add($"{indent level}- {string error}"))

            match node.Children |> Map.toList with
            | [] -> ()
            | children ->
                children
                |> List.iter (fun (segment, child) ->
                    lines.Add($"{indent level}{segmentText segment}:")
                    lines.Add(renderNode (level + 1) child))

            if lines.Count = 0 then
                $"{indent level}[]"
            else
                String.concat Environment.NewLine lines

        renderNode 0 graph

    /// <summary>Flattens the structured diagnostics graph into a linear list of diagnostics.</summary>
    /// <remarks>
    /// During flattening, child paths are accumulated from the root down into each emitted diagnostic.
    /// The tree itself stores only local errors and child branches, while <see cref="T:FsFlow.Diagnostic`1" />
    /// is reserved for reporting output.
    /// </remarks>
    /// <param name="graph">The <see cref="T:FsFlow.Diagnostics`1" /> to flatten.</param>
    /// <returns>A list of type <see cref="T:FsFlow.Diagnostic`1" /> list.</returns>
    let flatten (graph: Diagnostics<'error>) : Diagnostic<'error> list =
        let rec flattenWithPrefix (prefix: Path) (node: Diagnostics<'error>) =
            let local =
                node.Errors
                |> List.map (fun error -> { Path = prefix; Error = error })

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

    /// <summary>Converts a <see cref="T:FsFlow.Validation`2" /> into a standard <see cref="T:System.Result`2" />.</summary>
    /// <param name="validation">The validation to convert.</param>
    /// <returns>A result containing either the success value or the full diagnostics graph.</returns>
    let toResult (validation: Validation<'value, 'error>) : Result<'value, Diagnostics<'error>> =
        unwrap validation

    /// <summary>Creates a successful validation result.</summary>
    /// <param name="value">The success value of type <c>'value</c>.</param>
    /// <returns>A successful <see cref="T:FsFlow.Validation`2" />.</returns>
    let ok (value: 'value) : Validation<'value, 'error> =
        Validation (Ok value)

    /// <summary>Alias for <see cref="ok" />.</summary>
    /// <param name="value">The success value of type <c>'value</c>.</param>
    /// <returns>A successful <see cref="T:FsFlow.Validation`2" />.</returns>
    let succeed (value: 'value) : Validation<'value, 'error> =
        ok value

    /// <summary>Creates a failing validation result with the provided diagnostics.</summary>
    /// <param name="diagnostics">The <see cref="T:FsFlow.Diagnostics`1" /> graph.</param>
    /// <returns>A failing <see cref="T:FsFlow.Validation`2" />.</returns>
    let error (diagnostics: Diagnostics<'error>) : Validation<'value, 'error> =
        Validation (Error diagnostics)

    /// <summary>Alias for <see cref="error" />.</summary>
    /// <param name="diagnostics">The <see cref="T:FsFlow.Diagnostics`1" /> graph.</param>
    /// <returns>A failing <see cref="T:FsFlow.Validation`2" />.</returns>
    let fail (diagnostics: Diagnostics<'error>) : Validation<'value, 'error> =
        error diagnostics

    /// <summary>Lifts a standard <see cref="T:System.Result`2" /> into the <see cref="T:FsFlow.Validation`2" /> context.</summary>
    /// <remarks>
    /// If the result is an error, it is wrapped in a root-level <see cref="T:FsFlow.Diagnostics`1" /> graph.
    /// </remarks>
    /// <param name="result">The result to lift.</param>
    /// <returns>A <see cref="T:FsFlow.Validation`2" /> mirroring the result.</returns>
    let fromResult (result: Result<'value, 'error>) : Validation<'value, 'error> =
        match result with
        | Ok value -> ok value
        | Error failure -> error (Diagnostics.singleton failure)

    /// <summary>Maps the successful value of a validation.</summary>
    /// <param name="mapper">A function of type <c>'value -> 'next</c>.</param>
    /// <param name="validation">The source <see cref="T:FsFlow.Validation`2" />.</param>
    /// <returns>A validation with the transformed success value.</returns>
    let map
        (mapper: 'value -> 'next)
        (validation: Validation<'value, 'error>)
        : Validation<'next, 'error> =
        validation |> unwrap |> Result.map mapper |> Validation

    /// <summary>Sequences a validation-producing continuation.</summary>
    /// <remarks>
    /// This is the monadic "bind" for validation. Note that this operation short-circuits
    /// and does not accumulate errors from the binder if the source has already failed.
    /// For accumulation, use <see cref="map2" /> or the applicative <c>and!</c> syntax.
    /// </remarks>
    /// <param name="binder">A function of type <c>'value -> Validation&lt;'next, 'error&gt;</c>.</param>
    /// <param name="validation">The source validation.</param>
    /// <returns>The result of the binder or the original diagnostics.</returns>
    let bind
        (binder: 'value -> Validation<'next, 'error>)
        (validation: Validation<'value, 'error>)
        : Validation<'next, 'error> =
        match unwrap validation with
        | Ok value -> binder value
        | Error diagnostics -> error diagnostics

    /// <summary>Maps the error type of a validation graph.</summary>
    /// <param name="mapper">A function of type <c>'error -> 'nextError</c>.</param>
    /// <param name="validation">The source <see cref="T:FsFlow.Validation`2" />.</param>
    /// <returns>A validation with transformed error values.</returns>
    let mapError
        (mapper: 'error -> 'nextError)
        (validation: Validation<'value, 'error>)
        : Validation<'value, 'nextError> =
        let rec mapDiagnostics (graph: Diagnostics<'error>) : Diagnostics<'nextError> =
            {
                Errors =
                    graph.Errors
                    |> List.map mapper
                Children =
                    graph.Children
                    |> Map.map (fun _ child -> mapDiagnostics child)
            }

        validation |> unwrap |> Result.mapError mapDiagnostics |> Validation

    /// <summary>Combines two validations, accumulating errors if both fail.</summary>
    /// <remarks>
    /// This is the core applicative operation. If both <paramref name="left" /> and 
    /// <paramref name="right" /> fail, their diagnostics graphs are merged.
    /// </remarks>
    /// <param name="mapper">A function of type <c>'left -> 'right -> 'value</c>.</param>
    /// <param name="left">The first validation.</param>
    /// <param name="right">The second validation.</param>
    /// <returns>A validation with the combined result.</returns>
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

    /// <summary>Applies a validation-wrapped function to a validation-wrapped value.</summary>
    /// <param name="validation">The validation containing the function.</param>
    /// <param name="value">The validation containing the value.</param>
    /// <returns>The result of applying the function to the value, with accumulated errors.</returns>
    let apply
        (validation: Validation<'value -> 'next, 'error>)
        (value: Validation<'value, 'error>)
        : Validation<'next, 'error> =
        map2 (fun mapper input -> mapper input) validation value

    /// <summary>Maps a successful validation value to <c>unit</c> while preserving the diagnostics.</summary>
    /// <param name="validation">The source validation.</param>
    /// <returns>A validation that keeps the original diagnostics and discards the success value.</returns>
    let ignore (validation: Validation<'value, 'error>) : Validation<unit, 'error> =
        map (fun _ -> ()) validation

    /// <summary>Combines three validations, accumulating errors when any input fails.</summary>
    /// <param name="mapper">A function of type <c>'left -> 'middle -> 'right -> 'value</c>.</param>
    /// <param name="left">The first validation.</param>
    /// <param name="middle">The second validation.</param>
    /// <param name="right">The third validation.</param>
    /// <returns>A validation with the combined result.</returns>
    let map3
        (mapper: 'left -> 'middle -> 'right -> 'value)
        (left: Validation<'left, 'error>)
        (middle: Validation<'middle, 'error>)
        (right: Validation<'right, 'error>)
        : Validation<'value, 'error> =
        apply
            (map2 (fun leftValue middleValue -> fun rightValue -> mapper leftValue middleValue rightValue) left middle)
            right

    /// <summary>Falls back to another validation when the source validation fails.</summary>
    /// <remarks>
    /// This is a left-biased choice operator. If the source succeeds, the fallback is not used.
    /// If the source fails, the fallback validation is returned as-is.
    /// </remarks>
    /// <param name="fallback">The validation to use when the source fails.</param>
    /// <param name="validation">The source validation.</param>
    /// <returns>The source validation when it succeeds, otherwise the fallback validation.</returns>
    let orElse
        (fallback: Validation<'value, 'error>)
        (validation: Validation<'value, 'error>)
        : Validation<'value, 'error> =
        match unwrap validation with
        | Ok value -> ok value
        | Error _ -> fallback

    /// <summary>Computes a fallback validation from the source diagnostics when validation fails.</summary>
    /// <remarks>
    /// This is the lazy counterpart to <see cref="orElse" /> and is useful when the alternate
    /// branch depends on the accumulated diagnostics.
    /// </remarks>
    /// <param name="fallback">A function that turns the diagnostics into an alternate validation.</param>
    /// <param name="validation">The source validation.</param>
    /// <returns>The source validation when it succeeds, otherwise the computed fallback validation.</returns>
    let orElseWith
        (fallback: Diagnostics<'error> -> Validation<'value, 'error>)
        (validation: Validation<'value, 'error>)
        : Validation<'value, 'error> =
        match unwrap validation with
        | Ok value -> ok value
        | Error diagnostics -> fallback diagnostics

    /// <summary>Maps the successful value of a validation.</summary>
    /// <param name="mapper">A function of type <c>'value -> 'next</c>.</param>
    /// <param name="validation">The source <see cref="T:FsFlow.Validation`2" />.</param>
    /// <returns>A validation with the transformed success value.</returns>
    let inline (<!>) (mapper: 'value -> 'next) (validation: Validation<'value, 'error>) : Validation<'next, 'error> =
        map mapper validation

    /// <summary>Applies a validation-wrapped function to a validation-wrapped value.</summary>
    /// <param name="validation">The validation containing the function.</param>
    /// <param name="value">The validation containing the value.</param>
    /// <returns>The result of applying the function to the value, with accumulated errors.</returns>
    let inline (<*>) (validation: Validation<'value -> 'next, 'error>) (value: Validation<'value, 'error>) : Validation<'next, 'error> =
        apply validation value

    /// <summary>Collects a sequence of validations into a single validation of a list.</summary>
    /// <remarks>
    /// This operation is applicative: it will collect errors from ALL items in the sequence.
    /// </remarks>
    /// <param name="validations">A sequence of type <c>seq&lt;Validation&lt;'value, 'error&gt;&gt;</c>.</param>
    /// <returns>A validation containing the list of values or accumulated diagnostics.</returns>
    let collect (validations: seq<Validation<'value, 'error>>) : Validation<'value list, 'error> =
        let folder
            (state: Validation<'value list, 'error>)
            (validation: Validation<'value, 'error>) =
            map2 (fun values value -> values @ [ value ]) state validation

        Seq.fold folder (ok []) validations

    /// <summary>Transforms a sequence of validations into a validation of a list.</summary>
    /// <param name="validations">The input sequence.</param>
    /// <returns>A validation containing the list of values.</returns>
    let sequence (validations: seq<Validation<'value, 'error>>) : Validation<'value list, 'error> =
        collect validations

    /// <summary>Merges two validations into a validation of a tuple.</summary>
    /// <param name="left">The first validation.</param>
    /// <param name="right">The second validation.</param>
    /// <returns>A validation containing a tuple of the results.</returns>
    let merge (left: Validation<'value, 'error>) (right: Validation<'next, 'error>) : Validation<'value * 'next, 'error> =
        map2 (fun leftValue rightValue -> leftValue, rightValue) left right

    /// <summary>Scopes a validation under the supplied path segments.</summary>
    /// <param name="path">The path segments to apply to the validation.</param>
    /// <param name="validation">The validation to scope.</param>
    /// <returns>A validation nested under the given path.</returns>
    let at (path: PathSegment list) (validation: Validation<'value, 'error>) : Validation<'value, 'error> =
        let rec attach path graph =
            match path with
            | [] -> graph
            | segment :: rest ->
                {
                    Errors = []
                    Children = Map.add segment (attach rest graph) Map.empty
                }

        validation |> unwrap |> Result.mapError (attach path) |> Validation

    /// <summary>Prefixes a validation with a keyed branch.</summary>
    /// <param name="key">The branch key.</param>
    /// <param name="validation">The validation to scope.</param>
    /// <returns>A validation whose diagnostics are prefixed with <c>Key key</c>.</returns>
    let key (key: string) (validation: Validation<'value, 'error>) : Validation<'value, 'error> =
        at [ PathSegment.Key key ] validation

    /// <summary>Prefixes a validation with an indexed branch.</summary>
    /// <param name="index">The branch index.</param>
    /// <param name="validation">The validation to scope.</param>
    /// <returns>A validation whose diagnostics are prefixed with <c>Index index</c>.</returns>
    let index (index: int) (validation: Validation<'value, 'error>) : Validation<'value, 'error> =
        at [ PathSegment.Index index ] validation

    /// <summary>Prefixes a validation with a named branch.</summary>
    /// <param name="name">The branch name.</param>
    /// <param name="validation">The validation to scope.</param>
    /// <returns>A validation whose diagnostics are prefixed with <c>Name name</c>.</returns>
    let name (name: string) (validation: Validation<'value, 'error>) : Validation<'value, 'error> =
        at [ PathSegment.Name name ] validation

    /// <summary>Maps a sequence into validations while prefixing each item with its index.</summary>
    /// <remarks>
    /// This is the indexed version of <see cref="sequence" />. It is useful for list and array
    /// validation because each item can keep its own <see cref="T:FsFlow.PathSegment.Index" />
    /// branch without the caller manually wrapping every item.
    /// </remarks>
    /// <param name="binder">A function of type <c>int -> 'source -> Validation&lt;'value, 'error&gt;</c>.</param>
    /// <param name="values">The input sequence.</param>
    /// <returns>A validation containing the list of values or accumulated diagnostics.</returns>
    let traverseIndexed
        (binder: int -> 'source -> Validation<'value, 'error>)
        (values: seq<'source>)
        : Validation<'value list, 'error> =
        values
        |> Seq.mapi (fun i value -> binder i value |> index i)
        |> collect

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
[<RequireQualifiedAccess>]
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
        if Operators.not cond then Ok () else Error ()

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
        if String.IsNullOrEmpty str then Error () else Ok str

    /// <summary>Returns success when the string is null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is null or empty.</returns>
    let okIfEmptyStr (str: string) : Check<unit> =
        if String.IsNullOrEmpty str then Ok () else Error ()

    /// <summary>Returns success when the string is null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is null or empty.</returns>
    let failIfNonEmptyStr (str: string) : Check<unit> =
        if String.IsNullOrEmpty str then Ok () else Error ()

    /// <summary>Returns the string when it is null or empty.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the empty or null string.</returns>
    let failIfEmptyStr (str: string) : Check<string> =
        if String.IsNullOrEmpty str then Error () else Ok str

    /// <summary>Returns the string when it is not blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-blank string.</returns>
    let okIfNotBlank (str: string) : Check<string> =
        if String.IsNullOrWhiteSpace str then Error () else Ok str

    /// <summary>Returns the string when it is not blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-blank string.</returns>
    let notBlank (str: string) : Check<string> =
        fromPredicate (fun value -> Operators.not (String.IsNullOrWhiteSpace value)) str

    /// <summary>Returns success when the string is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is null, empty, or whitespace.</returns>
    let okIfBlank (str: string) : Check<unit> =
        if String.IsNullOrWhiteSpace str then Ok () else Error ()

    /// <summary>Returns success when the string is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is blank.</returns>
    let blank (str: string) : Check<unit> =
        okIfBlank str

    /// <summary>Returns success when the string is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> that succeeds if the string is blank.</returns>
    let failIfNotBlank (str: string) : Check<unit> =
        if String.IsNullOrWhiteSpace str then Ok () else Error ()

    /// <summary>Returns the string when it is blank.</summary>
    /// <param name="str">The string to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the blank string.</returns>
    let failIfBlank (str: string) : Check<string> =
        if String.IsNullOrWhiteSpace str then Error () else Ok str

    /// <summary>Returns the value when it is not null.</summary>
    /// <param name="value">The value to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-null value.</returns>
    let notNull (value: 'a when 'a : null) : Check<'a> =
        fromPredicate (fun inner -> Operators.not (isNull inner)) value

    /// <summary>Returns the sequence when it is not empty.</summary>
    /// <param name="coll">The sequence to check.</param>
    /// <returns>A <see cref="T:FsFlow.Check`1" /> containing the non-empty sequence.</returns>
    let notEmpty (coll: seq<'a>) : Check<seq<'a>> =
        fromPredicate (fun inner -> Operators.not (Seq.isEmpty inner)) coll

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

/// <summary>
/// Computation expression builder for a validation block scoped to a path segment or path prefix.
/// </summary>
/// <exclude/>
type ValidationScopeBuilder(scopePath: PathSegment list) =
    member _.Return(value: 'value) : Validation<'value, 'error> =
        Validation.ok value

    member _.ReturnFrom(validation: Validation<'value, 'error>) : Validation<'value, 'error> =
        validation

    member _.ReturnFrom(result: Result<'value, 'error>) : Validation<'value, 'error> =
        Validation.fromResult result

    member _.Zero() : Validation<unit, 'error> =
        Validation.ok ()

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
        Validation.at scopePath validation

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

    /// <summary>Scopes a nested validation block under the supplied path prefix.</summary>
    /// <param name="path">The path prefix to append.</param>
    /// <returns>A scoped validation builder.</returns>
    member _.at(path: PathSegment list) = ValidationScopeBuilder(scopePath @ path)

    /// <summary>Scopes a nested validation block under a keyed branch.</summary>
    /// <param name="key">The branch key.</param>
    /// <returns>A scoped validation builder.</returns>
    member this.key(key: string) = this.at [ PathSegment.Key key ]

    /// <summary>Scopes a nested validation block under an indexed branch.</summary>
    /// <param name="index">The branch index.</param>
    /// <returns>A scoped validation builder.</returns>
    member this.index(index: int) = this.at [ PathSegment.Index index ]

    /// <summary>Scopes a nested validation block under a named branch.</summary>
    /// <param name="name">The branch name.</param>
    /// <returns>A scoped validation builder.</returns>
    member this.name(name: string) = this.at [ PathSegment.Name name ]

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

/// <summary>
/// Computation expression builder for fail-fast <see cref="T:System.Result`2" /> workflows.
/// </summary>
/// <exclude/>
type ResultBuilder() =
    member _.Return(value: 'value) : Result<'value, 'error> =
        Ok value

    member _.ReturnFrom(result: Result<'value, 'error>) : Result<'value, 'error> =
        result

    member _.Zero() : Result<unit, 'error> =
        Ok ()

    member _.Bind
        (
            result: Result<'value, 'error>,
            binder: 'value -> Result<'next, 'error>
        ) : Result<'next, 'error> =
        Result.bind binder result

    member _.Delay(factory: unit -> Result<'value, 'error>) : Result<'value, 'error> =
        factory ()

    member _.Run(result: Result<'value, 'error>) : Result<'value, 'error> =
        result

    member _.Combine
        (
            first: Result<unit, 'error>,
            second: Result<'value, 'error>
        ) : Result<'value, 'error> =
        Result.bind (fun () -> second) first

    member _.TryWith
        (
            result: Result<'value, 'error>,
            handler: exn -> Result<'value, 'error>
        ) : Result<'value, 'error> =
        try
            result
        with error ->
            handler error

    member _.TryFinally(result: Result<'value, 'error>, compensation: unit -> unit) : Result<'value, 'error> =
        try
            result
        finally
            compensation ()

    member this.Using
        (
            resource: 'resource,
            binder: 'resource -> Result<'value, 'error>
        ) : Result<'value, 'error>
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
            body: Result<unit, 'error>
        ) : Result<unit, 'error> =
        if guard () then
            this.Bind(body, fun () -> this.While(guard, body))
        else
            this.Zero()

    member this.For
        (
            sequence: seq<'value>,
            binder: 'value -> Result<unit, 'error>
        ) : Result<unit, 'error> =
        this.Using(
            sequence.GetEnumerator(),
            fun enumerator -> this.While(enumerator.MoveNext, this.Delay(fun () -> binder enumerator.Current))
        )

/// <summary>
/// Computation expression builder for accumulating <see cref="T:FsFlow.Validation`2" /> workflows.
/// </summary>
/// <exclude/>
type ValidateBuilder() =
    member _.Return(value: 'value) : Validation<'value, 'error> =
        Validation.ok value

    member _.ReturnFrom(validation: Validation<'value, 'error>) : Validation<'value, 'error> =
        validation

    member _.ReturnFrom(result: Result<'value, 'error>) : Validation<'value, 'error> =
        Validation.fromResult result

    member _.Zero() : Validation<unit, 'error> =
        Validation.ok ()

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

    /// <summary>Scopes a validation block under the supplied path prefix.</summary>
    /// <param name="path">The path prefix to apply to the block.</param>
    /// <returns>A scoped validation builder.</returns>
    member _.at(path: PathSegment list) = ValidationScopeBuilder(path)

    /// <summary>Scopes a validation block under a keyed branch.</summary>
    /// <param name="key">The branch key.</param>
    /// <returns>A scoped validation builder.</returns>
    member this.key(key: string) = this.at [ PathSegment.Key key ]

    /// <summary>Scopes a validation block under an indexed branch.</summary>
    /// <param name="index">The branch index.</param>
    /// <returns>A scoped validation builder.</returns>
    member this.index(index: int) = this.at [ PathSegment.Index index ]

    /// <summary>Scopes a validation block under a named branch.</summary>
    /// <param name="name">The branch name.</param>
    /// <returns>A scoped validation builder.</returns>
    member this.name(name: string) = this.at [ PathSegment.Name name ]

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
