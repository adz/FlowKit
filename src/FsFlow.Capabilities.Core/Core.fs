namespace FsFlow.Capabilities.Core

open System
open System.Collections.Generic
open System.Globalization
open FsFlow

/// <summary>Provides synchronous access to the current UTC clock.</summary>
type IClock = FsFlow.IClock

/// <summary>Provides synchronous access to runtime logging.</summary>
type ILog = FsFlow.ILog

/// <summary>Provides synchronous random-number generation.</summary>
type IRandom = FsFlow.IRandom

/// <summary>Provides synchronous GUID generation.</summary>
type IGuid = FsFlow.IGuid

/// <summary>Provides synchronous environment-variable lookup.</summary>
type IEnvironmentVariables = FsFlow.IEnvironmentVariables

/// <summary>Describes a meaningful environment-variable failure.</summary>
[<RequireQualifiedAccess>]
type EnvironmentVariableError =
    /// <summary>The requested variable was not present.</summary>
    | MissingVariable of name: string

    /// <summary>The requested variable existed but could not be parsed.</summary>
    | InvalidVariable of name: string * value: string * expected: string

/// <summary>Helpers for clock capabilities.</summary>
[<RequireQualifiedAccess>]
module Clock =
    /// <summary>Reads the current UTC timestamp from the ambient runtime.</summary>
    /// <returns>A flow that produces the current <see cref="T:System.DateTimeOffset"/>.</returns>
    let now : Flow<'env, 'e, DateTimeOffset> =
        Flow.Runtime.now

    /// <summary>Creates a live clock backed by <see cref="P:System.DateTimeOffset.UtcNow" />.</summary>
    /// <returns>A live <see cref="T:FsFlow.IClock"/> implementation.</returns>
    let live : IClock =
        { new IClock with
            member _.UtcNow() = DateTimeOffset.UtcNow }

    /// <summary>Creates a deterministic clock that always returns the supplied instant.</summary>
    /// <param name="utcNow">The fixed timestamp to return from the clock.</param>
    /// <returns>A mock <see cref="T:FsFlow.IClock"/> implementation.</returns>
    let fromValue (utcNow: DateTimeOffset) : IClock =
        { new IClock with
            member _.UtcNow() = utcNow }

/// <summary>Helpers for runtime logging.</summary>
[<RequireQualifiedAccess>]
module Log =
    /// <summary>Writes an informational log message through the ambient runtime.</summary>
    /// <param name="message">The message to log.</param>
    /// <returns>A flow that performs the logging operation.</returns>
    let info (message: string) : Flow<'env, 'e, unit> =
        Flow.Runtime.log message

    /// <summary>Creates a no-op logger for tests and local overrides.</summary>
    /// <returns>A no-op <see cref="T:FsFlow.ILog"/> implementation.</returns>
    let live : ILog =
        { new ILog with
            member _.Info _ = () }

/// <summary>Helpers for random-number capabilities.</summary>
[<RequireQualifiedAccess>]
module Random =
    /// <summary>Reads a random integer from the ambient runtime.</summary>
    /// <param name="minInclusive">The inclusive lower bound.</param>
    /// <param name="maxExclusive">The exclusive upper bound.</param>
    /// <returns>A flow that produces a random integer.</returns>
    let nextInt (minInclusive: int) (maxExclusive: int) : Flow<'env, 'e, int> =
        Flow.Runtime.nextInt minInclusive maxExclusive

    /// <summary>Creates a live random-number generator backed by <see cref="T:System.Random" />.</summary>
    /// <returns>A live <see cref="T:FsFlow.IRandom"/> implementation.</returns>
    let live : IRandom =
        let rng = System.Random()
        let gate = obj()

        { new IRandom with
            member _.NextInt minInclusive maxExclusive =
                #if FABLE_COMPILER
                rng.Next(minInclusive, maxExclusive)
                #else
                lock gate (fun () -> rng.Next(minInclusive, maxExclusive))
                #endif
        }

    /// <summary>Creates a deterministic random generator that always returns the supplied value.</summary>
    /// <param name="value">The fixed integer to return.</param>
    /// <returns>A mock <see cref="T:FsFlow.IRandom"/> implementation.</returns>
    let fromValue (value: int) : IRandom =
        { new IRandom with
            member _.NextInt _ _ = value }

/// <summary>Helpers for GUID capabilities.</summary>
[<RequireQualifiedAccess>]
module Guid =
    /// <summary>Reads a GUID from the ambient runtime.</summary>
    /// <returns>A flow that produces a new <see cref="T:System.Guid"/>.</returns>
    let newGuid : Flow<'env, 'e, global.System.Guid> =
        Flow.Runtime.newGuid

    /// <summary>Creates a live GUID generator backed by <see cref="M:System.Guid.NewGuid" />.</summary>
    /// <returns>A live <see cref="T:FsFlow.IGuid"/> implementation.</returns>
    let live : IGuid =
        { new IGuid with
            member _.NewGuid() = global.System.Guid.NewGuid() }

    /// <summary>Creates a deterministic GUID generator that always returns the supplied value.</summary>
    /// <param name="value">The fixed GUID to return.</param>
    /// <returns>A mock <see cref="T:FsFlow.IGuid"/> implementation.</returns>
    let fromValue (value: global.System.Guid) : IGuid =
        { new IGuid with
            member _.NewGuid() = value }

