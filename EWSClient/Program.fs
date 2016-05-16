
open Common

[<EntryPoint>]
let main argv = 

    let processCommand = ConfigParser.parseConfiguration >> CommandProcessor.run

    argv |> List.ofArray 
         |> CommandParsing.tryParse 
         |> Result.fold processCommand (fun ex -> printfn "%s" ex.Message)

    0 // return an integer exit code
