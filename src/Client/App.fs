module App

open Elmish
open Elmish.React

open Fable.Helpers.React
module R = Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

open Fable.Websockets.Client
open Fable.Websockets.Protocol
open Fable.Websockets.Elmish
open Fable.Websockets.Elmish.Types

open Switches
open Shared

open Fulma
open Fulma.Layouts
open Fulma.Elements
open Fulma.Components
open Fulma.BulmaClasses

type ConnectionState = NotConnected | Connected    

type Model = {
    switches: Switches
    connectionState: ConnectionState 
    socket: SocketHandle<Commands,Events>
  } with
    static member empty = {
        switches=[]
        connectionState=NotConnected
        socket=SocketHandle.Blackhole()
    }

type AppMsg =
  | Init
  | Toggle of Toggle

type MsgType = Msg<Commands,Events,AppMsg>

let appMsgUpdate (msg:AppMsg) (m : Model) =
    match msg with
    | Init ->
        m, (Cmd.tryOpenSocket "ws://localhost:8080/wsapi")
    | Toggle toggle -> 
        m, (Cmd.ofSocketMessage m.socket (SetSwitch toggle))
    //| _ -> m, Cmd.none

let serverEventUpdate msg (m : Model) =
    printfn "event %A" msg
    match msg with 
    | Switches sw -> { m with switches=sw }, Cmd.none

let inline update msg prevState = 
    printfn "msg %A" msg
    match msg with
    | ApplicationMsg amsg -> appMsgUpdate amsg prevState
    | WebsocketMsg (socket, Opened) -> 
        ({ prevState with socket = socket; connectionState = Connected }, Cmd.none)    
    //| WebsocketMsg (socket, Closed) -> ({ prevState with socket = socket; connectionState = Connected }, Cmd.none)    
    | WebsocketMsg (_, Msg socketMsg) -> (serverEventUpdate socketMsg prevState)
    | _ -> (prevState, Cmd.none)


let renderSwitch sw (dispatch:MsgType->unit) =
  let onClick _ =
    let nextState = match sw.state with On -> Off | Off -> On
    dispatch (ApplicationMsg (Toggle(sw.channel,nextState)))
    ()
  [ Button.button_btn
      [ Button.onClick onClick; (if sw.state = On then Button.isPrimary else Button.isDark) ]
      [ str sw.name ]
    str (sprintf "%A" sw.mode)
  ]

let show model = 
    match model.switches with
    | [] -> "Loading..."
    | x -> sprintf "%A" x

let button txt onClick = 
  Button.button_btn
    [ Button.isFullWidth
      Button.isPrimary
      Button.onClick onClick ] 
    [ str txt ]

let view model dispatch =
  div []
    [ Navbar.navbar [ Navbar.customClass "is-primary" ]
        [ Navbar.item_div [ ]
            [ Heading.h2 [ ]
                [ str "Raspberry F#" ] ] ]

      Container.container []
        [ //Content.content [ Content.customClass Bulma.Level.Item.HasTextCentered ] 
          //  [ Heading.h3 [] [ str ("Switches: "); str (show model) ] ]
          //Columns.columns [] 
          Content.content []
            (model.switches |> List.map (fun sw -> Column.column [] (renderSwitch sw dispatch)))
        ]

      Footer.footer [ ]
        [ Content.content [ Content.customClass Bulma.Level.Item.HasTextCentered ]
            [ str "footer text" ] ] ]

let init () = Model.empty, (Cmd.ofMsg (ApplicationMsg Init)) 

#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
