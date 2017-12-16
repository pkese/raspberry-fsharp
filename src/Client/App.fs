module App

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Import.Browser
open Fable.Websockets.Client
open Fable.Websockets.Protocol
open Fable.Websockets.Elmish
open Fable.Websockets.Elmish.Types

open Shared

open Fulma
open Fulma.Layouts
open Fulma.Elements
open Fulma.Components
open Fulma.BulmaClasses

let websocketUrl = 
    match location.port with
    | "" ->   sprintf "ws://%s/wsapi"    location.hostname
    | port -> sprintf "ws://%s:%s/wsapi" location.hostname port

type State = {
    leds: Leds
    socket: SocketHandle<Command,Notification> option
  } with
    member this.SocketCmd msg =
        match this.socket with
        | Some socket -> Cmd.ofSocketMessage socket msg
        | None -> Cmd.none

type AppMsg =
  | Connect
  | SelectTrigger of Channel * string

type MsgType = Msg<Command,Notification,AppMsg>

let onAppMsg (msg:AppMsg) (state:State) = // handle internal App messages
    match msg with
    | Connect -> state, (Cmd.tryOpenSocket websocketUrl)
    | SelectTrigger (channel,trigger) -> state, (state.SocketCmd (SetTrigger (channel,trigger)))

let onServerNotification (msg:Notification) (state:State) = // handle shared socket message updates
    match msg with 
    | Leds leds -> { state with leds=leds }, Cmd.none

let inline update msg state = 
    match msg with
    | ApplicationMsg amsg -> onAppMsg amsg state
    | WebsocketMsg (socket, Opened) -> { state with socket = Some socket }, Cmd.none
    | WebsocketMsg (_, WebsocketEvent.Closed _) -> { state with socket = None }, (Cmd.ofMsg (ApplicationMsg Connect)) // todo: add some incremental delay
    | WebsocketMsg (_, Msg socketMsg) -> onServerNotification socketMsg state
    | _ -> state, Cmd.none

let init () = 
    let initialState = { leds=[]; socket=None }
    let startCommand = Cmd.ofMsg (ApplicationMsg Connect)
    initialState, startCommand 

// ---------------------- render html -------------------------------

let renderSwitches led (dispatch:AppMsg->unit) =
  let renderTriggerSwitch trigger =
    Button.button_btn
      [ Button.onClick (fun _ -> dispatch (SelectTrigger(led.channel,trigger)))
        Button.props [Style [Margin 2]]
        (if led.trigger = trigger then Button.isPrimary else Button.isDark)]
      [ str trigger ]

  led.triggers |> List.map renderTriggerSwitch

let footerComponents =
  let intersperse sep ls =
    List.foldBack (fun x -> function
      | [] -> [x]
      | xs -> x::sep::xs) ls []

  let components =
    [ 
      "Suave.IO", "http://suave.io" 
      "Fable"   , "http://fable.io"
      "Elmish"  , "https://fable-elmish.github.io/"
      "Fulma"   , "https://mangelmaxime.github.io/Fulma" 
    ]
    |> List.map (fun (desc,link) -> a [ Href link ] [ str desc ] )
    |> intersperse (str ", ")
    |> span [ ]

  p [ ]
    [ strong [] [ str "SAFE Template" ]
      str " powered by: "
      components]

let view model dispatch =
  let dispatch msg = dispatch (ApplicationMsg msg)
  div []
    [ Navbar.navbar [ Navbar.customClass "is-primary" ]
        [ Navbar.item_div [ ]
            [ Heading.h2 [ ]
                [ str "Raspberry F#" ] ] ]

      Container.container [Container.props [Style [MarginTop 30]]] (
        if model.socket.IsSome
        then
            model.leds |> List.map (fun led -> 
            Content.content [ Content.customClass Bulma.Level.Item.HasTextCentered ] 
              [
                Heading.h3 [] [ str (sprintf "%s led" led.color) ]
                Column.column [] (renderSwitches led dispatch)
              ])
        else
            [ Content.content [ Content.customClass Bulma.Level.Item.HasTextCentered ] 
                [ Heading.h3 [] [ str "not connected..." ]]]

      )

      Footer.footer [ ]
        [ Content.content [ Content.customClass Bulma.Level.Item.HasTextCentered ]
            [ footerComponents ] ] ]


// ---------------------- start -------------------------------

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
