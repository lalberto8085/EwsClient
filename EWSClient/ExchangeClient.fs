
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

        let folder = new FolderId(WellKnownFolderName.Calendar, new Mailbox(email))

        client.FindAppointments(folder, view)
