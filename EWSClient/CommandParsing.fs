
module CommandParsing

open System
open System.IO

type TZ = CommandTimeZone of string

type IntervalCommand = {
    Start: DateTime
    End: DateTime
    FilterOneId: bool
}

type OneIdCommand = OneId of string
                  | OneIds of string list

type CommandType = Interval of IntervalCommand | OneId of OneIdCommand


type ClientOptions = {
    Command: CommandType
    Email: string
    TimeZone: TZ
}

type OutputType = Console | File of string

type CommandOptions = {
    ConfigFilePath: string
    Output : OutputType
    Command: ClientOptions
}

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

let rec private parseInterval command args =    
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

let private parseOneIds args =
    match args with
    | "-id" :: id :: [] -> 
        OneIdCommand.OneId id
    | "-id" :: rest ->
        OneIdCommand.OneIds rest
    | x -> failwithf "invalid argument supplied %A" x

let private parseClientCommand args =
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

let parseCommand args =
    match args with
    | conf :: "-o" :: file :: rest when File.Exists(conf) ->
        let command = parseClientCommand rest
        {ConfigFilePath = conf; Output = OutputType.File file; Command = command}
    | conf :: rest when File.Exists(conf) ->
        let command = parseClientCommand rest
        {ConfigFilePath = conf; Output = OutputType.Console; Command = command}
    | conf :: rest ->
        failwithf "invalid configuration file %s" conf
    | _ -> 
        failwith "invalid arguments"