/// <summary>Helpers for environment-variable providers.</summary>
[<RequireQualifiedAccess>]
module EnvironmentVariables =
    /// <summary>Reads a raw environment-variable value from the ambient runtime.</summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>A flow that produces the variable value if it exists.</returns>
    let tryGet (name: string) : Flow<'env, 'e, string option> =
        Flow.Runtime.tryGetEnvironmentVariable name

    /// <summary>Creates a live provider backed by the current process environment.</summary>
    /// <returns>A live <see cref="T:FsFlow.IEnvironmentVariables"/> implementation.</returns>
    let live : IEnvironmentVariables =
        { new IEnvironmentVariables with
            member _.TryGet name =
                #if FABLE_COMPILER
                None
                #else
                match Environment.GetEnvironmentVariable name with
                | null -> None
                | value -> Some value
                #endif
        }

    /// <summary>Creates a deterministic provider from a fixed set of name/value pairs.</summary>
    /// <param name="values">The fixed name/value pairs to serve.</param>
    /// <returns>A mock <see cref="T:FsFlow.IEnvironmentVariables"/> implementation.</returns>
    let fromPairs (values: seq<string * string>) : IEnvironmentVariables =
        #if FABLE_COMPILER
        let lookup = Dictionary<string, string>()

        for (name, value) in values do
            lookup[name.ToLowerInvariant()] <- value

        { new IEnvironmentVariables with
            member _.TryGet name =
                match lookup.TryGetValue(name.ToLowerInvariant()) with
                | true, value -> Some value
                | false, _ -> None }
        #else
        let lookup = Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)

        for (name, value) in values do
            lookup[name] <- value

        { new IEnvironmentVariables with
            member _.TryGet name =
                match lookup.TryGetValue name with
                | true, value -> Some value
                | false, _ -> None }
        #endif

/// <summary>Helpers for reading and parsing environment variables from the ambient runtime.</summary>
[<RequireQualifiedAccess>]
module EnvironmentVariable =
    let private readParsed
        (expected: string)
        (parser: string -> 'value option)
        (name: string)
        : Flow<'env, EnvironmentVariableError, 'value> =
        flow {
            let! value = EnvironmentVariables.tryGet name
            match value with
            | None -> return! Flow.fail (EnvironmentVariableError.MissingVariable name)
            | Some value ->
                match parser value with
                | Some parsed -> return parsed
                | None -> return! Flow.fail (EnvironmentVariableError.InvalidVariable(name, value, expected))
        }

    /// <summary>Reads a raw string environment variable from the ambient runtime.</summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>A flow that produces the variable value or fails if it is missing.</returns>
    let get (name: string) : Flow<'env, EnvironmentVariableError, string> =
        flow {
            let! value = EnvironmentVariables.tryGet name
            match value with
            | Some value -> return value
            | None -> return! Flow.fail (EnvironmentVariableError.MissingVariable name)
        }

    /// <summary>Reads a raw string environment variable without wrapping it in a result.</summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>A flow that produces the variable value if it exists.</returns>
    let tryGet (name: string) : Flow<'env, 'e, string option> =
        EnvironmentVariables.tryGet name

    /// <summary>Reads an integer environment variable from the ambient runtime.</summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>A flow that produces the parsed integer or fails if missing or invalid.</returns>
    let getInt (name: string) : Flow<'env, EnvironmentVariableError, int> =
        readParsed "an integer" (fun value ->
            match Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture) with
            | true, parsed -> Some parsed
            | false, _ -> None) name

    /// <summary>Reads a GUID environment variable from the ambient runtime.</summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>A flow that produces the parsed GUID or fails if missing or invalid.</returns>
    let getGuid (name: string) : Flow<'env, EnvironmentVariableError, global.System.Guid> =
        readParsed "a GUID" (fun value ->
            match Guid.TryParse value with
            | true, parsed -> Some parsed
            | false, _ -> None) name

    /// <summary>Reads a boolean environment variable from the ambient runtime.</summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <returns>A flow that produces the parsed boolean or fails if missing or invalid.</returns>
    let getBool (name: string) : Flow<'env, EnvironmentVariableError, bool> =
        readParsed "a boolean" (fun value ->
            match Boolean.TryParse value with
            | true, parsed -> Some parsed
            | false, _ -> None) name

/// <summary>Helpers for formatting environment-variable errors.</summary>
[<RequireQualifiedAccess>]
module EnvironmentVariableErrors =
    /// <summary>Formats a human-readable description for an error.</summary>
    /// <param name="error">The environment variable error to describe.</param>
    /// <returns>A human-readable error message.</returns>
    let describe =
        function
        | EnvironmentVariableError.MissingVariable name ->
            $"Environment variable '{name}' was not set."
        | EnvironmentVariableError.InvalidVariable(name, value, expected) ->
            $"Environment variable '{name}' had value '{value}' but expected {expected}."
