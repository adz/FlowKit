namespace FsFlow.Tests

open Microsoft.FSharp.Reflection
open FsFlow
open FsFlow.Tests.TestSupport
open Swensen.Unquote
open Xunit

module CapsPrimitiveTests =
    [<Fact>]
    let ``CAPS primitives are public and nominally shaped`` () =
        let needsType = typeof<Needs<IDeviceClient>>
        let tokenType = typeof<Env<IDeviceClient>>
        let projectedType = typeof<Env<IDeviceClient, int>>

        let tokenInterfaceReference =
            { new IDeviceClient with
                member _.Name = "dev" }

        test <@ needsType.IsInterface @>
        test <@ needsType.IsPublic @>
        test <@ tokenType.IsPublic @>
        test <@ projectedType.IsPublic @>
        test <@ tokenType.IsValueType @>
        test <@ projectedType.IsValueType @>
        test <@ FSharpType.IsUnion tokenType @>
        test <@ FSharpType.IsUnion projectedType @>

        let tokenCase: UnionCaseInfo = FSharpType.GetUnionCases tokenType |> Array.exactlyOne
        let projectedCase: UnionCaseInfo = FSharpType.GetUnionCases projectedType |> Array.exactlyOne

        test <@ tokenCase.Name = "Env" @>
        test <@ projectedCase.Name = "Env" @>
        let tokenFields = tokenCase.GetFields()
        let projectedFieldsInfo = projectedCase.GetFields()

        test <@ tokenFields.Length = 0 @>
        test <@ projectedFieldsInfo.Length = 1 @>

        let projectedFieldType = projectedFieldsInfo[0].PropertyType

        test <@ projectedFieldType.IsGenericType @>
        test <@ projectedFieldType.GetGenericTypeDefinition().FullName = "Microsoft.FSharp.Core.FSharpFunc`2" @>

        let projectedFieldArguments = projectedFieldType.GetGenericArguments()

        test <@ projectedFieldArguments[0] = typeof<IDeviceClient> @>
        test <@ projectedFieldArguments[1] = typeof<int> @>

        let tokenValue = FSharpValue.MakeUnion(tokenCase, [||])

        let projectedValue =
            FSharpValue.MakeUnion(projectedCase, [| box (fun (client: IDeviceClient) -> client.Name.Length) |])

        let tokenCase', tokenFields = FSharpValue.GetUnionFields(tokenValue, tokenType)
        let projectedCase', projectedFields = FSharpValue.GetUnionFields(projectedValue, projectedType)

        test <@ tokenCase'.Name = tokenCase.Name @>
        test <@ projectedCase'.Name = projectedCase.Name @>
        test <@ tokenFields.Length = 0 @>
        test <@ projectedFields.Length = 1 @>

        let projected = projectedFields[0] :?> (IDeviceClient -> int)

        test <@ projected tokenInterfaceReference = 3 @>

        let _ : Needs<IDeviceClient> option = None
        let _ : Env<IDeviceClient> option = None
        let _ : Env<IDeviceClient, int> option = None

        ()
