open System
open System.IO
open System.Net

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open Shared

let dirPath = Path.Combine("..","Client") |> Path.GetFullPath 
let port = 8085us

let config =
  { defaultConfig with 
      homeFolder = Some dirPath
      bindings = [ HttpBinding.create HTTP (IPAddress.Parse "0.0.0.0") port ] }



let init : WebPart = 
    let api = {
        getSwitches = App.getSwitches
        setSwitch = App.setSwitch
        setMode = App.setMode
    }
    let routeBuilder typeName methodName = 
      sprintf "/api/%s/%s" typeName methodName
    Fable.Remoting.Suave.FableSuaveAdapter.webPartWithBuilderFor api routeBuilder

let webPart =
    choose [
        init
        Files.browseHome
        GET >=> choose
          [
            (path "/") >=> OK "Hi"
          ]
    ]

startWebServer config webPart