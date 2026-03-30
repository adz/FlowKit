namespace EffectFs

/// <summary>
/// Compatibility helpers for incremental migration from FsToolkit-style
/// <c>Async&lt;Result&lt;_,_&gt;&gt;</c> workflows.
/// </summary>
[<RequireQualifiedAccess>]
module AsyncResultCompat =
    /// <summary>
    /// Lifts an <c>Async&lt;Result&lt;_,_&gt;&gt;</c> into an effect.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     async { return Ok 42 }
    ///     |> AsyncResultCompat.ofAsyncResult
    /// </code>
    /// </example>
    let ofAsyncResult
        (operation: Async<Result<'value, 'error>>)
        : Effect<'env, 'error, 'value> =
        Effect.fromAsyncResult operation

    /// <summary>
    /// Runs an effect as <c>Async&lt;Result&lt;_,_&gt;&gt;</c> by supplying the environment.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let asyncResult =
    ///     Effect.succeed 42
    ///     |> AsyncResultCompat.toAsyncResult ()
    /// </code>
    /// </example>
    let toAsyncResult
        (environment: 'env)
        (effect: Effect<'env, 'error, 'value>)
        : Async<Result<'value, 'error>> =
        Effect.toAsyncResult environment effect

    /// <summary>
    /// Maps the success value of an effect during migration.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     async { return Ok 21 }
    ///     |> AsyncResultCompat.ofAsyncResult
    ///     |> AsyncResultCompat.map ((*) 2)
    /// </code>
    /// </example>
    let map
        (mapper: 'value -> 'next)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'next> =
        Effect.map mapper effect

    /// <summary>
    /// Binds the success value of an effect during migration.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.succeed 21
    ///     |> AsyncResultCompat.bind (fun value -> Effect.succeed (value * 2))
    /// </code>
    /// </example>
    let bind
        (binder: 'value -> Effect<'env, 'error, 'next>)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'error, 'next> =
        Effect.bind binder effect

    /// <summary>
    /// Maps the typed error of an effect during migration.
    /// </summary>
    /// <example>
    /// <code lang="fsharp">
    /// let workflow =
    ///     Effect.fail "bad"
    ///     |> AsyncResultCompat.mapError (fun error -> $"Error: {error}")
    /// </code>
    /// </example>
    let mapError
        (mapper: 'error -> 'nextError)
        (effect: Effect<'env, 'error, 'value>)
        : Effect<'env, 'nextError, 'value> =
        Effect.mapError mapper effect
