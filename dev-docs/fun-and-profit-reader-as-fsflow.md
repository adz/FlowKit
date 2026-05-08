# Rewriting F# for Fun and Profit’s Reader Monad Example with FsFlow

Scott Wlaschin’s excellent post [“Reinventing the Reader monad”](https://fsharpforfunandprofit.com/posts/elevated-world-6/) walks through a real problem: API code that needs an `ApiClient`, can fail with `Result`, and becomes awkward when you try to compose small functions.

The post starts with ordinary API code, then gradually discovers a custom wrapper:

```fsharp
type ApiAction<'a> = ApiAction of (ApiClient -> 'a)
```

Later, because the API calls can fail, it stacks that with `Result` and introduces something like:

```fsharp
ApiAction<Result<'a>>
```

The post then shows that this is really the Reader monad:

```fsharp
type Reader<'environment, 'a> = Reader of ('environment -> 'a)
```

That is the important insight.

With FsFlow, you normally do not need to invent the wrapper yourself. FsFlow already gives you the shape you were heading toward:

```fsharp
Flow<'env, 'error, 'value>
```

So the post’s final abstraction:

```fsharp
ApiClient -> Result<'a, string list>
```

becomes:

```fsharp
Flow<ApiClient, string list, 'a>
```

`ApiClient` is the environment.

`string list` is the error channel.

`'a` is the success value.

If you want the whole environment value inside a flow, the current API is `Flow.env`, `AsyncFlow.env`, or `TaskFlow.env`.

And FsFlow’s computation expression handles the boring parts: reading the environment, binding results, returning values, composing flows, managing acquire/release, catching exceptions, and finally running the flow at the boundary.

## The original problem

The post’s scenario is simple:

```text
Open API connection
Get product ids purchased by customer id using the API
For each product id:
    get the product info for that id using the API
Close API connection
Return the list of product infos
```

The API has this kind of shape:

```fsharp
type CustId = CustId of string

type ProductId = ProductId of string

type ProductInfo =
    { ProductName: string }
```

And a synchronous teaching API like this:

```fsharp
type ApiClient() =
    member _.Get<'a>(id: obj) : Result<'a, string list> =
        // pretend this talks to Redis / a key-value store / a remote API
        failwith "example"

    interface System.IDisposable with
        member _.Dispose() =
            printfn "[API] Disposing"
```

The original post eventually refactors the API-using functions into this shape:

```fsharp
CustId -> ApiClient -> Result<ProductId list, string list>
ProductId -> ApiClient -> Result<ProductInfo, string list>
```

That shape is already Reader-ish. Each function takes its normal input, then waits for an `ApiClient` environment.

In FsFlow, instead of exposing the final `ApiClient` argument directly, we make the dependency explicit in the return type.

## First synchronous FsFlow version

The purchase-id lookup becomes:

```fsharp
let getPurchaseIds
    (custId: CustId)
    : Flow<ApiClient, string list, ProductId list> =
    flow {
        let! api = Flow.env
        return! api.Get<ProductId list>(custId)
    }
```

The product-info lookup becomes:

```fsharp
let getProductInfo
    (productId: ProductId)
    : Flow<ApiClient, string list, ProductInfo> =
    flow {
        let! api = Flow.env
        return! api.Get<ProductInfo>(productId)
    }
```

There is no custom `ApiAction` wrapper, and there is no handwritten `run`/`bind` pair.

The type says everything:

```fsharp
Flow<ApiClient, string list, ProductInfo>
```

This means:

> Given an `ApiClient` environment, this flow may fail with `string list`, or succeed with `ProductInfo`.

Because FsFlow auto-lifts compatible values, the `Result<'value, 'error>` returned by `api.Get` can be used directly with `return!`.

So this line is clean:

```fsharp
return! api.Get<ProductInfo>(productId)
```

It means:

```text
Call the API.
If it returns Ok, continue with the value.
If it returns Error, short-circuit the flow with that error.
```

## Composing it without traverse first

To make the composition obvious, we can start without `traverse`:

```fsharp
let getPurchaseInfo
    (custId: CustId)
    : Flow<ApiClient, string list, ProductInfo list> =
    flow {
        let! productIds = getPurchaseIds custId

        let mutable productInfos = []

        for productId in productIds do
            let! productInfo = getProductInfo productId
            productInfos <- productInfo :: productInfos

        return List.rev productInfos
    }
```

This works, but it is not the style we want. The loop is not the interesting part of the program. The interesting part is:

```text
For each product id, run getProductInfo, collecting the results inside the same flow.
```

That is exactly `traverse`.

## Adding Flow.traverse

With `Flow.traverse`, the same function becomes:

```fsharp
let getPurchaseInfo
    (custId: CustId)
    : Flow<ApiClient, string list, ProductInfo list> =
    flow {
        let! productIds = getPurchaseIds custId
        return! Flow.traverse getProductInfo productIds
    }
```

`Flow.traverse` turns this:

```fsharp
ProductId -> Flow<ApiClient, string list, ProductInfo>
```

into this:

```fsharp
ProductId list -> Flow<ApiClient, string list, ProductInfo list>
```

So we do not need a mutable `ResizeArray`, a manual loop, or special failure plumbing.

## Running the synchronous flow

The API lifetime stays at the edge:

```fsharp
let executeApiFlow
    (apiFlow: Flow<ApiClient, string list, 'value>)
    : Result<'value, string list> =

    use api = new ApiClient()
    Flow.run api apiFlow
```

Usage:

```fsharp
let result =
    CustId "C1"
    |> getPurchaseInfo
    |> executeApiFlow
```

This is the same separation the post was aiming for:

* the flow describes what to do with an API client;
* the executor decides how to create and dispose the API client.

The difference is that FsFlow makes the Reader shape explicit without requiring a hand-written wrapper.

## But real APIs are usually async

The original article uses synchronous `Get` because it is teaching composition.

In real .NET code, a Redis client, HTTP client, database client, or GraphQL client is usually asynchronous.

If the API returns `Async<Result<_, _>>`, use `AsyncFlow`.
If it returns `Task<Result<_, _>>`, use `TaskFlow`.

For the async version, a direct translation looks like this:

```fsharp
member _.GetAsync<'a>(id: obj) : Async<Result<'a, string list>>
```

In that case, the direct FsFlow equivalent is `asyncFlow`:

```fsharp
let getPurchaseIdsAsync
    (custId: CustId)
    : AsyncFlow<ApiClient, string list, ProductId list> =
    asyncFlow {
        let! api = AsyncFlow.env
        return! api.GetAsync<ProductId list>(custId)
    }
```

And:

```fsharp
let getProductInfoAsync
    (productId: ProductId)
    : AsyncFlow<ApiClient, string list, ProductInfo> =
    asyncFlow {
        let! api = AsyncFlow.env
        return! api.GetAsync<ProductInfo>(productId)
    }
```

Again, the important point is that `GetAsync` returns:

```fsharp
Async<Result<'a, string list>>
```

and FsFlow can lift that into the async flow.

So this line is still clean:

```fsharp
return! api.GetAsync<ProductInfo>(productId)
```

The composed version is the same idea:

```fsharp
let getPurchaseInfoAsync
    (custId: CustId)
    : AsyncFlow<ApiClient, string list, ProductInfo list> =
    asyncFlow {
        let! productIds = getPurchaseIdsAsync custId
        return! AsyncFlow.traverse getProductInfoAsync productIds
    }
```

## Resource lifetime

If the API is a resource that needs an explicit open and close step, model that with `AsyncFlow.Runtime.useWithAcquireRelease`.

This helper does one thing: it runs an acquire flow, then a use flow, then a release function. The acquire step is `Open`, and the release step is `Close`.

```fsharp
let openApiAsync =
    asyncFlow {
        return! ApiClient.ConnectAsync()
    }

let closeApiAsync (api: ApiClient) =
    api.CloseAsync()

let executeApiAsyncFlow
    (apiFlow: AsyncFlow<ApiClient, string list, 'value>)
    : Async<Result<'value, string list>> =

    let safeFlow =
        apiFlow
        |> AsyncFlow.catch (fun ex -> [ sprintf "Unexpected API exception: %s" ex.Message ])

    AsyncFlow.Runtime.useWithAcquireRelease
        openApiAsync
        closeApiAsync
        (fun api ->
            asyncFlow {
                return! AsyncFlow.run api safeFlow
            })
    |> AsyncFlow.run ()
```

That keeps the open/use/close meaning visible in the code instead of collapsing the lifetime into a plain `use`.

Usage:

```fsharp
let resultAsync =
    CustId "C1"
    |> getPurchaseInfoAsync
    |> executeApiAsyncFlow
```

Now the whole flow is cold and async. Nothing happens until the flow is run.

That keeps the Reader-like property from the original post: the flow is just a description of work that needs an environment.

## Modern .NET APIs usually use Task and CancellationToken

In modern .NET, the API is more likely to look like this:

```fsharp
member _.GetAsync<'a>
    (id: obj, cancellationToken: CancellationToken)
    : Task<Result<'a, string list>>
```

That means our wrapping functions need access to the current cancellation token.

In FsFlow, `TaskFlow.Runtime.cancellationToken` reads the token at execution time, so the task-based wrapper can stay cold without extra plumbing.

Instead, write:

```fsharp
let getPurchaseIdsTask
    (custId: CustId)
    : TaskFlow<ApiClient, string list, ProductId list> =
    taskFlow {
        let! api = TaskFlow.env
        let! ct = TaskFlow.Runtime.cancellationToken
        return! api.GetAsync<ProductId list>(custId, ct)
    }
```

And:

```fsharp
let getProductInfoTask
    (productId: ProductId)
    : TaskFlow<ApiClient, string list, ProductInfo> =
    taskFlow {
        let! api = TaskFlow.env
        let! ct = TaskFlow.Runtime.cancellationToken
        return! api.GetAsync<ProductInfo>(productId, ct)
    }
```

The composition stays the same:

```fsharp
let getPurchaseInfoTask
    (custId: CustId)
    : TaskFlow<ApiClient, string list, ProductInfo list> =
    taskFlow {
        let! productIds = getPurchaseIdsTask custId
        return! TaskFlow.traverse getProductInfoTask productIds
    }
```

The flow describes the API work. The cancellation token is supplied only when the outer task flow is run.

## Resource lifetime with taskFlow

If the API connection itself is acquired asynchronously, model the open/use/close steps with `TaskFlow.Runtime.useWithAcquireRelease`.

Again, the helper just sequences `Open`, `Use`, and `Close`. It does not invent disposal semantics for you.

For example:

```fsharp
type ApiClient =
    static member ConnectAsync
        (cancellationToken: CancellationToken)
        : Task<ApiClient> =
        task {
            // open socket, authenticate, warm up connection, etc.
            return ApiClient()
        }

    member _.CloseAsync() : Task =
        task {
            // Close socket, flush buffers, release handles, etc.
            return ()
        }

let openApiTask =
    taskFlow {
        let! ct = TaskFlow.Runtime.cancellationToken
        return! ApiClient.ConnectAsync(ct)
    }

let closeApiTask (api: ApiClient) =
    api.CloseAsync()
```

Then the executor can use the acquire/release helper:

```fsharp
let executeApiTaskFlow
    (cancellationToken: CancellationToken)
    (apiFlow: TaskFlow<ApiClient, string list, 'value>)
    : Task<Result<'value, string list>> =

    let safeFlow =
        apiFlow
        |> TaskFlow.catch toApiError

    TaskFlow.Runtime.useWithAcquireRelease
        openApiTask
        closeApiTask
        (fun api ->
            taskFlow {
                let! ct = TaskFlow.Runtime.cancellationToken
                return! TaskFlow.run api ct safeFlow
            })
    |> TaskFlow.run () cancellationToken
```

There is no manual `Open()` and `Close()` anymore. Resource lifetime is expressed using the acquire/release helper, and the close step is handled by the flow.

## Catch exceptions in the flow, then run it

The API may already return `Result`, but the real world still throws:

* DNS failures;
* socket failures;
* serializer bugs;
* timeout exceptions;
* unexpected client-library exceptions.

So the boundary should catch exceptions and map them into the flow’s error channel.

One option is to add `catch` around the whole task flow before running it:

```fsharp
let toApiError (ex: exn) : string list =
    [ sprintf "Unexpected API exception: %s" ex.Message ]

let executeApiTaskFlow
    (cancellationToken: CancellationToken)
    (apiFlow: TaskFlow<ApiClient, string list, 'value>)
    : Task<Result<'value, string list>> =

    openApiTask
    |> TaskFlow.Runtime.useWithAcquireRelease closeApiTask
        (fun api ->
            taskFlow {
                let! ct = TaskFlow.Runtime.cancellationToken

                let safeFlow =
                    apiFlow
                    |> TaskFlow.catch toApiError

                return! TaskFlow.run api ct safeFlow
            })
    |> TaskFlow.run () cancellationToken
```

This keeps the actual API functions focused on the happy-path composition:

```fsharp
let getPurchaseInfoTask custId =
    taskFlow {
        let! productIds = getPurchaseIdsTask custId
        return! TaskFlow.traverse getProductInfoTask productIds
    }
```

And it keeps the operational policy at the boundary:

```text
Acquire API client.
Catch unexpected exceptions.
Run the flow.
Dispose the API client.
```

That is usually the right split.

## What changed from the original post?

The original post teaches the abstraction by building it manually.

That is valuable.

But once you already have FsFlow, you do not need to keep the training wheels in application code.

This:

```fsharp
type ApiAction<'a> = ApiAction of (ApiClient -> 'a)
```

becomes the environment parameter of `Flow`:

```fsharp
Flow<ApiClient, string list, 'a>
```

This:

```fsharp
ApiAction<Result<'a, string list>>
```

becomes:

```fsharp
Flow<ApiClient, string list, 'a>
```

This:

```fsharp
ApiActionResult.bind
```

becomes normal `let!` inside `flow`, `asyncFlow`, or `taskFlow`.

This:

```fsharp
ApiAction.run api action
```

becomes:

```fsharp
Flow.run api flow
AsyncFlow.run api flow
TaskFlow.run api cancellationToken flow
```

And this manually discovered Reader idea:

```fsharp
Reader<'environment, 'a>
```

becomes a practical application type:

```fsharp
Flow<'env, 'error, 'value>
```

That is FsFlow’s value here: it lets you write the code at the level the article eventually reaches, without requiring each application to reinvent the wrapper, the result-stacked wrapper, the bind functions, and the execution function.

## Entire synchronous example

```fsharp
open System

// Domain

type CustId = CustId of string

type ProductId = ProductId of string

type ProductInfo =
    { ProductName: string }

// Dummy API client

type ApiClient() =
    static let mutable data = Map.empty<string, obj>

    member private _.TryCast<'a> key (value: obj) : Result<'a, string list> =
        match value with
        | :? 'a as a -> Ok a
        | _ ->
            let typeName = typeof<'a>.Name
            Error [ sprintf "Can't cast value at %s to %s" key typeName ]

    member this.Get<'a>(id: obj) : Result<'a, string list> =
        let key = sprintf "%A" id
        printfn "[API] Get %s" key

        match Map.tryFind key data with
        | Some value -> this.TryCast<'a> key value
        | None -> Error [ sprintf "Key %s not found" key ]

    member _.Set(id: obj, value: obj) : Result<unit, string list> =
        let key = sprintf "%A" id
        printfn "[API] Set %s" key

        if key = "bad" then
            Error [ sprintf "Bad Key %s" key ]
        else
            data <- Map.add key value data
            Ok ()

    interface IDisposable with
        member _.Dispose() =
            printfn "[API] Disposing"

// FsFlow API actions

let getPurchaseIds
    (custId: CustId)
    : Flow<ApiClient, string list, ProductId list> =
    flow {
        let! api = Flow.env
        return! api.Get<ProductId list>(custId)
    }

let getProductInfo
    (productId: ProductId)
    : Flow<ApiClient, string list, ProductInfo> =
    flow {
        let! api = Flow.env
        return! api.Get<ProductInfo>(productId)
    }

let getPurchaseInfoWithoutTraverse
    (custId: CustId)
    : Flow<ApiClient, string list, ProductInfo list> =
    flow {
        let! productIds = getPurchaseIds custId

        let mutable productInfos = []

        for productId in productIds do
            let! productInfo = getProductInfo productId
            productInfos <- productInfo :: productInfos

        return List.rev productInfos
    }

let getPurchaseInfo
    (custId: CustId)
    : Flow<ApiClient, string list, ProductInfo list> =
    flow {
        let! productIds = getPurchaseIds custId
        return! Flow.traverse getProductInfo productIds
    }

// Executor

let executeApiFlow
    (apiFlow: Flow<ApiClient, string list, 'value>)
    : Result<'value, string list> =

    use api = new ApiClient()

    apiFlow
    |> Flow.catch (fun ex -> [ sprintf "Unexpected API exception: %s" ex.Message ])
    |> Flow.run api

// Example use

let result =
    CustId "C1"
    |> getPurchaseInfo
    |> executeApiFlow
```

## Entire asyncFlow example

```fsharp
open System

// API shape assumed here:
// member _.GetAsync<'a>(id: obj) : Async<Result<'a, string list>>
// static member ApiClient.ConnectAsync() : Async<ApiClient>

let getPurchaseIdsAsync
    (custId: CustId)
    : AsyncFlow<ApiClient, string list, ProductId list> =
    asyncFlow {
        let! api = AsyncFlow.env
        return! api.GetAsync<ProductId list>(custId)
    }

let getProductInfoAsync
    (productId: ProductId)
    : AsyncFlow<ApiClient, string list, ProductInfo> =
    asyncFlow {
        let! api = AsyncFlow.env
        return! api.GetAsync<ProductInfo>(productId)
    }

let getPurchaseInfoAsync
    (custId: CustId)
    : AsyncFlow<ApiClient, string list, ProductInfo list> =
    asyncFlow {
        let! productIds = getPurchaseIdsAsync custId
        return! AsyncFlow.traverse getProductInfoAsync productIds
    }

let executeApiAsyncFlow
    (apiFlow: AsyncFlow<ApiClient, string list, 'value>)
    : Async<Result<'value, string list>> =

    openApiAsync
    |> AsyncFlow.Runtime.useWithAcquireRelease closeApiAsync
        (fun api ->
            asyncFlow {
                let safeFlow =
                    apiFlow
                    |> AsyncFlow.catch (fun ex -> [ sprintf "Unexpected API exception: %s" ex.Message ])

                return! AsyncFlow.run api safeFlow
            })
    |> AsyncFlow.run ()

let resultAsync =
    CustId "C1"
    |> getPurchaseInfoAsync
    |> executeApiAsyncFlow
```

## Entire taskFlow example

```fsharp
open System
open System.Threading
open System.Threading.Tasks

// Domain

type CustId = CustId of string

type ProductId = ProductId of string

type ProductInfo =
    { ProductName: string }

// API shape assumed here:
// member _.GetAsync<'a>(id: obj, cancellationToken: CancellationToken) : Task<Result<'a, string list>>
// static member ApiClient.ConnectAsync(cancellationToken: CancellationToken) : Task<ApiClient>
type ApiClient private () =
    static member ConnectAsync(cancellationToken: CancellationToken) : Task<ApiClient> =
        task {
            // Open socket, authenticate, warm up connection, etc.
            return ApiClient()
        }

    member _.GetAsync<'a>
        (id: obj, cancellationToken: CancellationToken)
        : Task<Result<'a, string list>> =
        task {
            // Call Redis / HTTP / database / GraphQL / etc.
            return failwith "example"
        }

    member _.CloseAsync() : Task =
        task {
            // Close socket, flush buffers, release handles, etc.
            return ()
        }

let getPurchaseIdsTask
    (custId: CustId)
    : TaskFlow<ApiClient, string list, ProductId list> =
    taskFlow {
        let! api = TaskFlow.env
        let! ct = TaskFlow.Runtime.cancellationToken
        return! api.GetAsync<ProductId list>(custId, ct)
    }

let getProductInfoTask
    (productId: ProductId)
    : TaskFlow<ApiClient, string list, ProductInfo> =
    taskFlow {
        let! api = TaskFlow.env
        let! ct = TaskFlow.Runtime.cancellationToken
        return! api.GetAsync<ProductInfo>(productId, ct)
    }

let getPurchaseInfoTask
    (custId: CustId)
    : TaskFlow<ApiClient, string list, ProductInfo list> =
    taskFlow {
        let! productIds = getPurchaseIdsTask custId
        return! TaskFlow.traverse getProductInfoTask productIds
    }

let toApiError (ex: exn) : string list =
    [ sprintf "Unexpected API exception: %s" ex.Message ]

let executeApiTaskFlow
    (cancellationToken: CancellationToken)
    (apiFlow: TaskFlow<ApiClient, string list, 'value>)
    : Task<Result<'value, string list>> =

    openApiTask
    |> TaskFlow.Runtime.useWithAcquireRelease closeApiTask
        (fun api ->
            taskFlow {
                let! ct = TaskFlow.Runtime.cancellationToken

                let safeFlow =
                    apiFlow
                    |> TaskFlow.catch toApiError

                return! TaskFlow.run api ct safeFlow
            })
    |> TaskFlow.run () cancellationToken

let resultTask =
    CustId "C1"
    |> getPurchaseInfoTask
    |> executeApiTaskFlow CancellationToken.None
```

## Final take

The original post is still worth reading because it teaches the path to Reader from first principles.

FsFlow is what the application code can look like once you have accepted that lesson.

You keep the useful separation:

```text
Describe API work now.
Provide the API client later.
Run at the boundary.
```

But you remove the custom wrapper types and hand-written plumbing.

The result is still Reader-shaped, but it is also Result-aware, and in real code it can be Async-aware or Task-aware too.

For modern .NET, the task-based version is probably the default:

* task-based API calls;
* cancellation tokens supplied by `TaskFlow.Runtime.cancellationToken`;
* API acquisition handled with `TaskFlow.Runtime.useWithAcquireRelease`;
* unexpected exceptions mapped with `TaskFlow.catch`;
* execution delayed until `TaskFlow.run`.
