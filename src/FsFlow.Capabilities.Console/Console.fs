namespace FsFlow.Capabilities.Console

open System
open FsFlow

/// <summary>Provides synchronous access to standard console I/O.</summary>
type IConsole =
    /// <summary>Reads a line of characters from the standard input stream.</summary>
    abstract ReadLine : unit -> string
    /// <summary>Writes the specified string value, followed by the current line terminator, to the standard output stream.</summary>
    abstract WriteLine : string -> unit

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Console =
    /// <summary>Reads a line from the console environment.</summary>
    let readLine<'env when 'env :> Requires<IConsole>> : Flow<'env, 'e, string> =
        Flow.read (fun (env: 'env) -> env.Dep.ReadLine())

    /// <summary>Writes a line to the console environment.</summary>
    let writeLine (message: string) : Flow<'env, 'e, unit> when 'env :> Requires<IConsole> =
        Flow.read (fun (env: 'env) -> env.Dep.WriteLine(message))

#if !FABLE_COMPILER
    /// <summary>Creates a live console backed by <see cref="T:System.Console" />.</summary>
    let live : IConsole =
        { new IConsole with
            member _.ReadLine() = Console.ReadLine()
            member _.WriteLine(message) = Console.WriteLine(message) }
#endif
