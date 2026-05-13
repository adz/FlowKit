open System
open FsFlow.Capabilities.Core.Examples

module Runner =
    let run () =
        CoreCapabilitiesExample.run()

[<EntryPoint>]
let main _ =
    Runner.run()
    0
