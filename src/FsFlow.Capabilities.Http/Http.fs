namespace FsFlow.Capabilities.Http

open System
open System.Net.Http
open System.Threading.Tasks
open FsFlow

/// <summary>Provides asynchronous access to HTTP client operations.</summary>
type IHttp =
    /// <summary>Sends a GET request to the specified Uri and returns the response body as a string.</summary>
    /// <param name="url">The URL to fetch.</param>
    /// <returns>A task that represents the asynchronous fetch operation.</returns>
    abstract GetString : url: string -> Task<string>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Http =
    /// <summary>Sends a GET request using the HTTP environment and returns the response body.</summary>
    /// <param name="url">The URL to fetch.</param>
    /// <returns>A flow that returns the response body as a string.</returns>
    /// <example>
    /// <code>
    /// let content = Http.getString "https://example.com"
    /// </code>
    /// </example>
    let getString (url: string) : Flow<'env, 'e, string> when 'env :> IHttp =
        flow {
            let! (env: 'env) = Flow.env
            return! env.GetString(url)
        }

#if !FABLE_COMPILER
    /// <summary>Creates a live HTTP client backed by <see cref="T:System.Net.Http.HttpClient" />.</summary>
    /// <param name="client">The underlying HttpClient to use.</param>
    /// <returns>An implementation of IHttp.</returns>
    let live (client: HttpClient) : IHttp =
        { new IHttp with
            member _.GetString(url) = client.GetStringAsync(url) }
#endif
