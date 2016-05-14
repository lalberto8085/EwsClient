
module CommandParsing

open System

type TZ = CommandTimeZone of string

let public Pacific = CommandTimeZone "Pacific Standard Time"
let public Central = CommandTimeZone "Central Standard Time"
let public Eastern = CommandTimeZone "Eastern Standard Time"
let public Atlantic = CommandTimeZone "Atlantic Standard Time"
let public Mountain = CommandTimeZone "Mountain Standard Time"
let public Alaskan = CommandTimeZone "Alaskan Standard Time"
let public Hawaiian = CommandTimeZone "Hawaiian Standard Time"
let public Samoa = CommandTimeZone "Samoa Standard Time"

let mapped = ["EST", Eastern; "AKT", Alaskan;  "SST", Samoa;
              "CST", Central; "MST", Mountain; "ATT", Atlantic;
              "PST", Pacific; "HST", Hawaiian;]
              |> Map.ofList

let convert (CommandTimeZone tz) =
    TimeZoneInfo.FindSystemTimeZoneById(tz)

type IntervalCommand = {
    Start: DateTime
    End: DateTime
    FilterOneId: bool
}

type OneIdCommand = OneId of string
                  | OneIds of string list

type CommandType = Interval of IntervalCommand | OneId of OneIdCommand


type CommandOptions = {
    Command: CommandType
    Email: string
    TimeZone: TZ
}

let rec parseInterval command args =    
    match args with
    | "-s" :: year :: month :: day :: rest -> 
        let cmd = {command with Start = new DateTime(int year, int month, int day)}
        parseInterval cmd rest
    | "-e" :: year :: month :: day :: rest -> 
        let cmd = {command with End = new DateTime(int year, int month, int day)}
        parseInterval cmd rest
    | "-id" :: rest ->
        {command with FilterOneId = true}
    | [] ->
        command
    | x -> failwithf "invalid arguments supplied %A" x

let parseOneIds args =
    match args with
    | "-id" :: id :: [] -> 
        OneIdCommand.OneId id
    | "-ids" :: rest ->
        OneIdCommand.OneIds rest
    | x -> failwithf "invalid argument supplied %A" x

let parseCommand args =
    match args with
    | email :: tz :: commandType :: rest ->
        let timeZone = match mapped |> Map.tryFind tz with
                       | Some t -> t
                       | None -> failwithf "unknown time zone %s" tz
        match commandType with
        | "id" -> 
           { Command = CommandType.OneId (parseOneIds rest); TimeZone = timeZone; Email = email}
        | "interval" -> 
           let emptyCommand = { Start = DateTime.Now.Subtract(TimeSpan.FromDays(30.0)); End = DateTime.Now; FilterOneId = false}
           { Command = CommandType.Interval (parseInterval emptyCommand rest); TimeZone = timeZone; Email = email} 
        | x -> failwithf "unknown command %s" x

    | x -> failwithf "invalid command %A" x

