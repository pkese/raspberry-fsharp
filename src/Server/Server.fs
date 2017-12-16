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

type State = { 
    appState: App.State
    clients: ClientSocket list
}

let initialize () =
    let appState, cmd = App.init ()
    { appState=appState; clients=[] }, cmd

let update msg state =
    let passStateToApp = 
        match msg with
        | Client (Connected client) ->
            Some { state with clients = client::state.clients }
        | Client (Disconnected client) ->
            Some { state with clients = state.clients |> List.where (fun c -> c <> client) }
        | Client (SendAll evt) ->
            state.clients |> List.iter (fun c -> c.send evt)
            None
        | Client (Send (socket,evt)) ->
            socket.send evt
            None
        | _ -> Some state
    match passStateToApp with
        | Some state -> // dispatch msg also to App
            let appState', cmd = App.update msg state.appState
            { state with appState = appState' }, cmd
        | None -> state, Cmd.none

let onWebsocketConnect dispatch closeHandle socketEventSource socketEventSink = 

    let socket = { send = socketEventSink }

    let handleIncommingEvents : WebsocketEvent<Command> -> unit = function
        | WebsocketEvent.Msg msg -> Command(socket,msg) |> dispatch
        | WebsocketEvent.Opened -> Client (Connected socket) |> dispatch
        | WebsocketEvent.Closed _ -> Client (Disconnected socket) |> dispatch
        | WebsocketEvent.Error -> () // ...
        | WebsocketEvent.Exception e -> printfn "Socket exception: %A" e

    socketEventSource |> Observable.subscribe handleIncommingEvents

let webServer (dispatch:App.AppMsg -> unit) = 
    startWebServer config (
        choose [
            path "/wsapi" >=> websocket<Command,Notification> (onWebsocketConnect dispatch)
            GET >=> path "/" >=> Files.file (Path.Combine(dirPath,"index.html"))
            GET >=> Files.browseHome
            RequestErrors.NOT_FOUND "Page not found." 
        ])

let webServerSubscription _ = Cmd.ofSub webServer

Program.mkProgram initialize update (fun state _ -> printfn "%A" state)
|> Program.withSubscription webServerSubscription
|> Program.run
