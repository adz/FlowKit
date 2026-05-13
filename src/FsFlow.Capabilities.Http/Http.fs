namespace FsFlow.Capabilities.Http

open System
open System.Net.Http
open System.Threading.Tasks
open FsFlow

/// <summary>Provides asynchronous access to HTTP client operations.</summary>
type IHttp =
    /// <summary>Sends a GET request to the specified Uri and returns the response body as a string in an asynchronous operation.</summary>
    abstract GetString : url: string -> Task<string>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Http =
    /// <summary>Sends a GET request using the HTTP environment and returns the response body.</summary>
    let getString (url: string) : Flow<'env, 'e, string> when 'env :> Requires<IHttp> =
        flow {
            let! (env: 'env) = Flow.env
            return! env.Dep.GetString(url)
        }

#if !FABLE_COMPILER
    /// <summary>Creates a live HTTP client backed by <see cref="T:System.Net.Http.HttpClient" />.</summary>
    let live (client: HttpClient) : IHttp =
        { new IHttp with
            member _.GetString(url) = client.GetStringAsync(url) }
#endif
