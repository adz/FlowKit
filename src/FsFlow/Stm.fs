namespace FsFlow

#if !FABLE_COMPILER
open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Internal interface for transactional references used by the STM engine.
/// </summary>
type ITRef =
    abstract Id: int64
    abstract Commit: obj -> unit
    abstract CurrentValue: obj

/// <summary>
/// Represents a transactional reference that can be updated atomically within an <see cref="T:FsFlow.STM`1" /> transaction.
/// </summary>
/// <typeparam name="T">The type of the value stored in the reference.</typeparam>
type TRef<'T>(initialValue: 'T) =
    static let mutable idCounter = 0L
    let id = Interlocked.Increment(&idCounter)
    let mutable value = initialValue
    
    interface ITRef with
        member _.Id = id
        member _.Commit(v) = value <- unbox<'T> v
        member _.CurrentValue = box value
        
    member internal _.Id = id
    member internal _.Value = value

/// <summary>
/// Internal journal used to track transactional changes.
/// </summary>
type TJournal = Dictionary<int64, obj * ITRef>

/// <summary>
/// Represents a transactional operation that can be composed and executed atomically.
/// </summary>
/// <typeparam name="T">The type of the value produced by the operation.</typeparam>
type STM<'T> = STM of (TJournal -> 'T)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module TRef =
    /// <summary>Creates a new <see cref="T:FsFlow.TRef`1" /> with the initial value.</summary>
    let make (value: 'T) : STM<TRef<'T>> =
        STM(fun _ -> TRef(value))

    /// <summary>Reads the current value of the transactional reference within a transaction.</summary>
    let get (tref: TRef<'T>) : STM<'T> =
        STM(fun journal ->
            match journal.TryGetValue tref.Id with
            | true, (v, _) -> unbox<'T> v
            | false, _ -> tref.Value)

    /// <summary>Sets the value of the transactional reference within a transaction.</summary>
    let set (value: 'T) (tref: TRef<'T>) : STM<unit> =
        STM(fun journal ->
            journal[tref.Id] <- (box value, tref :> ITRef))

    /// <summary>Updates the value of the transactional reference within a transaction using the supplied function.</summary>
    let update (f: 'T -> 'T) (tref: TRef<'T>) : STM<unit> =
        STM(fun journal ->
            let current = 
                match journal.TryGetValue tref.Id with
                | true, (v, _) -> unbox<'T> v
                | false, _ -> tref.Value
            journal[tref.Id] <- (box (f current), tref :> ITRef))

/// <summary>
/// Computation expression builder for STM transactions.
/// </summary>
type StmBuilder() =
    member _.Return(value: 'T) : STM<'T> = STM(fun _ -> value)
    member _.ReturnFrom(stm: STM<'T>) : STM<'T> = stm
    member _.Bind(stm: STM<'T>, f: 'T -> STM<'U>) : STM<'U> =
        STM(fun journal ->
            let (STM op) = stm
            let value = op journal
            let (STM nextOp) = f value
            nextOp journal)
    member _.Zero() : STM<unit> = STM(fun _ -> ())
    member _.Delay(f: unit -> STM<'T>) : STM<'T> = STM(fun journal -> let (STM op) = f () in op journal)
    member _.Combine(stm1: STM<unit>, stm2: STM<'T>) : STM<'T> =
        STM(fun journal -> 
            let (STM op1) = stm1
            let (STM op2) = stm2
            op1 journal
            op2 journal)

[<AutoOpen>]
module StmBuilders =
    /// <summary>
    /// The <c>stm { }</c> computation expression for building atomic transactions.
    /// </summary>
    let stm = StmBuilder()

[<RequireQualifiedAccess>]
module STM =
    let private stmLock = obj()

    /// <summary>Executes an STM transaction atomically within a flow.</summary>
    /// <param name="transaction">The STM transaction to execute.</param>
    /// <returns>A flow that performs the transaction and returns its result.</returns>
    let atomically (transaction: STM<'T>) : Flow<'env, 'none, 'T> =
        Flow(fun _ _ ->
            let (STM op) = transaction
            lock stmLock (fun () ->
                let journal = TJournal()
                let result = op journal
                for KeyValue(_, (v, tref)) in journal do
                    tref.Commit(v)
                EffectFlow.ofValue result
            )
        )
#endif
