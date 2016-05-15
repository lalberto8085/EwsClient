
module Common

module Funcs =

    let identity x = x

module Result = 

    open System

    type R<'s, 'f> = Success of 's | Failure of 'f

    /// runs the given func and wraps any exception in Failure
    let run (func: unit -> 'a) =
        try
            func() |> Success
        with
        | ex -> ex |> Failure

    /// applies one of the passed functions according to the result's state
    let fold success failure result =
        match result with
        | Success x -> success x
        | Failure x -> failure x

    /// runs one of the passed functions in the inner value of the result and returns the wrapped computation
    let map successFunc failureFunc result =
        result |> fold (successFunc >> Success) (failureFunc >> Failure)

    /// runs the function and returns the wrapped value only if its in Success status
    let mapSuccess func result =
        map func Funcs.identity result

    /// runs the function and returns the wrapped value only if its in Failure status
    let mapFailure func result =
        map Funcs.identity func result


module TimeZone = 

    open System

    type TZ = TimeZone of string

    let pacific = TimeZone "Pacific Standard Time"
    let central = TimeZone "Central Standard Time"
    let eastern = TimeZone "Eastern Standard Time"
    let atlantic = TimeZone "Atlantic Standard Time"
    let mountain = TimeZone "Mountain Standard Time"
    let alaskan = TimeZone "Alaskan Standard Time"
    let hawaiian = TimeZone "Hawaiian Standard Time"
    let samoa = TimeZone "Samoa Standard Time"

    let timeZoneCodes = ["EST", eastern; "AKT", alaskan;  "SST", samoa;
                         "CST", central; "MST", mountain; "ATT", atlantic;
                         "PST", pacific; "HST", hawaiian;]

    let timeZoneCodeMap = timeZoneCodes |> Map.ofList

    /// converts a TimeZone to a System.TimeZoneInfo wrapped in a result
    let toTimeZoneInfo (TimeZone tz) =
        Result.run (fun() -> TimeZoneInfo.FindSystemTimeZoneById(tz))

    /// get a TimeZone from its code
    let fromCode code =
        Result.run (fun() -> timeZoneCodeMap |> Map.find code)




