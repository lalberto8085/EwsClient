
module JsonFormatting

open System
open Microsoft.FSharp.Reflection
open Newtonsoft.Json

type private OptionConverter() =
    inherit JsonConverter() 

    override this.CanConvert(t: Type) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>
      
    override this.ReadJson(reader, objectType, existingValue, serializer) =
        if reader.TokenType = JsonToken.Null then None :> obj
        else
            let innerType = objectType.GetGenericArguments().[0]
            let value = serializer.Deserialize(reader, innerType)
            let cases = FSharpType.GetUnionCases(objectType)
            if value = null 
                then FSharpValue.MakeUnion(cases.[0], [||])
                else FSharpValue.MakeUnion(cases.[1], [|value|])


    override this.WriteJson(writer, value, serializer) =
        let value = 
            if value = null then JsonToken.Null :> obj
            else
                let _, fields = FSharpValue.GetUnionFields(value, value.GetType())
                fields.[0]
        serializer.Serialize(writer, value)

let settings =
    let settings = new JsonSerializerSettings()
    settings.Converters <- [|OptionConverter()|]
    settings.Formatting <- Formatting.Indented
    settings


let serialize data =
    JsonConvert.SerializeObject(data, settings)

let deserialize<'a> data =
    JsonConvert.DeserializeObject<'a>(data, settings)
