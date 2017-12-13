
module App

open System
open Elmish

open Switches
open EasyHome
open Shared

[<ReferenceEquality>]
type Socket = { send: Events -> unit }

type ClientEvent =
    | Connected of Socket
    | Disconnected of Socket

type State = {
    switches: Switches
  } with
    static member empty = { 
      switches =
        [   
            { channel=Sw1; state=Off; name="Heater"; mode=Auto }
            { channel=Sw2; state=Off; name="Boiler"; mode=Auto }
            { channel=Sw3; state=Off; name="HiFi"; mode=Presence }
            { channel=Sw4; state=Off; name="Aux"; mode=Manual }
        ]
    }

type Msg = 
    | Client of ClientEvent
    | Command of Commands
    | Send of Events
    | SendTo of Socket*Events
    | InitializeHw
    | UpdateElectricityTariff of bool

let awaitSequentially lst =
  let rec awaitSequentially' = function
    | [] -> async { return [] }
    | h::t -> async { 
      let! h' = h
      let! t' = awaitSequentially' t
      return h'::t'}
  lst |> awaitSequentially'

let updateElectricityTariff state withSchedule =

    let now = DateTime.Now

    let isHoliday = 
        match now.Day, now.Month with
        | 1,1 | 2,1 // novo leto
        | 8,2 // Prešernov dan
        | 27,4 // dan upora proti okupatorju
        | 1,5 | 2,5 // 1. maj
        | 25,6 // dan državnosti
        | 15,8 // marijino vnebovzetje
        | 31,10 | 1,11 // dan reformacije, dan mrtvih
        | 25,12 | 26,12 // božič, dan samostojnosti
          -> true
        | _ -> false

    let cheapElectricity =
        if isHoliday then On
        else match now.DayOfWeek, now.Hour with
              | DayOfWeek.Sunday, _ -> On
              | DayOfWeek.Saturday, _ -> On
              | _, hour when hour < 6 -> On
              | _, hour when hour >= 22 -> On
              | _, _ -> Off

    let msgs =
        state.switches
        |> List.filter (fun sw -> sw.mode = Auto)
        //|> List.filter (fun sw -> sw.state <> cheapElectricity)
        |> List.map (fun sw ->
            printfn "%A: auto %A -> %A" now sw.channel cheapElectricity
            Cmd.ofMsg (Command (SetSwitch (sw.channel, cheapElectricity))))

    let nextSwitch =
        let todayAt hour = now.Date.AddHours(float hour).AddSeconds(1.0)
        let tomorrowAt hour = now.Date.AddDays(1.0).AddHours(float hour).AddSeconds(1.0)
        match isHoliday, now.Hour, now.DayOfWeek with
        | _, _, DayOfWeek.Saturday -> tomorrowAt (24+6) // day after tomorrow
        | true, _, _ -> tomorrowAt 6
        | _, _, DayOfWeek.Sunday -> tomorrowAt 6
        | _, hour, _ when hour < 6 -> todayAt 6
        | _, hour, _ when hour < 22 -> todayAt 22
        | _ -> tomorrowAt 6


    if withSchedule
    then
        printfn "next switch at: %A / in: %A" nextSwitch (nextSwitch - DateTime.Now)

        let wait () = async {
            while (nextSwitch > DateTime.Now) do
                do! Async.Sleep (nextSwitch - DateTime.Now).Milliseconds
            return (UpdateElectricityTariff true)
        }
        let schedule = Cmd.ofAsync wait () id (fun _ -> UpdateElectricityTariff true)
        Cmd.batch (schedule::msgs)
    else
        Cmd.batch msgs

let rec findAndModify channel modifier =
    function
    | [] -> []
    | h::t when h.channel = channel -> (modifier h)::t
    | h::t -> h::(findAndModify channel modifier t)

let update (msg:Msg) (state:State) = //: State * Cmd<Msg> =
    printfn "appMsg %A" msg
    match msg with
    | Client (Connected socket) ->
        state, (Cmd.ofMsg (SendTo (socket,(Switches state.switches))))
    
    | Send _
    | SendTo _
    | Client (Disconnected _) -> state, Cmd.none

    | InitializeHw ->
        let cmds = updateElectricityTariff state true
        state, cmds

    | UpdateElectricityTariff withSchedule ->
        let cmds = updateElectricityTariff state withSchedule
        state, cmds

    | Command GetSwitches -> 
        state, (Cmd.ofMsg (Send (Switches state.switches)))

    | Command (SetSwitch (channel,swState)) -> 
        EasyHome.switches.Post (channel,swState)
        let switches' = state.switches |> findAndModify channel (fun s -> { s with state=swState })
        let cmd = Cmd.ofMsg (Send (Switches switches'))
        { state with switches = switches' }, cmd
    
    | Command (SetMode (channel,mode)) ->
        let switches' = state.switches |> findAndModify channel (fun s -> { s with mode=mode })
        let cmd = 
            let c = Cmd.ofMsg (Send (Switches switches'))
            if mode = Auto
            then Cmd.batch [c; Cmd.ofMsg (UpdateElectricityTariff false)]
            else c
        { state with switches = switches' }, cmd

let init () = State.empty, (Cmd.ofMsg InitializeHw)

