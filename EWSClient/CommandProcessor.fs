
module CommandProcessor

open System
open System.IO
open Newtonsoft.Json

open Common
open CommandParsing
open ConfigParser

let run (command: ConfigurationCommand) =
    
    let timeZone = command.Command.TimeZone 
                   |> TimeZone.toTimeZoneInfo
                   |> Result.fold (Funcs.identity) (raise)

    let output = match command.Output with
                 | File s -> 
                    let writer = new StreamWriter(s, false)
                    (fun (data: string) -> writer.Write(data); writer.Close())
                 | Console -> (fun (data:string) -> printfn "%s" data)

    let client = ExchangeClient.Client(Uri(command.Config.Url), command.Config.User, command.Config.Password)

    let result = match command.Command.Command with
                 | Interval { FilterOneId = oneIds; Start = eStart; End = eEnd } ->
                    if oneIds then
                        client.GetAppointmentsWithOneBodyId(command.Command.Email, eStart, eEnd, timeZone)
                    else
                        client.GetAppointments(command.Command.Email, eStart, eEnd, timeZone)
                 | OneId id ->
                    match client.GetByOneId(command.Command.Email, id, timeZone) with
                    | Some app -> seq {yield app}
                    | None -> failwith "appointment not found"
                 | OneIds ids ->
                    client.GetByOneIds(command.Command.Email, timeZone, ids)

    result |> JsonFormatting.serialize |> output
                    
