
module ExchangeClient

open System
open System.Net
open Microsoft.Exchange.WebServices.Data

type AppointmentDto = {
    EwsId: string
    OneId: string option
    Subject: string option
    Start: DateTime
    End: DateTime
    Categories: string list
    IsAllDay: bool
    IsRecurring: bool
    Location: string
    AppointmentType: string option
    Body: string option
}

let public OneBodyId = new ExtendedPropertyDefinition(DefaultExtendedPropertySet.PublicStrings,
                                                      "AppointmentID", MapiPropertyType.String)

let public AppointmentType = new ExtendedPropertyDefinition(DefaultExtendedPropertySet.PublicStrings,
                                                            "AppointmentType", MapiPropertyType.String)

type Client(endpoint, user, pwd) =

    let properties = 
        new PropertySet(ItemSchema.Id,
                        ItemSchema.Subject,
                        ItemSchema.Categories,
                        AppointmentSchema.Start,
                        AppointmentSchema.End,
                        AppointmentSchema.Location,
                        AppointmentSchema.IsAllDayEvent,
                        AppointmentSchema.IsRecurring,
                        OneBodyId,
                        AppointmentType)

    let loadBody (app: Appointment) =
        let extendedProperties = new PropertySet(properties)
        extendedProperties.Add(ItemSchema.Body)
        app.Load(extendedProperties)
        app

    let loadExtProp (property: ExtendedPropertyDefinition) (app: Appointment) =
        let mutable result = ""
        if app.TryGetProperty(property, ref result) then
            Some result
        else
            None

    let toDto includeBody app =
        let loaded = if includeBody 
                     then loadBody app
                     else app
        {
            EwsId = loaded.Id.UniqueId
            OneId = loadExtProp OneBodyId loaded
            Subject = loaded.Subject |> Option.ofObj
            Start = loaded.Start
            End = loaded.End
            Categories = loaded.Categories |>  Seq.toList
            IsAllDay = loaded.IsAllDayEvent
            IsRecurring = loaded.IsRecurring
            Location = loaded.Location
            AppointmentType = loadExtProp OneBodyId loaded
            Body = if includeBody then loaded.Body.Text |> Option.ofObj else None
        }

    let getClient timeZone =
        let client = ExchangeService(ExchangeVersion.Exchange2010_SP2, timeZone)
        client.Url <- endpoint
        client.Credentials <- ExchangeCredentials.op_Implicit(new NetworkCredential())

        client

    let getFolderId email = new FolderId(WellKnownFolderName.Calendar, new Mailbox(email))

    let getInInterval email startDate endDate timeZone =
        let client = getClient timeZone
        
        let view = new CalendarView(startDate, endDate)
        view.PropertySet <- properties

        let folder = getFolderId email

        client.FindAppointments(folder, view) 


    member this.GetAppointments(email, startDate, endDate, timeZone) =
        getInInterval email startDate endDate timeZone |> Seq.map (toDto false)


    member this.GetAppointmentsWithOneBodyId(email, startDate, endDate, timeZone) =
        let mutable obj = ""
        getInInterval email startDate endDate timeZone
                |> Seq.filter (fun a -> a.TryGetProperty(OneBodyId, ref obj))
                |> Seq.map (toDto false)


    member this.GetByOneId(email, oneId: string, timeZone) =
        let filter = new SearchFilter.IsEqualTo(OneBodyId, oneId)
        let folder = getFolderId email
        let view = new ItemView(1)
        view.PropertySet <- properties

        let client = getClient timeZone

        client.FindItems(folder, filter, view) 
        |> Seq.cast<Appointment> 
        |> Seq.tryHead
        |> Option.map (toDto true)


    member this.GetByOneIds(email, timeZone, oneIds: string list) =
        let filters = oneIds |> Seq.map (fun id -> new SearchFilter.IsEqualTo(OneBodyId, id))
                             |> Seq.cast<SearchFilter>
                             |> Seq.toArray
        let filter = new SearchFilter.SearchFilterCollection(LogicalOperator.Or, filters)
        let folder = getFolderId email
        let view = new ItemView(oneIds |> List.length)
        view.PropertySet <- properties

        let client = getClient timeZone

        client.FindItems(folder, filter, view) 
        |> Seq.cast<Appointment> 
        |> Seq.map (toDto false)
        
