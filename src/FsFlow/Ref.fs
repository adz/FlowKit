namespace FsFlow

open System
open System.Threading
open System.Threading.Tasks

/// <summary>
/// Represents a handle to a mutable reference that can be updated atomically.
/// </summary>
/// <typeparam name="T">The type of the value stored in the reference.</typeparam>
type Ref<'T> =
    private
    | Ref of ('T ref * obj)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Ref =
    /// <summary>Creates a new <see cref="T:FsFlow.Ref`1" /> with the initial value.</summary>
    let make (value: 'T) : Flow<'env, 'none, Ref<'T>> =
        Flow.ok (Ref (ref value, obj()))

    /// <summary>Reads the current value of the reference.</summary>
    let get (Ref (cell, gate)) : Flow<'env, 'none, 'T> =
        Flow.read (fun _ -> lock gate (fun () -> cell.Value))

    /// <summary>Sets the value of the reference to the specified value.</summary>
    let set (value: 'T) (Ref (cell, gate)) : Flow<'env, 'none, unit> =
        Flow.read (fun _ -> lock gate (fun () -> cell.Value <- value))

    /// <summary>Updates the value of the reference using the supplied function.</summary>
    let update (f: 'T -> 'T) (Ref (cell, gate)) : Flow<'env, 'none, unit> =
        Flow.read (fun _ -> lock gate (fun () -> cell.Value <- f cell.Value))

    /// <summary>Updates the value of the reference using the supplied function and returns a derived value.</summary>
    let modify (f: 'T -> 'T * 'v) (Ref (cell, gate)) : Flow<'env, 'none, 'v> =
        Flow.read (fun _ -> 
            lock gate (fun () -> 
                let next, result = f cell.Value
                cell.Value <- next
                result))
