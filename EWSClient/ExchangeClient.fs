
module ExchangeClient

open System
open System.Net
open Microsoft.Exchange.WebServices.Data

type Client(endpoint, user, pwd) =

    let properties = 
        new PropertySet(ItemSchema.Id,
                        ItemSchema.Subject,
                        ItemSchema.Categories,
                        AppointmentSchema.Start,
                        AppointmentSchema.End,
                        AppointmentSchema.Location,
                        AppointmentSchema.IsAllDayEvent,
                        AppointmentSchema.IsRecurring)

    let getFolderId email = new FolderId(WellKnownFolderName.Calendar, new Mailbox(email))

    let getClient timeZone =
        let client = ExchangeService(ExchangeVersion.Exchange2010_SP2, timeZone)
        client.Url <- endpoint
        client.Credentials <- ExchangeCredentials.op_Implicit(new NetworkCredential())

        34

    member this.GetAppointments(email) =
        35
