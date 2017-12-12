module App

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

open Switches
open Shared

open Fulma
open Fulma.Layouts
open Fulma.Elements
open Fulma.Components
open Fulma.BulmaClasses

type Model = {
    switches: Switches
  } with
    static member empty = { switches=[] }

type Msg =
  | Init of Result<Switches, exn>
  | UpdateSwitchState of Result<Switches, exn>
  | Toggle of Toggle


let swApi = 
  let routeBuilder typeName methodName = 
    sprintf "/api/%s/%s" typeName methodName
  Fable.Remoting.Client.Proxy.createWithBuilder<SwApp> routeBuilder

let init () = 
  let model = Model.empty
  let cmd =
    Cmd.ofAsync 
      swApi.getSwitches
      () 
      (Ok >> Init)
      (Error >> Init)
  model, cmd

let update msg (model : Model) : (Model*Cmd<Msg>)=
    match model, msg with
    | m, Init (Ok sw) -> printf "init: %A" sw; { m with switches=sw }, Cmd.none
    | m, Init (Error e) -> printf "init error: %A" e; m, Cmd.none
    | m, UpdateSwitchState (Ok sw) -> { m with switches=sw }, Cmd.none
    | m, UpdateSwitchState (Error e) -> printf "init error: %A" e; m, Cmd.none
    | m, Toggle toggle -> 
        let cmd =
            Cmd.ofAsync 
              swApi.setSwitch
              toggle
              (Ok >> UpdateSwitchState)
              (Error >> UpdateSwitchState)
        m,cmd
    //| m -> m, Cmd.none

let renderSwitch sw (dispatch:Msg->unit) =
  let toggle _ =
    let nextState = match sw.state with On -> Off | Off -> On
    dispatch (Toggle(sw.channel,nextState))
  [ Button.button_btn
      [ Button.onClick toggle; (if sw.state = On then Button.isPrimary else Button.isDark) ]
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

let view model (dispatch:Msg->unit) =
  div []
    [ Navbar.navbar [ Navbar.customClass "is-primary" ]
        [ Navbar.item_div [ ]
            [ Heading.h2 [ ]
                [ str "Home navigation" ] ] ]

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
