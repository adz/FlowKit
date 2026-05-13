namespace FsFlow.Capabilities.Process

open System
open System.Diagnostics
open System.Threading.Tasks
open FsFlow

/// <summary>Represents the outcome of an external process execution.</summary>
type ProcessResult =
    {
        /// <summary>The exit code returned by the process.</summary>
        ExitCode: int
        /// <summary>The standard output stream of the process.</summary>
        StdOut: string
        /// <summary>The standard error stream of the process.</summary>
        StdErr: string
    }

/// <summary>Provides asynchronous access to external process execution.</summary>
type IProcess =
    /// <summary>Executes an external process and returns its result asynchronously.</summary>
    abstract Execute : fileName: string * arguments: string -> Task<ProcessResult>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Process =
    /// <summary>Executes a process using the process environment and returns the result.</summary>
    let execute (fileName: string) (arguments: string) : Flow<'env, 'e, ProcessResult> when 'env :> Requires<IProcess> =
        flow {
            let! (env: 'env) = Flow.env
            return! env.Dep.Execute(fileName, arguments)
        }

#if !FABLE_COMPILER
    /// <summary>Creates a live process runner backed by <see cref="T:System.Diagnostics.Process" />.</summary>
    let live : IProcess =
        { new IProcess with
            member _.Execute(fileName, arguments) =
                task {
                    let startInfo = ProcessStartInfo(fileName, arguments)
                    startInfo.RedirectStandardOutput <- true
                    startInfo.RedirectStandardError <- true
                    startInfo.UseShellExecute <- false
                    startInfo.CreateNoWindow <- true
                    
                    use p = new Process()
                    p.StartInfo <- startInfo
                    p.Start() |> ignore
                    
                    let! stdOut = p.StandardOutput.ReadToEndAsync()
                    let! stdErr = p.StandardError.ReadToEndAsync()
                    #if NETSTANDARD2_1
                    p.WaitForExit()
                    #else
                    do! p.WaitForExitAsync()
                    #endif
                    
                    return { ExitCode = p.ExitCode; StdOut = stdOut; StdErr = stdErr }
                } }
#endif
