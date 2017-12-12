
module App

open System

open Switches
open EasyHome
open Shared

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


let mutable state = State.empty
let patchState updater = state <- updater state

let getSwitches () = async {
   return state.switches
}
let setSwitch (channel,swState) = async {
    EasyHome.switches.Post (channel,swState)
    let updateState (state:State) = 
        let rec toggleInList = function
          | [] -> []
          | sw::tail when sw.channel = channel -> {sw with state=swState}::tail
          | h::t -> h::(toggleInList t)
        { state with switches = toggleInList state.switches }

    patchState updateState
    return state.switches
}

let setMode (channel,mode) = async {

    let updateState (state:State) = 
        let rec toggleInList = function
          | [] -> []
          | sw::tail when sw.channel = channel -> {sw with mode=mode}::tail
          | h::t -> h::(toggleInList t)
        { state with switches = toggleInList state.switches }

    patchState updateState

    return state.switches
}

let awaitSequentially lst =
  let rec awaitSequentially' = function
    | [] -> async { return [] }
    | h::t -> async { 
      let! h' = h
      let! t' = awaitSequentially' t
      return h'::t'}
  lst |> List.rev |> awaitSequentially'

let rec cheapElectricityFollower () = async {

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

    do! state.switches
        |> List.filter (fun sw -> sw.mode = Auto)
        |> List.map (fun sw ->
            printfn "%A: auto %A -> %A" now sw.channel cheapElectricity
            setSwitch (sw.channel,cheapElectricity))
        |> awaitSequentially
        |> Async.Ignore

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

    printfn "next switch at: %A / in: %A" nextSwitch (nextSwitch - DateTime.Now)

    while (nextSwitch > DateTime.Now) do do! Async.Sleep (nextSwitch - DateTime.Now).Milliseconds
    return! cheapElectricityFollower()
  }
cheapElectricityFollower() |> Async.Start
