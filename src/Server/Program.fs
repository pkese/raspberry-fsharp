open System.IO

open System.Net

open Suave
open Suave.Operators

open Shared

let path = Path.Combine("..","Client") |> Path.GetFullPath 
let port = 8085us

let config =
  { defaultConfig with 
      homeFolder = Some path
      bindings = [ HttpBinding.create HTTP (IPAddress.Parse "0.0.0.0") port ] }

let getCounter () : Async<Counter> = async { return 42 }

let init : WebPart = 
  let api = 
    { getCounter = getCounter }
  let routeBuilder typeName methodName = 
    sprintf "/api/%s/%s" typeName methodName
  Fable.Remoting.Suave.FableSuaveAdapter.webPartWithBuilderFor api routeBuilder

let webPart =
  choose [
    init
    Files.browseHome
  ]

startWebServer config webPart