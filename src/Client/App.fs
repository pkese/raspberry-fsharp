module App

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

open Shared

open Fulma
open Fulma.Layouts
open Fulma.Elements
open Fulma.Components
open Fulma.BulmaClasses

type Model = Counter option

type Msg =
| Increment
| Decrement
| Init of Result<Counter, exn>

let init () = 
  let model = None
  let cmd =
    let routeBuilder typeName methodName = 
      sprintf "/api/%s/%s" typeName methodName
    let api = Fable.Remoting.Client.Proxy.createWithBuilder<Init> routeBuilder
    Cmd.ofAsync 
      api.getCounter
      () 
      (Ok >> Init)
      (Error >> Init)
  model, cmd

let update msg (model : Model) =
  let model' =
    match model,  msg with
    | Some x, Increment -> Some (x + 1)
    | Some x, Decrement -> Some (x - 1)
    | None, Init (Ok x) -> Some x
    | _ -> None
  model', Cmd.none

let safeComponents =
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
      "Fable.Remoting", "https://github.com/Zaid-Ajaj/Fable.Remoting"
    ]
    |> List.map (fun (desc,link) -> a [ Href link ] [ str desc ] )
    |> intersperse (str ", ")
    |> span [ ]

  p [ ]
    [ strong [] [ str "SAFE Template" ]
      str " powered by: "
      components ]

let show = function
| Some x -> string x
| None -> "Loading..."

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
                [ str "SAFE Template" ] ] ]

      Container.container []
        [ Content.content [ Content.customClass Bulma.Level.Item.HasTextCentered ] 
            [ Heading.h3 [] [ str ("Press buttons to manipulate counter: " + show model) ] ]
          Columns.columns [] 
            [ Column.column [] [ button "-" (fun _ -> dispatch Decrement) ]
              Column.column [] [ button "+" (fun _ -> dispatch Increment) ] ] ]
    
      Footer.footer [ ]
        [ Content.content [ Content.customClass Bulma.Level.Item.HasTextCentered ]
            [ safeComponents ] ] ]
  
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
