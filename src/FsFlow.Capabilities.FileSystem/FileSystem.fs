namespace FsFlow.Capabilities.FileSystem

open System.IO
open FsFlow

/// <summary>Provides synchronous access to file system operations.</summary>
type IFileSystem =
    /// <summary>Opens a text file, reads all lines of the file into a string, and then closes the file.</summary>
    abstract ReadAllText : path: string -> string
    /// <summary>Creates a new file, writes the specified string to the file, and then closes the file. If the target file already exists, it is overwritten.</summary>
    abstract WriteAllText : path: string * contents: string -> unit
    /// <summary>Determines whether the specified file exists.</summary>
    abstract Exists : path: string -> bool

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module FileSystem =
    /// <summary>Reads all text from a file using the file system environment.</summary>
    /// <param name="path">The path of the file to read.</param>
    /// <returns>A flow that produces the contents of the file.</returns>
    let readAllText (path: string) : Flow<'env, 'e, string> when 'env :> IFileSystem =
        Flow.read (fun (env: 'env) -> env.ReadAllText(path))

    /// <summary>Writes all text to a file using the file system environment.</summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="contents">The string contents to write.</param>
    /// <returns>A flow that performs the file write operation.</returns>
    let writeAllText (path: string) (contents: string) : Flow<'env, 'e, unit> when 'env :> IFileSystem =
        Flow.read (fun (env: 'env) -> env.WriteAllText(path, contents))

    /// <summary>Checks if a file exists using the file system environment.</summary>
    /// <param name="path">The path of the file to check.</param>
    /// <returns>A flow that produces true if the file exists, false otherwise.</returns>
    let exists (path: string) : Flow<'env, 'e, bool> when 'env :> IFileSystem =
        Flow.read (fun (env: 'env) -> env.Exists(path))

#if !FABLE_COMPILER
    /// <summary>Creates a live file system backed by <see cref="T:System.IO.File" />.</summary>
    /// <returns>A live <see cref="T:FsFlow.Capabilities.FileSystem.IFileSystem"/> implementation.</returns>
    let live : IFileSystem =
        { new IFileSystem with
            member _.ReadAllText(path) = File.ReadAllText(path)
            member _.WriteAllText(path, contents) = File.WriteAllText(path, contents)
            member _.Exists(path) = File.Exists(path) }
#endif
