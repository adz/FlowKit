namespace FsFlow

#if !FABLE_COMPILER
open System
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Represents a stateful schedule that can decide whether to continue and how long to delay.
/// </summary>
type Schedule<'env, 'input, 'output> =
    private
    | Schedule of ('input -> int -> Flow<'env, unit, 'output option * TimeSpan>)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Schedule =
    /// <summary>Creates a schedule that recurs a fixed number of times.</summary>
    /// <param name="n">The maximum number of times to recur.</param>
    /// <returns>A schedule that recurs up to <paramref name="n"/> times, emitting the current attempt count (0 to n-1).</returns>
    /// <example>
    /// <code>
    /// let schedule = Schedule.recurs 3
    /// // Will run for attempts 0, 1, 2 and then stop.
    /// </code>
    /// </example>
    let recurs (n: int) : Schedule<'env, 'input, int> =
        Schedule(fun _ attempt ->
            if attempt < n then
                Flow.ok (Some attempt, TimeSpan.Zero)
            else
                Flow.ok (None, TimeSpan.Zero))

    /// <summary>Creates a schedule that recurs with a fixed delay between attempts.</summary>
    /// <param name="delay">The fixed time span to wait between each attempt.</param>
    /// <returns>A schedule that recurs indefinitely with the specified fixed delay, emitting the current attempt count.</returns>
    /// <example>
    /// <code>
    /// let schedule = Schedule.spaced (TimeSpan.FromSeconds 1.0)
    /// </code>
    /// </example>
    let spaced (delay: TimeSpan) : Schedule<'env, 'input, int> =
        Schedule(fun _ attempt ->
            Flow.ok (Some attempt, delay))

    /// <summary>Creates a schedule that recurs with exponential backoff.</summary>
    /// <param name="baseDelay">The initial delay for the first retry.</param>
    /// <returns>A schedule that recurs indefinitely, doubling the delay each time (baseDelay * 2^attempt).</returns>
    /// <example>
    /// <code>
    /// let schedule = Schedule.exponential (TimeSpan.FromMilliseconds 100.0)
    /// // Delays: 100ms, 200ms, 400ms, 800ms...
    /// </code>
    /// </example>
    let exponential (baseDelay: TimeSpan) : Schedule<'env, 'input, TimeSpan> =
        Schedule(fun _ attempt ->
            let delay = TimeSpan.FromTicks(baseDelay.Ticks * int64 (Math.Pow(2.0, float attempt)))
            Flow.ok (Some delay, delay))

    /// <summary>Adds random jitter to a schedule's delay.</summary>
    /// <param name="schedule">The base schedule to which jitter will be applied.</param>
    /// <returns>A new schedule where each delay is multiplied by a random factor between 0.5 and 1.5.</returns>
    /// <example>
    /// <code>
    /// let schedule = Schedule.spaced (TimeSpan.FromSeconds 1.0) |> Schedule.jittered
    /// </code>
    /// </example>
    let jittered (Schedule op) : Schedule<'env, 'input, 'output> =
        let random = Random()
        Schedule(fun input attempt ->
            Flow.map (fun (out, (delay: TimeSpan)) ->
                let jitter = random.NextDouble() + 0.5
                let jitteredDelay = TimeSpan.FromTicks(int64 (float delay.Ticks * jitter))
                out, jitteredDelay
            ) (op input attempt))

[<AutoOpen>]
module FlowScheduleExtensions =
    type Flow<'env, 'error, 'value> with
        /// <summary>Retries a failing flow according to the supplied schedule.</summary>
        /// <param name="flow">The workflow to retry if it fails.</param>
        /// <param name="schedule">The schedule that determines when and if to retry based on the error.</param>
        /// <returns>A flow that will retry the original flow according to the schedule until it succeeds or the schedule stops.</returns>
        /// <example>
        /// <code>
        /// let flakyWork = Flow.fail "oops"
        /// let retried = Flow.Retry(flakyWork, Schedule.recurs 3)
        /// </code>
        /// </example>
        static member Retry
            (
                flow: Flow<'env, 'error, 'value>,
                schedule: Schedule<'env, 'error, 'output>
            ) : Flow<'env, 'error, 'value> =
            let (Schedule op) = schedule
            let rec loop attempt =
                let (Flow operation) = flow
                Flow(fun env ct ->
                    EffectFlow.fold
                        (fun v -> EffectFlow.ofValue v)
                        (fun cause ->
                            match cause with
                            | Cause.Fail e ->
                                let (Flow scheduleOp) = op e attempt
                                EffectFlow.bind (fun (decision, delay) ->
                                    match decision with
                                    | Some _ ->
                                        let (Flow sleepOp) = Flow.Runtime.sleep delay
                                        EffectFlow.bind (fun () -> 
                                            let (Flow nextLoopOp) = loop (attempt + 1)
                                            nextLoopOp env ct) 
                                            (EffectFlow.mapError (fun () -> e) (sleepOp env ct))
                                    | None -> EffectFlow.ofCause cause) 
                                    (EffectFlow.mapError (fun () -> e) (scheduleOp env ct))
                            | _ -> EffectFlow.ofCause cause)
                        (operation env ct))
            loop 0

        /// <summary>Repeats a successful flow according to the supplied schedule.</summary>
        /// <param name="flow">The workflow to repeat if it succeeds.</param>
        /// <param name="schedule">The schedule that determines when and if to repeat based on the successful value.</param>
        /// <returns>A flow that repeats the original flow according to the schedule, returning the last successful value when it stops.</returns>
        /// <example>
        /// <code>
        /// let work = Flow.ok 42
        /// let repeated = Flow.Repeat(work, Schedule.recurs 5)
        /// </code>
        /// </example>
        static member Repeat
            (
                flow: Flow<'env, 'error, 'value>,
                schedule: Schedule<'env, 'value, 'output>
            ) : Flow<'env, 'error, 'value> =
            let (Schedule op) = schedule
            let rec loop attempt lastValue =
                let (Flow scheduleOp) = op lastValue attempt
                Flow(fun env ct ->
                    EffectFlow.bind (fun (decision, (delay: TimeSpan)) ->
                        match decision with
                        | Some _ ->
                            let (Flow sleepOp) = Flow.Runtime.sleep delay
                            EffectFlow.bind (fun () -> 
                                let (Flow nextOp) = flow
                                EffectFlow.bind (fun nextValue -> 
                                    let (Flow nextLoopOp) = loop (attempt + 1) nextValue
                                    nextLoopOp env ct) (nextOp env ct)) 
                                (EffectFlow.mapError (fun () -> Unchecked.defaultof<'error>) (sleepOp env ct))
                        | None -> EffectFlow.ofValue lastValue) 
                        (EffectFlow.mapError (fun () -> Unchecked.defaultof<'error>) (scheduleOp env ct)))
            flow |> Flow.bind (loop 0)
#endif
