open System.IO
open System.Net

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open Elmish
open Fable.Websockets.Suave
open Fable.Websockets.Protocol

open Shared
open App

let dirPath = Path.Combine("..","Client") |> Path.GetFullPath 
let port = 8085us

let config =
  { defaultConfig with 
      homeFolder = Some dirPath
      bindings = [ HttpBinding.create HTTP (IPAddress.Parse "0.0.0.0") port ] }

type ServerState = { 
    appState: App.State
    clients: Socket list
}

let initialize () =
    let appState, cmd = App.init ()
    let initialState = { appState = appState; clients = [] }
    initialState, cmd


let update msg state =
    let passState = 
        match msg with
        | Client(Connected client) ->
            Some { state with clients = client::state.clients }
        | Client(Disconnected client) ->
            Some { state with clients = state.clients |> List.where (fun c -> c <> client) }
        | Send evt ->
            state.clients |> List.iter (fun c -> c.send evt)
            None
        | SendTo (sock,evt) ->
            sock.send evt
            None
        | _ -> Some state
    match passState with
        | Some state -> // dispatch msg also to App
            printfn "srvMsg %A" msg
            let appState', cmd = App.update msg state.appState
            { state with appState = appState' }, cmd
        | None -> state, Cmd.none

let webServer (dispatch:App.Msg -> unit) = 

    let onWebsocketConnection closeHandle socketEventSource socketEventSink = 

        let client = { send = socketEventSink }

        let handleIncomming (socketEvent:WebsocketEvent<Commands>) =
            printfn "ws: %A" socketEvent
            match socketEvent with
            | WebsocketEvent.Msg msg -> Command(msg) |> dispatch
            | WebsocketEvent.Opened -> Client (Connected client) |> dispatch
            | WebsocketEvent.Closed _
            | WebsocketEvent.Error
            | WebsocketEvent.Exception _ ->
                Client (Disconnected client) |> dispatch

        socketEventSource |> Observable.subscribe handleIncomming

    let webPart =
        choose [
            Files.browseHome
            path "/wsapi" >=> websocket<Commands,Events> onWebsocketConnection
            GET >=> choose [ (path "/") >=> OK "Hello world" ]
        ]

    startWebServer config webPart

let webServerSubscription _ =
    let subscription dispatch =
        printfn "Registering webserver, got dispatch..."
        webServer dispatch
        dispatch (UpdateElectricityTariff false)
    Cmd.ofSub subscription

let mutable first = true
let view state dispatch =
    printfn "%A" state
    if first then
        first <- false
        dispatch InitializeHw

Program.mkProgram initialize update view
|> Program.withSubscription webServerSubscription
|> Program.run
