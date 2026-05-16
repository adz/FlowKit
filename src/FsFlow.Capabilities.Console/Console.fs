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
    /// <returns>A flow that produces the string read from the console.</returns>
    let readLine<'env, 'e when 'env :> IConsole> : Flow<'env, 'e, string> =
        Flow.read (fun (env: 'env) -> env.ReadLine())

    /// <summary>Writes a line to the console environment.</summary>
    /// <param name="message">The message to write to the console.</param>
    /// <returns>A flow that performs the console output.</returns>
    let writeLine (message: string) : Flow<'env, 'e, unit> when 'env :> IConsole =
        Flow.read (fun (env: 'env) -> env.WriteLine(message))

#if !FABLE_COMPILER
    /// <summary>Creates a live console backed by <see cref="T:System.Console" />.</summary>
    /// <returns>A live <see cref="T:FsFlow.Capabilities.Console.IConsole"/> implementation.</returns>
    let live : IConsole =
        { new IConsole with
            member _.ReadLine() = Console.ReadLine()
            member _.WriteLine(message) = Console.WriteLine(message) }
#endif
