
module ExchangeClient

open System
open System.Net
open Microsoft.Exchange.WebServices.Data

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

    let getFolderId email = new FolderId(WellKnownFolderName.Calendar, new Mailbox(email))

    let getClient timeZone =
        let client = ExchangeService(ExchangeVersion.Exchange2010_SP2, timeZone)
        client.Url <- endpoint
        client.Credentials <- ExchangeCredentials.op_Implicit(new NetworkCredential())

        client

    member this.GetAppointments(email, startDate, endDate, timeZone) =
        let client = getClient timeZone
        
        let view = new CalendarView(startDate, endDate)
        view.PropertySet <- properties

        let folder = getFolderId email

        client.FindAppointments(folder, view)


    member this.GetAppointmentsWithOneBodyId(email, startDate, endDate, timeZone) =
        let mutable obj = ""
        this.GetAppointments(email, startDate, endDate, timeZone)
                |> Seq.filter (fun a -> a.TryGetProperty(OneBodyId, ref obj))


    member this.GetByOneId(email, oneId, timeZone) =
        let filter = new SearchFilter.IsEqualTo(OneBodyId, oneId)
        let folder = getFolderId email
        let view = new ItemView(1)
        view.PropertySet <- properties

        let client = getClient timeZone

        let appointment = client.FindItems(folder, filter, view) |> Seq.cast<Appointment> |> Seq.tryHead
        match appointment with
        | None -> None
        | Some app -> 
            let extendedProperties = new PropertySet(properties)
            extendedProperties.Add(ItemSchema.Body)
            app.Load(extendedProperties)
            Some app

    member this.GetByOneIds(email, timeZone, oneIds: string list) =
        let filters = oneIds |> Seq.map (fun id -> new SearchFilter.IsEqualTo(OneBodyId, id))
                             |> Seq.cast<SearchFilter>
                             |> Seq.toArray
        let filter = new SearchFilter.SearchFilterCollection(LogicalOperator.Or, filters)
        let folder = getFolderId email
        let view = new ItemView(oneIds |> List.length)
        view.PropertySet <- properties

        let client = getClient timeZone

        client.FindItems(folder, filter, view) |> Seq.cast<Appointment> |> Seq.toList
        
