
module ConfigParser

open System.IO
open Newtonsoft.Json
open CommandParsing

type ConfigurationInfo = {
    Url: string
    User: string
    Password: string
}

type ConfigurationCommand = {
    Config: ConfigurationInfo
    Output : OutputType
    Command: ClientOptions
}

let private parseConfig (file: string) =
    
    use reader = new StreamReader(file)
    let data = reader.ReadToEnd()

    JsonConvert.DeserializeObject<ConfigurationInfo>(data)

let parseConfiguration (command: CommandOptions) =
    let config = parseConfig command.ConfigFilePath
    {Config = config; Output = command.Output; Command= command.Command}
