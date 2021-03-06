﻿
module CommandParsing

open System
open System.IO

open Common
open Common.Result
open Common.TimeZone

type IntervalCommand = {
    Start: DateTime
    End: DateTime
    FilterOneId: bool
}

type CommandType = Interval of IntervalCommand | OneId of string | OneIds of string list

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

let rec private parseInterval command args =    
    match args with
    | "-s" :: year :: month :: day :: rest -> 
        let cmd = {command with Start = new DateTime(int year, int month, int day)}
        parseInterval cmd rest
    | "-e" :: year :: month :: day :: rest -> 
        let cmd = {command with End = new DateTime(int year, int month, int day)}
        parseInterval cmd rest
    | "-id" :: _ ->
        {command with FilterOneId = true}
    | [] ->
        command
    | x -> failwithf "invalid arguments supplied %A" x

let private parseOneIds args =
    match args with
    | [id] -> 
        OneId id
    | rest ->
        OneIds rest

let private parseClientCommand args =
    match args with
    | email :: tz :: commandType :: rest ->
        let timeZone = match tz |> TimeZone.fromCode with
                       | Success t -> t
                       | Failure msg -> failwith msg
        match commandType with
        | "id" -> 
           { Command = parseOneIds rest; TimeZone = timeZone; Email = email}
        | "interval" -> 
           let emptyCommand = { Start = DateTime.Now.Subtract(TimeSpan.FromDays(30.0)); End = DateTime.Now; FilterOneId = false}
           { Command = CommandType.Interval (parseInterval emptyCommand rest); TimeZone = timeZone; Email = email} 
        | x -> failwithf "unknown command %s" x

    | x -> failwithf "invalid command %A" x

let private parseCommand args =
    match args with
    | conf :: "-o" :: file :: rest when File.Exists(conf) ->
        let command = parseClientCommand rest
        {ConfigFilePath = conf; Output = OutputType.File file; Command = command}
    | conf :: rest when File.Exists(conf) ->
        let command = parseClientCommand rest
        {ConfigFilePath = conf; Output = OutputType.Console; Command = command}
    | "help" :: ["tz"] ->
        let timeZoneCodes = TimeZone.timeZoneCodes 
                            |> List.map (fun (code, TimeZone name) -> sprintf "%s -> %s" code name)
                            |> List.reduce (fun c n -> c + Environment.NewLine + n)    
        failwith timeZoneCodes
    | ["help"] 
    | [] ->
        let message = [ "";
                        "<config-file> [-o <output-file>] <email> <time-zone-code> {id [|<ids>|] | interval [-s <year> <month> <day>] [-e <year> <month> <day>] [-id]}" ;
                        "";
                        "<config-file>: path to a file (json)formatted like { Url: xxx, User: xxx, Password: xxx}";
                        "-o <output-file>: path to the json-formatted file to be written to, if the -o flag is not present results will be written to console";
                        "<email>: the email whose calendar will be queried";
                        "<time-zone-code>: time zone to visualize the dates. Type 'help tz' to see time zones info";
                        "====================================";
                        "Filter by OneBodyIds";
                        "====================================";
                        "id [|ids|]: only appointments having the OneBodyId field in the [|ids|] list will be returned";
                        "[|ids|]: list of (at least one) OneBodyIds to filter by"
                        "";
                        "====================================";
                        "Filter by interval";
                        "====================================";
                        "interval [-s <year> <month> <day>] [-e <year> <month> <day>] [-id]: only appointments in the interval will be returned";
                        "-s ...: Interval starting date. If not specified will be 30 days before current date, otherwise <year>, <month>, <day> are required";
                        "-e ...: Interval ending date. If not specified will be the current date, otherwise <year>, <month>, <day> are required";
                        "-id: Only appointments in the given interval with the OneBodyId field set"
                        ] 
                      |> List.reduce (fun c n -> c + Environment.NewLine + n)
        
        failwith message
    | conf :: rest when not(rest |> List.isEmpty) ->
        failwithf "invalid configuration file %s" conf
    | _ -> 
        failwithf "Invalid arguments %s Type help for more details" Environment.NewLine

let tryParse args =
    Result.run (fun () -> parseCommand args)

