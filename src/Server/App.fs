module App

open System
open System.IO
open Elmish

open Shared

[<ReferenceEquality>]
type ClientSocket = { send: Notification -> unit }

type ClientMsg =
  | Connected of ClientSocket
  | Disconnected of ClientSocket
  | Send of ClientSocket*Notification
  | SendAll of Notification

type AppMsg = 
  | Client of ClientMsg
  | Command of ClientSocket*Command
  // add your own messages here

type State = {
    leds: Leds
  } with
    member this.UpdateLed channel f =
        this.leds |> List.map (fun led -> if led.channel = channel then f led else led)

type Led with
    static member private triggerFile = sprintf "/sys/class/leds/led%d/trigger"
    static member Init channel color = 
        let stripBrackets (s:string) = s.Replace("[","").Replace("]","")
        let rawTriggers =
            File.ReadAllText (Led.triggerFile channel)
            |> fun str -> str.Split()
            |> Seq.filter (not << String.IsNullOrEmpty)
            |> Seq.toList
        { channel=channel
          color=color
          trigger = rawTriggers |> List.find (fun s -> s.StartsWith("[")) |> stripBrackets
          triggers = rawTriggers |> List.map stripBrackets }
    static member SetTrigger trigger led =
        if led.triggers |> List.contains trigger then
            File.WriteAllText((Led.triggerFile led.channel), trigger)
            { led with trigger = trigger }
        else led

let update (msg:AppMsg) (state:State) : State * Cmd<AppMsg> =
    printfn "appMsg %A" msg
    match msg with

    | Command (_, SetTrigger (channel,trigger)) -> 
        let leds' = state.UpdateLed channel (Led.SetTrigger trigger)
        { state with leds = leds' }, (Cmd.ofMsg (Client (SendAll (Leds leds'))))

    | Client (Connected socket) -> // client connected: send it current state
        state, (Cmd.ofMsg (Client (Send (socket,(Leds state.leds)) )))

    | Client _ -> state, Cmd.none
    
let init () = 
    let state = { leds = [Led.Init 0 "Green"; Led.Init 1 "Red"] }
    state, Cmd.none

